using System.Text.RegularExpressions;

namespace Motif.Sketch.BrowserSpecs;

/// <summary>A copied link: read-only for whoever opens it, until copied.</summary>
[Collection(SiteCollection.Name)]
public sealed class Sharing(Site site)
{
    [Fact]
    public async Task A_copied_link_opens_read_only_in_another_browser()
    {
        var link = await CopiedLink();
        await using var other = await site.Profile();

        var page = await site.Open(other, Fragment(link));

        await Assertions.Expect(page.Locator(".pill").GetByText("read-only"))
            .ToBeVisibleAsync();
        await Assertions.Expect(page.GetByTitle("Add a storage"))
            .ToBeHiddenAsync();
        (await Autosaving.Elements(page)).Should()
            .Be(2, "the system and the storage");
    }

    [Fact]
    public async Task Make_a_copy_turns_a_link_into_own_work()
    {
        var link = await CopiedLink();
        await using var profile = await site.Profile();
        var page = await site.Open(profile, Fragment(link));

        await page.GetByRole(AriaRole.Button,
            new() { Name = "Make a copy to edit" }).ClickAsync();
        await page.GetByRole(AriaRole.Button,
            new() { Name = "Make a copy", Exact = true }).ClickAsync();

        await Assertions.Expect(page.GetByTitle("Add a storage"))
            .ToBeVisibleAsync();
        await Assertions.Expect(page).Not.ToHaveURLAsync(new Regex("#"));
    }

    /// <summary>
    /// A link to work with a storage added, as Copy link puts it on the
    /// clipboard.
    /// </summary>
    private async Task<string> CopiedLink()
    {
        await using var profile = await site.Profile(
            1280, "clipboard-read", "clipboard-write");
        var page = await site.Open(profile);
        await page.GetByTitle("Add a storage").ClickAsync();
        await page.GetByTitle("Copy a read-only link").ClickAsync();
        // The link is on the clipboard once the app says so.
        await Assertions.Expect(page.Locator(".toast").First)
            .ToBeVisibleAsync();
        var link = await page.EvaluateAsync<string>(
            "navigator.clipboard.readText()");
        link.Should().Contain("#", "the link Copy link put on the clipboard");
        return link;
    }

    private static string Fragment(string link) =>
        link[link.IndexOf('#')..];
}
