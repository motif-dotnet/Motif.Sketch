namespace Motif.Sketch.Specs;

public sealed class Autosaving
{
    [Fact]
    public async Task Every_edit_is_autosaved()
    {
        var h = await Harness.Started();

        h.Tasks.AddElement(ElementType.Storage);

        h.BrowserStorage.Work!.Json.Should().Be(h.Queries.ProjectJsonText);
        h.BrowserStorage.Work.Unsaved.Should().BeTrue();
    }

    [Fact]
    public async Task The_next_visit_restores_the_work_with_its_file_and_unsaved_marker()
    {
        var first = await Harness.Started();
        first.ProjectFiles.NextSaveAsName = "shop.motifsketch.json";
        await first.Tasks.SaveAs();
        first.Tasks.AddElement(ElementType.Storage);

        var next = new Harness();
        next.BrowserStorage.Work = first.BrowserStorage.Work;
        var started = await next.Tasks.Start(null);

        started.StartedAs.Should().Be(StartedAs.Restored);
        next.Queries.ProjectJsonText.Should().Be(first.Queries.ProjectJsonText);
        next.Queries.Should().BeEquivalentTo(new { FileName = "shop.motifsketch.json", HasUnsavedChanges = true });
    }

    [Fact]
    public async Task After_a_refresh_Save_still_goes_to_the_same_file()
    {
        var first = await Harness.Started();
        await first.Tasks.SaveAs();
        var handle = first.ProjectFiles.Writes[^1].Handle;

        var next = new Harness(first.ProjectFiles.AfterARefresh());
        next.BrowserStorage.Work = first.BrowserStorage.Work;
        await next.Tasks.Start(null);
        (await next.Tasks.Save()).Succeeded.Should().BeTrue();

        next.ProjectFiles.Writes.Should().ContainSingle().Which.Handle.Should().Be(handle);
    }

    [Fact]
    public async Task After_a_refresh_Save_says_so_if_the_browser_isnt_let_write_to_the_file()
    {
        var first = await Harness.Started();
        await first.Tasks.SaveAs();

        var next = new Harness(first.ProjectFiles.AfterARefresh());
        next.BrowserStorage.Work = first.BrowserStorage.Work;
        next.ProjectFiles.RefuseWrites = true;
        await next.Tasks.Start(null);
        var outcome = await next.Tasks.Save();

        outcome.Succeeded.Should().BeFalse();
        outcome.Problems.Should().ContainSingle().Which.Should()
            .Contain("wasn't allowed to write to that file").And.Contain("Save as");
        next.ProjectFiles.Writes.Should().BeEmpty();
    }

    [Fact]
    public async Task With_nothing_autosaved_the_first_run_is_a_blank_project()
    {
        var h = new Harness();

        (await h.Tasks.Start(null)).StartedAs.Should().Be(StartedAs.Fresh);
        h.Queries.Context.Elements.Should().ContainSingle();
    }

    [Fact]
    public async Task A_second_tab_is_read_only_and_never_writes_autosave()
    {
        var h = new Harness();
        h.BrowserStorage.HeldElsewhere = true;
        h.BrowserStorage.Work = new AutosavedWork(Example.Json(), null, null, false);

        (await h.Tasks.Start(null)).StartedAs.Should().Be(StartedAs.OtherTab);

        h.Queries.Should().BeEquivalentTo(new { Mode = AppMode.OtherTab, IsReadOnly = true });
        h.Queries.Title.Should().Be("Motif Sketch");
        h.Invoking(x => x.Tasks.AddElement(ElementType.Storage)).Should().Throw<ProjectRuleException>();
        h.BrowserStorage.Writes.Should().BeEmpty();
    }

    [Fact]
    public async Task Use_this_tab_instead_hands_ownership_over_with_the_latest_work()
    {
        var h = new Harness();
        h.BrowserStorage.HeldElsewhere = true;
        await h.Tasks.Start(null);
        h.BrowserStorage.Work = new AutosavedWork(Example.Json(), "latest.motifsketch.json", null, true);

        await h.Tasks.UseThisTab();

        h.Queries.Mode.Should().Be(AppMode.Architect);
        h.Queries.FileName.Should().Be("latest.motifsketch.json");
        h.Tasks.AddElement(ElementType.Storage);
        h.BrowserStorage.Writes.Should().ContainSingle();
    }

    [Fact]
    public async Task A_second_tab_says_its_read_only_and_how_to_edit_there()
    {
        var h = new Harness();
        h.BrowserStorage.HeldElsewhere = true;

        (await h.Tasks.Start(null)).Note.Should().Be(
            new Note("Motif Sketch is open in another tab", "This tab is read-only. Use this tab instead to edit here."));
    }

    [Fact]
    public async Task Use_this_tab_instead_says_the_other_tab_is_read_only_now()
    {
        var h = new Harness();
        h.BrowserStorage.HeldElsewhere = true;
        await h.Tasks.Start(null);

        (await h.Tasks.UseThisTab()).Should().Be(new Note("Editing in this tab", "The other tab is read-only now."));
    }

    [Fact]
    public async Task A_tab_that_owns_autosave_has_nothing_to_say_on_start_or_on_use_this_tab()
    {
        var h = new Harness();

        (await h.Tasks.Start(null)).Should().Be(new StartResult(StartedAs.Fresh));
        (await h.Tasks.UseThisTab()).Should().Be(Note.None);
    }

