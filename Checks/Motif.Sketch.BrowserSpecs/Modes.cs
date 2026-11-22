namespace Motif.Sketch.BrowserSpecs;

/// <summary>What the page is showing when it is measured.</summary>
public enum Showing
{
    Example, RepoLink, SnapshotLink, OwnWork,

    /// <summary>A second tab in the same profile, so read-only.</summary>
    OtherTab,
}

/// <summary>Opens the app in each mode it shows.</summary>
public sealed class Modes(Site site)
{
    public async Task<IPage> Open(IBrowserContext profile, Showing showing)
    {
        switch (showing)
        {
            case Showing.Example:
                return await site.Open(profile, "#example");
            case Showing.RepoLink:
                return await site.Open(
                    profile, "#repo=motif-dotnet/Motif.Sketch");
            case Showing.SnapshotLink:
                return await site.Open(
                    profile, "#" + SnapshotLink.Fragment(Example.Project()));
            case Showing.OtherTab:
                await site.Open(profile);
                return await site.Open(profile);
            default:
                // A phone can't edit, so the work is made on a computer,
                // then autosave brings it back at this width.
                var page = await profile.NewPageAsync();
                var width = page.ViewportSize!.Width;
                await page.SetViewportSizeAsync(1280, 812);
                await site.Go(page);
                await page.GetByTitle("Add a storage").ClickAsync();
                await page.SetViewportSizeAsync(width, 812);
                await site.Go(page);
                return page;
        }
    }
}
