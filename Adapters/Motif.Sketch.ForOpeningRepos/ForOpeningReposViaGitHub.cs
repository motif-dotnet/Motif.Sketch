using System.Net;

namespace Motif.Sketch.Adapters;

/// <summary>
/// Reads a public repo's sketch from raw.githubusercontent.com, at HEAD (the
/// default branch), as Motif Glossary does. No sign-in and no API rate limit,
/// and raw.githubusercontent.com allows cross-origin reads, so this works from
/// Pages. Its Seam is the platform's <see cref="HttpMessageHandler"/>: the
/// browser's in the app, a fake GitHub in specs.
/// </summary>
public sealed class ForOpeningReposViaGitHub(HttpMessageHandler github)
    : ForOpeningRepos
{
    private const string Host = "https://raw.githubusercontent.com";

    private readonly HttpClient http = new(github);

    public async Task<string?> ReadSketch(
        RepoName repo, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync(
            Url(repo, RepoLink.SketchPath), cancellationToken);
        // GitHub says 404 both for a missing file and for a repo that isn't
        // public.
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static Uri Url(RepoName repo, string path) =>
        new($"{Host}/{Uri.EscapeDataString(repo.Owner)}/"
            + $"{Uri.EscapeDataString(repo.Name)}/HEAD/{path}");
}
