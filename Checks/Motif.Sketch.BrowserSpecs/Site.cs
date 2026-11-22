using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Motif.Sketch.BrowserSpecs;

[CollectionDefinition(Name)]
public sealed class SiteCollection : ICollectionFixture<Site>
{
    public const string Name = "Site";
}

/// <summary>
/// The app as it's deployed: published in Release (trimmed), served as
/// static files, and opened in a real browser. Each page gets a fresh
/// browser profile, so autosave and preferences start empty. Nothing leaves
/// the machine: GitHub is this repo's own files, and every other request
/// (such as the visit counter) is blocked.
/// </summary>
public sealed class Site : IAsyncLifetime
{
    private const string GitHub = "https://raw.githubusercontent.com/";

    private readonly string repo = RepoRoot();
    private WebApplication? server;
    private IPlaywright? playwright;
    private IBrowser? browser;

    public string Url { get; private set; } = "";

    public async Task InitializeAsync()
    {
        // MOTIF_SITE is a folder the app is already published to, as CI
        // publishes it once for the specs and the deploy. Otherwise the
        // specs publish it themselves.
        var site = Environment.GetEnvironmentVariable("MOTIF_SITE");
        if (string.IsNullOrEmpty(site))
        {
            site = Path.Combine(AppContext.BaseDirectory, "site");
            await Publish(site);
        }
        server = Serve(Path.Combine(site, "wwwroot"));
        await server.StartAsync();
        Url = server.Services.GetRequiredService<IServer>()
            .Features.GetRequiredFeature<IServerAddressesFeature>()
            .Addresses.Single() + "/";

        playwright = await Playwright.CreateAsync();
        // The Edge every Windows machine has, so there's nothing to download.
        // MOTIF_BROWSER picks another channel, such as "chrome".
        browser = await playwright.Chromium.LaunchAsync(new()
        {
            Channel = Environment.GetEnvironmentVariable("MOTIF_BROWSER")
                ?? "msedge",
        });
    }

    /// <summary>
    /// A browser profile of its own, at this window width, allowed these
    /// permissions (such as "clipboard-read").
    /// </summary>
    public async Task<IBrowserContext> Profile(
        int width = 1280, params string[] permissions)
    {
        var profile = await browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = 812 },
            Permissions = permissions,
        });
        await profile.RouteAsync("**/*", Route);
        return profile;
    }

    /// <summary>Opens the app at this fragment and waits for it to start.</summary>
    public async Task<IPage> Open(
        IBrowserContext profile, string fragment = "")
    {
        var page = await profile.NewPageAsync();
        await Go(page, fragment);
        return page;
    }

    public async Task Go(IPage page, string fragment = "")
    {
        await page.GotoAsync(Url + fragment);
        await Started(page);
    }

    public static Task Started(IPage page) =>
        page.Locator("header.appbar").WaitForAsync();

    private async Task Route(IRoute route)
    {
        var url = route.Request.Url;
        if (url.StartsWith(Url, StringComparison.Ordinal))
            await route.ContinueAsync();
        else if (url.StartsWith(GitHub, StringComparison.Ordinal))
            await FromThisRepo(route, url[GitHub.Length..]);
        else
            await route.AbortAsync();
    }

    /// <summary>
    /// Every repo is this one: motif-dotnet/Motif.Sketch/HEAD/motif/… is
    /// read from the motif/ folder here, and anything else isn't there.
    /// </summary>
    private async Task FromThisRepo(IRoute route, string path)
    {
        const string Prefix = "motif-dotnet/Motif.Sketch/HEAD/";
        var file = Path.Combine(repo, path.Replace(Prefix, ""));
        if (path.StartsWith(Prefix, StringComparison.Ordinal)
            && File.Exists(file))
            await route.FulfillAsync(new()
            {
                Body = await File.ReadAllTextAsync(file),
                Headers = new Dictionary<string, string>
                {
                    ["Access-Control-Allow-Origin"] = "*",
                },
            });
        else
            await route.FulfillAsync(new() { Status = 404 });
    }

    private async Task Publish(string output)
    {
        var app = Path.Combine(repo, "Motif.Sketch.App");
        using var publish = Process.Start(new ProcessStartInfo(
            "dotnet", ["publish", app, "-c", "Release", "-o", output])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        })!;
        var log = publish.StandardOutput.ReadToEndAsync();
        var errors = publish.StandardError.ReadToEndAsync();
        await publish.WaitForExitAsync();
        if (publish.ExitCode != 0)
            throw new InvalidOperationException(
                "Publishing the app failed:\n" + await log + await errors);
    }

    private static WebApplication Serve(string root)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        var app = builder.Build();
        var files = new PhysicalFileProvider(root);
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = files,
            ContentTypeProvider = new FileExtensionContentTypeProvider(),
            ServeUnknownFileTypes = true,
        });
        return app;
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir.FullName, "Motif.Sketch.sln")))
            dir = dir.Parent ?? throw new InvalidOperationException(
                "The specs must run inside the Motif.Sketch repo.");
        return dir.FullName;
    }

    public async Task DisposeAsync()
    {
        if (browser is not null) await browser.DisposeAsync();
        playwright?.Dispose();
        if (server is not null) await server.DisposeAsync();
    }
}
