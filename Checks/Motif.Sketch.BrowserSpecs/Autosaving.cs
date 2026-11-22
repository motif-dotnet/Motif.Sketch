namespace Motif.Sketch.BrowserSpecs;

/// <summary>Autosave in the browser's own storage, across reloads and tabs.</summary>
[Collection(SiteCollection.Name)]
public sealed class Autosaving(Site site)
{
    private const string OtherTab =
        "Motif Sketch is open in another tab, so this one is read-only";

    [Fact]
    public async Task Work_comes_back_after_a_reload_still_marked_unsaved()
    {
        await using var profile = await site.Profile();
        var page = await site.Open(profile);
        await page.GetByTitle("Add a storage").ClickAsync();

        await site.Go(page);

        (await Elements(page)).Should().Be(2, "the system and the storage");
        await Assertions.Expect(page.GetByTitle("Unsaved changes"))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task A_second_tab_opens_read_only()
    {
        await using var profile = await site.Profile();
        await site.Open(profile);

        var second = await site.Open(profile);

        await Assertions.Expect(second.GetByText(OtherTab))
            .ToBeVisibleAsync();
        await Assertions.Expect(second.GetByTitle("Add a storage"))
            .ToBeHiddenAsync();
    }

    [Fact]
    public async Task Use_this_tab_instead_moves_editing_to_it()
    {
        await using var profile = await site.Profile();
        var first = await site.Open(profile);
        await first.GetByTitle("Add a storage").ClickAsync();
        var second = await site.Open(profile);

        await second.GetByRole(AriaRole.Button,
            new() { Name = "Use this tab instead" }).First.ClickAsync();

        await Assertions.Expect(first.GetByText(OtherTab))
            .ToBeVisibleAsync();
        await Assertions.Expect(second.GetByTitle("Add a storage"))
            .ToBeVisibleAsync();
        (await Elements(second)).Should().Be(2, "the first tab's work");
    }

    /// <summary>
    /// The context diagram's elements, the system among them. An element is
    /// drawn as several nodes that share its hit, so hits are counted once.
    /// </summary>
    private static Task<int> Elements(IPage page) =>
        page.Locator(".pane.context [data-hit^='element:']")
            .EvaluateAllAsync<int>(
                "nodes => new Set(nodes.map(n => n.dataset.hit)).size");
}
