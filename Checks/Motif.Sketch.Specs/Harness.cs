using Motif.Sketch.Adapters;
using Motif.Sketch.Fakes;

namespace Motif.Sketch.Specs;

/// <summary>The application wired to every fake driven adapter, driven only through the User port.</summary>
public sealed class Harness
{
    public FakeProjectFiles ProjectFiles { get; }
    public FakeBrowserStorage BrowserStorage { get; } = new();
    public FakeForExportingDiagrams DiagramExports { get; } = new();
    public FakeForExportingThePlan PlanExports { get; } = new();
    public FakeForCopying Clipboard { get; } = new();
    public FakeGitHub GitHub { get; } = new();
    public FakeGlossaryFiles GlossaryFiles { get; } = new();

    public MotifSketchApplication App { get; }
    public UserTasks Tasks => App;
    public UserQueries Queries => App;

    public Harness() : this(new FakeProjectFiles())
    {
    }

    /// <summary>An app on these project files, such as the last visit's after a refresh.</summary>
    public Harness(FakeProjectFiles projectFiles)
    {
        ProjectFiles = projectFiles;
        App = new MotifSketchApplication(
            new ForStoringProjectsViaBrowserFiles(ProjectFiles),
            new ForAutosavingViaIndexedDB(BrowserStorage),
            DiagramExports,
            PlanExports,
            Clipboard,
            new ForOpeningReposViaGitHub(GitHub),
            new ForExportingTheGlossaryViaBrowserFiles(GlossaryFiles));
    }

    /// <summary>A started app with a blank project.</summary>
    public static async Task<Harness> Started(string? fragment = null)
    {
        var harness = new Harness();
        await harness.Tasks.Start(fragment);
        return harness;
    }

    /// <summary>A started app with the bundled example open.</summary>
    public static async Task<Harness> WithExample()
    {
        var harness = await Started();
        await harness.Tasks.OpenExample();
        return harness;
    }

    /// <summary>Takes every Seam off the sketch, leaving its adapters thin.</summary>
    public void ClearSeams()
    {
        foreach (var adapter in Adapters.Where(a => a.Seam is not null).ToList())
            Tasks.SetAdapterSeam(adapter.Id, null);
    }

    public ElementView Element(string name) => Queries.Context.Elements.Single(e => e.Name == name);

    public ElementView System => Queries.Context.Elements.Single(e => e.Type == ElementType.System);

    public PortShape Port(string name) => Queries.Sketch!.Ports.Single(p => p.Name == name);

    public AdapterShape Adapter(string name) => Queries.Sketch!.Ports.SelectMany(p => p.Adapters).Single(a => a.Name == name);

    public IEnumerable<AdapterShape> Adapters => Queries.Sketch!.Ports.SelectMany(p => p.Adapters);

    public IEnumerable<string> UnplacedNames => Queries.Unplaced.Select(u => u.Name);
}
