namespace Motif.Sketch.Specs;

/// <summary>
/// The plan for deep adapters, as Motif.NET's adapters design note sets out: adapters named
/// for their port then their way, and a Seam with a real and a fake Fitting in place of a port fake.
/// </summary>
public sealed class PlanningDeepAdapters
{
    /// <summary>The example with no Seams but the IndexedDB adapter's, which reaches the browser through one.</summary>
    private static async Task<Harness> WithDeepAutosave()
    {
        var h = await Harness.WithExample();
        h.ClearSeams();
        h.Tasks.SetAdapterSeam(h.Adapter("IndexedDB").Id, "Browser Storage");
        return h;
    }

    private static async Task<string> Plan(Harness h)
    {
        var outcome = await h.Tasks.ExportPlan();
        outcome.Problems.Should().BeEmpty();
        return h.PlanExports.Written.Single().Markdown;
    }

    [Fact]
    public void Adapters_are_named_for_their_port_then_their_way()
    {
        SolutionPlanner.ClassFor("ForCopying", "BrowserClipboard", null, AdapterKind.Real)
            .Should().Be("ForCopyingViaBrowserClipboard");
        SolutionPlanner.ClassFor("ForCopying", "ForCopying", "Clipboard API", AdapterKind.Real)
            .Should().Be("ForCopyingViaClipboardApi");
        SolutionPlanner.ClassFor("ForCopying", "ForCopyingViaBrowser", null, AdapterKind.Real)
            .Should().Be("ForCopyingViaBrowser");
        SolutionPlanner.ClassFor("ForCopying", "", null, AdapterKind.Simulated)
            .Should().Be("ForCopyingViaSimulated");
        SolutionPlanner.ClassFor("ForCopying", "", null, AdapterKind.Real).Should().BeNull();
    }

    [Fact]
    public void Fakes_keep_the_fake_first()
    {
        SolutionPlanner.ClassFor("ForCopying", "FakeForCopying", null, AdapterKind.Fake)
            .Should().Be("FakeForCopying");
        SolutionPlanner.ClassFor("Projects", "", null, AdapterKind.Fake).Should().Be("FakeProjects");
    }

    [Fact]
    public async Task A_deep_adapter_gets_its_Seam_with_a_real_and_a_fake_Fitting_in_place_of_a_port_fake()
    {
        var md = await Plan(await WithDeepAutosave());

        md.Should().Contain(
            "| For autosaving | IndexedDB | `BrowserStorage` | `JsInteropBrowserStorage` | `FakeBrowserStorage` (`ForAutosaving/`) |");
        md.Should().Contain("public sealed class ForAutosavingViaIndexedDB(BrowserStorage browserStorage) : ForAutosaving");
        md.Should().NotContain("FakeForAutosaving");
    }

    [Fact]
    public async Task A_port_with_an_adapter_that_is_its_own_Fitting_still_gets_a_port_fake()
    {
        var h = await WithDeepAutosave();
        h.Tasks.AddAdapter(h.Port("For autosaving").Id, AdapterKind.Simulated);

        var md = await Plan(h);

        md.Should().Contain("| For autosaving | Fake for autosaving | Fake | - | `Motif.Sketch.Fakes` (`ForAutosaving/`) | `FakeForAutosaving` | Added by the plan");
    }

    [Fact]
    public async Task A_drawn_port_fake_beside_deep_adapters_is_warned_about()
    {
        var h = await WithDeepAutosave();
        h.Tasks.AddAdapter(h.Port("For autosaving").Id, AdapterKind.Fake);

        var outcome = await h.Tasks.ExportPlan();

        outcome.Succeeded.Should().BeTrue();
        outcome.Warnings.Should().ContainSingle(w => w.Contains("Fake for autosaving"))
            .Which.Should().Contain("fake Fittings");
    }

    [Fact]
    public async Task The_fakes_and_specs_reference_the_adapter_projects_with_Seams()
    {
        var md = await Plan(await WithDeepAutosave());

        md.Should().Contain("| `Motif.Sketch.Fakes` | `Motif.Sketch.UseCases`, `Motif.Sketch.ForAutosaving` |");
        md.Should().Contain(
            "| `Motif.Sketch.Specs` | `Motif.Sketch.UseCases`, `Motif.Sketch.Fakes`, `Motif.Sketch.ForAutosaving` |");
    }

    [Fact]
    public async Task Without_Seams_the_fakes_and_specs_reference_no_adapter_projects()
    {
        var h = await Harness.WithExample();
        h.ClearSeams();

        var md = await Plan(h);

        md.Should().Contain("| `Motif.Sketch.Fakes` | `Motif.Sketch.UseCases` |");
        md.Should().NotContain("Fitting");
    }

    [Fact]
    public async Task A_ports_real_adapters_are_offered_in_the_order_drawn()
    {
        var h = await Harness.WithExample();
        var port = h.Port("For storing projects").Id;
        h.Tasks.RenameAdapter(h.Tasks.AddAdapter(port), "Download");

        var md = await Plan(h);

        md.Should().Contain("On For storing projects, the real adapters are offered in this order: Browser Files, then Download.");
    }

    [Fact]
    public async Task Two_adapters_on_a_port_can_share_a_Seam()
    {
        var h = await WithDeepAutosave();
        var other = h.Tasks.AddAdapter(h.Port("For autosaving").Id);
        h.Tasks.RenameAdapter(other, "Local Storage");
        h.Tasks.SetAdapterSeam(other, "Browser Storage");

        var md = await Plan(h);

        md.Should().Contain("| For autosaving | IndexedDB, Local Storage | `BrowserStorage` |");
    }

    [Theory]
    [InlineData("42", "usable code name")]
    [InlineData("For copying", "interface called 'ForCopying'")]
    [InlineData("For storing projects via browser files", "class called 'ForStoringProjectsViaBrowserFiles'")]
    public async Task A_Seam_that_cant_be_named_blocks_the_plan(string seam, string problem)
    {
        var h = await Harness.WithExample();
        h.Tasks.SetAdapterSeam(h.Adapter("IndexedDB").Id, seam);

        var outcome = await h.Tasks.ExportPlan();

        outcome.Succeeded.Should().BeFalse();
        outcome.Problems.Should().Contain(p => p.Contains(problem));
    }

    [Fact]
    public async Task Ports_not_named_for_their_purpose_are_warned_about_in_one_line()
    {
        var h = await Harness.WithExample();
        h.Tasks.RenamePort(h.Port("For storing projects").Id, "Projects");
        h.Tasks.RenamePort(h.Port("For autosaving").Id, "Autosave");
        h.Tasks.RenamePort(h.Port("For copying").Id, "Clipboard");

        var outcome = await h.Tasks.ExportPlan();

        outcome.Succeeded.Should().BeTrue();
        outcome.Warnings.Should().ContainSingle(w => w.Contains("For …")).Which.Should()
            .Contain("Projects, Autosave and Clipboard")
            .And.NotContain("User");
    }

    [Fact]
    public async Task Ports_named_for_their_purpose_get_no_such_warning()
    {
        var h = await Harness.WithExample();

        var outcome = await h.Tasks.ExportPlan();

        outcome.Succeeded.Should().BeTrue();
        outcome.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task The_same_Seam_on_two_ports_blocks_the_plan()
    {
        var h = await WithDeepAutosave();
        h.Tasks.SetAdapterSeam(h.Adapter("SVG Files").Id, "Browser Storage");

        var outcome = await h.Tasks.ExportPlan();

        outcome.Problems.Should().Contain(p => p.Contains("'BrowserStorage'") && p.Contains("more than one port"));
    }
}
