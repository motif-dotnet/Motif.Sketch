namespace Motif.Sketch.Specs;

/// <summary>
/// Which project template a driven adapter gets in a browser app: a Razor class library only when it
/// reaches the browser, since that's the only kind that ships a JS module as a static web asset.
/// </summary>
public sealed class PlanningBrowserAdapters
{
    [Theory]
    [InlineData("Folder picker (JS interop)")]
    [InlineData("Browser storage")]
    [InlineData("Browser Files (File System Access API, download fallback)")]
    [InlineData("File System Access API, download fallback")]
    [InlineData("IndexedDB")]
    [InlineData("Browser Clipboard API (JS interop)")]
    public void A_driven_adapter_that_uses_the_browser_is_a_Razor_class_library(string technology)
    {
        SolutionPlanner.TemplateFor(Side.Driven, technology, blazorSolution: true).Should().Be("razorclasslib");
    }

    [Theory]
    [InlineData("Embedded resources")]
    [InlineData("raw.githubusercontent.com (HttpClient)")]
    [InlineData(null)]
    public void A_driven_adapter_that_doesnt_use_the_browser_is_a_plain_class_library(string? technology)
    {
        SolutionPlanner.TemplateFor(Side.Driven, technology, blazorSolution: true).Should().Be("classlib");
    }

    [Fact]
    public async Task A_port_whose_adapters_differ_gets_a_Razor_class_library_when_any_uses_the_browser()
    {
        var h = await Harness.WithExample();
        var other = h.Tasks.AddAdapter(h.Port("For autosaving").Id);
        h.Tasks.RenameAdapter(other, "Memory");
        h.Tasks.SetAdapterTechnology(other, "In memory");

        (await h.Tasks.ExportPlan()).Problems.Should().BeEmpty();
        h.PlanExports.Written.Single().Markdown.Should().Contain(
            "| `Motif.Sketch.ForAutosaving` | `Adapters/Motif.Sketch.ForAutosaving/` | `razorclasslib` |");
    }

    [Fact]
    public void Outside_a_browser_app_every_driven_adapter_is_a_plain_class_library()
    {
        SolutionPlanner.TemplateFor(Side.Driven, "Browser storage", blazorSolution: false).Should().Be("classlib");
    }
}