    [Fact]
    public async Task The_first_tab_goes_read_only_when_another_takes_over()
    {
        var h = await Harness.Started();

        await h.BrowserStorage.AnotherTabAsksForTheLock();

        h.Queries.Mode.Should().Be(AppMode.OtherTab);
        h.Invoking(x => x.Tasks.AddElement(ElementType.Storage)).Should().Throw<ProjectRuleException>();
    }

    [Fact]
    public async Task A_tab_that_took_over_hands_autosave_back_when_asked()
    {
        var h = new Harness();
        h.BrowserStorage.HeldElsewhere = true;
        await h.Tasks.Start(null);
        await h.Tasks.UseThisTab();
        h.BrowserStorage.HeldElsewhere.Should().BeFalse("this tab took the lock");

        await h.BrowserStorage.AnotherTabAsksForTheLock();

        h.Queries.Mode.Should().Be(AppMode.OtherTab);
        h.BrowserStorage.HeldElsewhere.Should().BeTrue();
    }

    [Fact]
    public async Task Autosave_isnt_a_save_unsaved_still_means_not_saved_to_a_file()
    {
        var h = await Harness.Started();
        h.Tasks.AddElement(ElementType.Storage);

        h.BrowserStorage.Writes.Count.Should().BePositive();
        h.Queries.HasUnsavedChanges.Should().BeTrue();
    }

    [Fact]
    public async Task While_a_write_is_in_flight_only_the_newest_work_is_written_next()
    {
        var h = await Harness.Started();
        h.BrowserStorage.HoldWrites();
        h.Tasks.AddElement(ElementType.Storage);
        var first = h.Queries.ProjectJsonText;
        h.Tasks.AddElement(ElementType.ExternalSystem);
        h.Tasks.AddElement(ElementType.SecondaryActor);

        h.BrowserStorage.ReleaseWrites();

        h.BrowserStorage.Writes.Select(w => w.Json).Should().Equal(first, h.Queries.ProjectJsonText);
    }

    [Fact]
    public async Task The_last_autosave_lands_before_another_tab_takes_over()
    {
        var h = await Harness.Started();
        h.BrowserStorage.HoldWrites();
        h.Tasks.AddElement(ElementType.Storage);
        h.Tasks.AddElement(ElementType.ExternalSystem);

        var takeOver = h.BrowserStorage.AnotherTabAsksForTheLock();
        h.BrowserStorage.HeldElsewhere.Should().BeFalse("the lock waits for the writes");
        h.BrowserStorage.ReleaseWrites();
        await takeOver;

        h.BrowserStorage.Work!.Json.Should().Be(h.Queries.ProjectJsonText);
        h.BrowserStorage.HeldElsewhere.Should().BeTrue();
        h.Queries.Mode.Should().Be(AppMode.OtherTab);
    }

    [Fact]
    public async Task Another_tab_gets_autosave_even_when_the_last_write_fails()
    {
        var h = await Harness.Started();
        h.BrowserStorage.HoldWrites();
        h.Tasks.AddElement(ElementType.Storage);
        h.BrowserStorage.Fail = true;

        var takeOver = h.BrowserStorage.AnotherTabAsksForTheLock();
        h.BrowserStorage.ReleaseWrites();
        await takeOver;

        h.BrowserStorage.HeldElsewhere.Should().BeTrue();
        h.Queries.Mode.Should().Be(AppMode.OtherTab);
    }

    [Fact]
    public async Task Where_tabs_cant_share_a_lock_every_tab_owns_autosave()
    {
        var h = new Harness();
        h.BrowserStorage.Lockable = false;
        h.BrowserStorage.HeldElsewhere = true;

        (await h.Tasks.Start(null)).StartedAs.Should().Be(StartedAs.Fresh);
        h.Tasks.AddElement(ElementType.Storage);

        h.Queries.Mode.Should().Be(AppMode.Architect);
        h.BrowserStorage.Writes.Should().ContainSingle();
    }

    [Theory]
    [InlineData("NewProject")]
    [InlineData("OpenExample")]
    [InlineData("Open")]
    [InlineData("Save")]
    [InlineData("SaveAs")]
    public async Task A_second_tab_cant_start_new_work_open_the_example_open_a_file_or_save(string action)
    {
        var h = new Harness();
        h.BrowserStorage.HeldElsewhere = true;
        h.BrowserStorage.Work = new AutosavedWork(Example.Json(), null, null, false);
        await h.Tasks.Start(null);
        var before = h.Queries.ProjectJsonText;
        h.ProjectFiles.NextOpen = h.ProjectFiles.FileOf("other.motifsketch.json", Example.Json());
        h.ProjectFiles.NextSaveAsName = "mine.motifsketch.json";
        var writesBefore = h.BrowserStorage.Writes.Count;

        Func<Task> act = action switch
        {
            "NewProject" => () => h.Tasks.NewProject(),
            "OpenExample" => () => h.Tasks.OpenExample(),
            "Open" => () => h.Tasks.Open(),
            "Save" => () => h.Tasks.Save(),
            _ => () => h.Tasks.SaveAs(),
        };

        await act.Should().ThrowAsync<ProjectRuleException>();
        h.Queries.ProjectJsonText.Should().Be(before);
        h.ProjectFiles.Writes.Should().BeEmpty();
        h.BrowserStorage.Writes.Count.Should().Be(writesBefore);
    }
}
