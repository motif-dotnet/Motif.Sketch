namespace Motif.Sketch.BrowserSpecs;

/// <summary>What the page is showing when the app bar is measured.</summary>
public enum Showing { Example, RepoLink, SnapshotLink, OwnWork }

/// <summary>
/// The repo memory's layout rule: at a phone's width, between the phone and
/// narrow breakpoints, and on a computer, in each mode the app shows, the
/// app bar's content is no wider than the bar and nothing in it overlaps.
/// </summary>
[Collection(SiteCollection.Name)]
public sealed class TheAppBar(Site site)
{
    public static TheoryData<int, Showing> Cases()
    {
        var cases = new TheoryData<int, Showing>();
        foreach (var width in new[] { 375, 637, 1280 })
            foreach (var showing in Enum.GetValues<Showing>())
                cases.Add(width, showing);
        return cases;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Fits_with_nothing_overlapping(int width, Showing showing)
    {
        await using var profile = await site.Profile(width);
        var page = await Open(profile, showing);

        var bar = await Measure(page);

        bar.ContentWidth.Should().BeLessThanOrEqualTo(bar.Width);
        bar.Overlapping.Should().BeEmpty();
    }

    private async Task<IPage> Open(IBrowserContext profile, Showing showing)
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

    /// <summary>Filled in by Playwright, so it needs setters.</summary>
    private sealed class Bar
    {
        public double Width { get; set; }
        public double ContentWidth { get; set; }
        public string[] Overlapping { get; set; } = [];
    }

    /// <summary>
    /// The bar's widths, and each pair of its visible items that overlap,
    /// named by their text or tooltip. An item squeezed narrower than its
    /// content reaches as far as its content does, since that's what shows.
    /// </summary>
    private static Task<Bar> Measure(IPage page) =>
        page.Locator("header.appbar").EvaluateAsync<Bar>("""
            bar => {
              const reach = e => {
                const r = e.getBoundingClientRect();
                const right = Math.max(r.right, r.left + e.scrollWidth);
                return { left: r.left, right, top: r.top, bottom: r.bottom,
                         width: r.width, height: r.height };
              };
              const items = [...bar.children]
                .filter(e => !e.classList.contains("spacer"))
                .map(e => ({ e, r: reach(e) }))
                .filter(i => i.r.width > 0 && i.r.height > 0);
              const name = e => [e.title, e.textContent,
                  e.getAttribute("aria-label"), e.className]
                .map(s => (s ?? "").trim()).find(s => s).slice(0, 30);
              const overlapping = [];
              for (let a = 0; a < items.length; a++)
                for (let b = a + 1; b < items.length; b++) {
                  const [p, q] = [items[a].r, items[b].r];
                  if (p.right - 0.5 > q.left && q.right - 0.5 > p.left
                      && p.bottom - 0.5 > q.top && q.bottom - 0.5 > p.top)
                    overlapping.push(
                      name(items[a].e) + " / " + name(items[b].e));
                }
              return {
                width: bar.clientWidth,
                contentWidth: bar.scrollWidth,
                overlapping,
              };
            }
            """);
}
