namespace Motif.Sketch.Specs;

public sealed class Sharing
{
    private const string Page = "https://example.github.io/Motif.Sketch/";

    [Fact]
    public async Task Copy_link_puts_a_snapshot_of_the_whole_project_in_the_fragment()
    {
        var h = await Harness.WithExample();

        var outcome = await h.Tasks.CopyLink(Page + "#old");

        outcome.Should().BeEquivalentTo(new
        {
            Succeeded = true,
            IsLong = false,
            Title = "Read-only link copied",
            Text = "It's a snapshot: later edits need a new link.",
        });
        h.Clipboard.Text!.Should().StartWith(Page + "#v1.").And.Be(outcome.Link);
    }

    [Fact]
    public async Task Opening_the_link_gives_the_same_project_read_only()
    {
        var h = await Harness.WithExample();
        await h.Tasks.CopyLink(Page);
        var text = h.Clipboard.Text!;
        var fragment = text[text.IndexOf('#')..];

        var viewer = new Harness();
        var started = await viewer.Tasks.Start(fragment);

        started.StartedAs.Should().Be(StartedAs.Snapshot);
        viewer.Queries.ProjectJsonText.Should().Be(h.Queries.ProjectJsonText);
        viewer.Queries.Should().BeEquivalentTo(new { Mode = AppMode.Viewer, IsReadOnly = true, HasSketch = true });
        viewer.Invoking(x => x.Tasks.AddElement(ElementType.Storage)).Should().Throw<ProjectRuleException>();
    }

    [Fact]
    public async Task Viewer_mode_never_touches_autosave()
    {
        var mine = new AutosavedWork("{ my own work }", "mine.motifsketch.json", null, true);
        var viewer = new Harness();
        viewer.BrowserStorage.Work = mine;

        await viewer.Tasks.Start("#" + SnapshotLink.Fragment(Example.Project()));
        await viewer.Tasks.ExportContextDiagram();
        await viewer.Tasks.ExportSketch();
        await viewer.Tasks.ExportPlan();

        viewer.BrowserStorage.Writes.Should().BeEmpty();
        viewer.BrowserStorage.Work.Should().Be(mine);
        viewer.DiagramExports.Written.Should().HaveCount(2);
        viewer.PlanExports.Written.Should().ContainSingle();
    }

    [Fact]
    public async Task Make_a_copy_to_edit_turns_the_snapshot_into_ordinary_unsaved_work()
    {
        var viewer = new Harness();
        await viewer.Tasks.Start("#" + SnapshotLink.Fragment(Example.Project()));

        await viewer.Tasks.MakeCopyToEdit();

        viewer.Queries.Should().BeEquivalentTo(new { Mode = AppMode.Architect, HasUnsavedChanges = true, FileName = (string?)null });
        viewer.BrowserStorage.Work!.Json.Should().Be(viewer.Queries.ProjectJsonText);
        viewer.Tasks.AddElement(ElementType.Storage);
    }

    [Theory]
    [InlineData("#v1.not-really-deflate", "damaged")]
    [InlineData("#v9.abc", "newer version")]
    public async Task A_bad_link_gives_a_friendly_error_and_opens_the_visitors_own_work(string fragment, string problem)
    {
        var h = new Harness();
        h.BrowserStorage.Work = new AutosavedWork(Example.Json(), null, null, false);

        var started = await h.Tasks.Start(fragment);

        started.Problem.Text.Should().Contain(problem);
        started.StartedAs.Should().Be(StartedAs.Restored);
        h.Queries.Mode.Should().Be(AppMode.Architect);
    }

    [Fact]
    public async Task A_bad_link_says_it_cant_be_opened_and_that_this_is_the_visitors_own_work()
    {
        var started = await new Harness().Tasks.Start("#v1.not-really-deflate");

        started.Problem.Title.Should().Be("That link can't be opened");
        started.Problem.Text.Should().EndWith(" Here is your own work instead.");
    }

    [Fact]
    public async Task Making_a_copy_says_the_work_is_now_the_visitors_own()
    {
        var viewer = new Harness();
        await viewer.Tasks.Start("#" + SnapshotLink.Fragment(Example.Project()));

        (await viewer.Tasks.MakeCopyToEdit()).Should().Be(
            new Note("This is now your own work", "It autosaves in this browser. Save it to a file to keep it."));
    }

    [Fact]
    public void A_snapshot_holding_an_invalid_project_is_refused()
    {
        var bad = Project.CreateNew();
        bad.ContextDiagram.Elements.Clear();

        SnapshotLink.Read(SnapshotLink.Fragment(bad)).Problem.Should().Contain("can't be opened");
    }

    [Fact]
    public async Task A_very_long_link_is_still_copied_but_flagged()
    {
        var h = await Harness.Started();
        for (var i = 0; i < 160; i++)
        {
            var id = h.Tasks.AddElement(ElementType.Storage);
            h.Tasks.DescribeElement(id, Guid.NewGuid().ToString() + Guid.NewGuid() + Guid.NewGuid());
        }

        var outcome = await h.Tasks.CopyLink(Page);

        outcome.Should().BeEquivalentTo(new { Succeeded = true, IsLong = true, Title = "Link copied, but it's long" });
        outcome.Text.Should().StartWith($"It's {outcome.Link!.Length:N0} characters.");
        h.Clipboard.Text.Should().Be(outcome.Link);
    }
}
