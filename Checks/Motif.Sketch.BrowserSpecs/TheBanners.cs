namespace Motif.Sketch.BrowserSpecs;

/// <summary>
/// The same layout rule for the banner under the app bar: in each mode that
/// shows one, its content is no wider than it and nothing in it overlaps.
/// </summary>
[Collection(SiteCollection.Name)]
public sealed class TheBanners(Site site)
{
    public static TheoryData<int, Showing> Cases()
    {
        var cases = new TheoryData<int, Showing>();
        foreach (var width in new[] { 375, 637, 1280 })
            foreach (var showing in new[]
                { Showing.Example, Showing.RepoLink, Showing.OtherTab })
                cases.Add(width, showing);
        // Only phones show a banner for a snapshot link or own work.
        cases.Add(375, Showing.SnapshotLink);
        cases.Add(375, Showing.OwnWork);
        return cases;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Fits_with_nothing_overlapping(int width, Showing showing)
    {
        await using var profile = await site.Profile(width);
        var page = await new Modes(site).Open(profile, showing);

        await Assertions.Expect(page.Locator(".banner")).ToBeVisibleAsync();
        var banner = await Measuring.Measure(page, ".banner");

        banner.ContentWidth.Should().BeLessThanOrEqualTo(banner.Width);
        banner.Overlapping.Should().BeEmpty();
    }
}
