using System.Text.RegularExpressions;

namespace Motif.Sketch;

/// <summary>
/// The plan's opinions, hard-coded: the core (Domain and UseCases) shares the root namespace with
/// the port interfaces; each port's real and simulated adapters share one project under Adapters/;
/// the User port's one runnable adapter is the App; fakes share one project; checks and driving-port
/// fakes are the Specs; every port gets a fake or checks. An adapter with a Seam is deep: its Seam
/// gets a real and a fake Fitting, and its port needs no fake of its own.
/// </summary>
public static partial class SolutionPlanner
{
    public const string TargetFramework = "net10.0";

    /// <summary>The xunit template's packages (coverlet.collector included) plus the assertion library.</summary>
    public static readonly IReadOnlyList<string> SpecPackages =
        ["Microsoft.NET.Test.Sdk", "xunit", "xunit.runner.visualstudio", "coverlet.collector", "AwesomeAssertions"];

    /// <summary>Adapters in a Blazor WebAssembly solution reach the browser through JS interop.</summary>
    public static readonly IReadOnlyList<string> BrowserAdapterPackages = ["Microsoft.JSInterop"];

    /// <summary>
    /// Ports can't take these names: each is a project or a namespace beside the port interfaces, and
    /// "Application" would hide the UI framework's Application class from the App.
    /// </summary>
    public static readonly IReadOnlyList<string> ReservedNames =
        ["Domain", "UseCases", "Application", "App", "Adapters", "Fakes", "Specs"];

    public static SolutionPlan Plan(Project project)
    {
        var problems = new List<string>();
        var warnings = new List<string>();
        var system = project.ContextDiagram.System;
        var sketch = project.Sketch ?? throw new InvalidOperationException("The plan needs a sketch.");
        var s = CodeName.Root(system.CodeRoot ?? system.Name);
        if (!CodeName.IsValid(s))
            problems.Add(system.CodeRoot is { } root
                ? $"The code root '{root}' doesn't make a usable solution name."
                : $"The system name '{system.Name}' doesn't make a usable solution name. Give the system a code root.");

        var ports = sketch.Ports.OrderBy(p => p.Side).ToList();
        var userRunnable = sketch.UserPort.Adapters.Where(IsRunnable).ToList();
        var blazor = userRunnable.Any(a => TemplateFor(Side.Driving, a.Technology, false) == "blazorwasm");
        var planned = ports.ToDictionary(p => p, p => AdaptersFor(project, p, s, userRunnable.Count, problems));

        var domain = new PlannedProject($"{s}.Domain", $"{s}/{s}.Domain", "classlib", s, ProjectRole.Domain, [], []);
        var useCases = new PlannedProject($"{s}.UseCases", $"{s}/{s}.UseCases", "classlib", s, ProjectRole.UseCases, [domain.Name], []);
        var projects = new List<PlannedProject> { domain, useCases };

        foreach (var (port, adapters) in planned)
        {
            var plugged = port.Adapters.Zip(adapters).Where(x => IsRunnable(x.First)).ToList();
            if (port.IsUser)
            {
                projects.AddRange(plugged.Select(x => new PlannedProject(
                    x.Second.Project, x.Second.Project, TemplateFor(Side.Driving, x.First.Technology, blazor),
                    x.Second.Project, ProjectRole.App, [useCases.Name], [])));
            }
            else if (plugged.Count > 0)
            {
                var templates = plugged.Select(x => TemplateFor(port.Side, x.First.Technology, blazor)).Distinct().ToList();
                if (templates.Count > 1)
                    problems.Add($"The adapters on port '{port.Name}' share a project but need different project templates ({string.Join(", ", templates)}).");
                var name = plugged[0].Second.Project;
                var packages = templates[0] == "razorclasslib" ? BrowserAdapterPackages : [];
                projects.Add(new(name, $"Adapters/{name}", templates[0], $"{s}.Adapters", ProjectRole.Adapters, [useCases.Name], packages));
            }
        }

        var seams = SeamsFor(ports, planned, projects, problems);
        // The fakes implement the Seams, and the specs run the real adapters over them.
        var seamProjects = seams.Select(x => x.Project).Distinct().ToList();
        var specsReferences = new List<string> { useCases.Name };
        if (ports.Any(p => p.Side == Side.Driven))
        {
            projects.Add(new($"{s}.Fakes", $"Checks/{s}.Fakes", "classlib", $"{s}.Fakes", ProjectRole.Fakes,
                [useCases.Name, .. seamProjects], []));
            specsReferences.Add($"{s}.Fakes");
        }
        projects.Add(new($"{s}.Specs", $"Checks/{s}.Specs", "xunit", $"{s}.Specs", ProjectRole.Specs,
            [.. specsReferences, .. seamProjects], SpecPackages));

        var appNames = projects.Where(p => p.Role == ProjectRole.App && userRunnable.Count > 1).Select(p => p.Name[(s.Length + 1)..]);
        CheckNames(s, ports, planned, seams, [.. ReservedNames, .. appNames, .. s.Split('.'), s.Replace(".", "")], problems);
        foreach (var clash in projects.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            problems.Add($"More than one project would be called '{clash.Key}'. Rename a port or adapter.");

        var unplaced = project.Unplaced;
        if (unplaced.Count > 0)
            warnings.Add(unplaced.Count == 1
                ? $"'{unplaced[0].Name}' isn't placed. It's listed under \"Not placed\" in the plan."
                : $"{unplaced.Count} items aren't placed. They're listed under \"Not placed\" in the plan.");
        foreach (var adapter in sketch.Adapters.Where(a => a.SourceDeleted))
            warnings.Add($"'{adapter.Name}' stood for a context diagram element that has been deleted.");
        foreach (var port in ports.Where(IsDeep))
            foreach (var fake in port.Adapters.Where(a => a.Kind == AdapterKind.Fake))
                warnings.Add($"'{fake.Name}' fakes the whole '{port.Name}' port, but its adapters have Seams, so their fake Fittings stand in for them instead.");

        var plannedPorts = ports.Select(p => new PlannedPort(
            p.Name, p.Description, p.Side, p.IsUser, InterfacesFor(p), p.IsUser ? "" : "Ports", planned[p])).ToList();

        return new SolutionPlan(s, project.Title, system.Name, system.Description, system.Technology,
            projects, plannedPorts, seams, unplaced, warnings, problems);
    }

    /// <summary>The User port is two interfaces (tasks and queries); every other port is one.</summary>
    public static IReadOnlyList<string> InterfacesFor(Port port) =>
        port.IsUser ? ["UserTasks", "UserQueries"] : [CodeName.From(port.Name)];

    /// <summary>
    /// A real or simulated adapter is named for its port, then its way, taken from the adapter's name
    /// ("Browser" on port "For copying" is ForCopyingViaBrowser). A fake keeps the extra first
    /// (FakeForCopying): it's a Fitting, not an adapter. When the name only repeats the port's, or is
    /// empty, the technology stands in, and failing that the kind. Null when a real adapter has neither.
    /// </summary>
    public static string? ClassFor(string portCode, string adapterCode, string? technology, AdapterKind kind)
    {
        var way =
            CodeName.IsValid(adapterCode) && !adapterCode.Equals(portCode, StringComparison.OrdinalIgnoreCase) ? adapterCode
            : Technology(technology) is { Length: > 0 } tech ? tech
            : kind != AdapterKind.Real ? kind.ToString()
            : null;
        if (way is null) return null;
        if (kind == AdapterKind.Fake)
            return way.EndsWith(portCode, StringComparison.Ordinal) ? way : way + portCode;
        var via = portCode + "Via";
        return way.StartsWith(via, StringComparison.Ordinal) && way.Length > via.Length ? way : via + way;
    }

    /// <summary>A technology as a code name, without its brackets: "Folder picker (JS interop)" is FolderPicker.</summary>
    private static string Technology(string? technology)
    {
        var code = CodeName.From(Brackets().Replace(technology ?? "", " "));
        return CodeName.IsValid(code) ? code : "";
    }

    /// <summary>A driven port whose every runnable adapter has a Seam, so it needs no fake of its own.</summary>
    private static bool IsDeep(Port port)
    {
        var runnable = port.Adapters.Where(IsRunnable).ToList();
        return port.Side == Side.Driven && runnable.Count > 0 && runnable.All(a => a.Seam is not null);
    }

    /// <summary>
    /// Each Seam once per port, with the adapters that share it. Its real Fitting is named for its
    /// technology: JS interop in a browser app (JsInteropFolder), otherwise the adapter's technology.
    /// </summary>
    private static List<PlannedSeam> SeamsFor(
        List<Port> ports, Dictionary<Port, List<PlannedAdapter>> planned, List<PlannedProject> projects,
        List<string> problems)
    {
        var seams = new List<PlannedSeam>();
        foreach (var port in ports.Where(p => p.Side == Side.Driven))
        {
            var deep = port.Adapters.Zip(planned[port]).Where(x => IsRunnable(x.First) && x.First.Seam is not null);
            foreach (var group in deep.GroupBy(x => CodeName.From(x.First.Seam), StringComparer.Ordinal))
            {
                var (adapter, plannedAdapter) = group.First();
                if (!CodeName.IsValid(group.Key))
                {
                    problems.Add($"The Seam '{adapter.Seam}' on '{adapter.Name}' doesn't make a usable code name.");
                    continue;
                }
                var project = projects.First(p => p.Name == plannedAdapter.Project);
                var technology = project.Template == "razorclasslib" ? "JsInterop" : Technology(adapter.Technology);
                if (technology.Length == 0)
                    problems.Add($"The adapter '{adapter.Name}' on port '{port.Name}' needs a technology, to name its Seam's real Fitting.");
                seams.Add(new PlannedSeam(group.Key, port.Name, [.. group.Select(x => x.First.Name)], project.Name,
                    technology + group.Key, $"Fake{group.Key}", CodeName.From(port.Name)));
            }
        }
        foreach (var clash in seams.GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            problems.Add($"Adapters on more than one port reach through a Seam called '{clash.Key}'. Give each port's Seam its own name.");
        return seams;
    }

    private static bool IsRunnable(Adapter adapter) => adapter.Kind is AdapterKind.Real or AdapterKind.Simulated;

    private static List<PlannedAdapter> AdaptersFor(Project project, Port port, string s, int runnable, List<string> problems)
    {
        var portCode = CodeName.From(port.Name);
        var result = new List<PlannedAdapter>();

        foreach (var adapter in port.Adapters)
        {
            var code = CodeName.From(adapter.Name);
            if (!CodeName.IsValid(code))
                problems.Add($"The adapter name '{adapter.Name}' on port '{port.Name}' doesn't make a usable code name.");

            var names = adapter.Sources.Select(project.ContextDiagram.Find).OfType<Element>().Select(e => e.Name).ToList();
            var source = names.Count > 0 ? $"Context diagram: {string.Join(", ", names)}" : "Added in the sketch";
            var cls = port.Side == Side.Driven ? ClassFor(portCode, code, adapter.Technology, adapter.Kind) : null;
            if (port.Side == Side.Driven && cls is null)
                problems.Add($"The adapter '{adapter.Name}' on port '{port.Name}' needs a name of its own or a technology, to name its class.");

            var (proj, folder) = adapter.Kind switch
            {
                AdapterKind.Checks or AdapterKind.Fake when port.Side == Side.Driving => ($"{s}.Specs", ""),
                AdapterKind.Fake => ($"{s}.Fakes", portCode),
                _ when port.IsUser => (runnable == 1 ? $"{s}.App" : $"{s}.{code}", ""),
                _ => ($"{s}.{portCode}", ""),
            };
            result.Add(new PlannedAdapter(adapter.Name, adapter.Kind, adapter.Technology, adapter.Description,
                proj, cls, folder, source, AutoAdded: false));
        }

        var driving = port.Side == Side.Driving;
        var covered = driving
            ? port.Adapters.Any(a => a.Kind is AdapterKind.Checks or AdapterKind.Fake)
            : port.Adapters.Any(a => a.Kind == AdapterKind.Fake) || IsDeep(port);
        if (!covered)
        {
            result.Add(new PlannedAdapter(
                driving ? Project.SpecsName : $"Fake {port.Name}",
                driving ? AdapterKind.Checks : AdapterKind.Fake,
                driving ? Project.SpecsTechnology : null,
                driving
                    ? $"Specs that drive the application through the {port.Name} port, using the fake driven adapters."
                    : $"In-memory stand-in for the {port.Name} port, for fast specs.",
                driving ? $"{s}.Specs" : $"{s}.Fakes",
                driving ? null : ClassFor(portCode, "Fake", null, AdapterKind.Fake),
                driving ? "" : portCode,
                "Added by the plan: every port gets a fake or checks", AutoAdded: true));
        }
        return result;
    }

    private static void CheckNames(
        string s, List<Port> ports, Dictionary<Port, List<PlannedAdapter>> planned, List<PlannedSeam> seams,
        IReadOnlyList<string> reserved, List<string> problems)
    {
        foreach (var port in ports.Where(p => !CodeName.IsValid(CodeName.From(p.Name))))
            problems.Add($"The port name '{port.Name}' doesn't make a usable code name.");

        var interfaces = ports.SelectMany(InterfacesFor).Where(n => n.Length > 0).Append($"{s.Replace(".", "")}Application").ToList();
        foreach (var clash in interfaces.GroupBy(n => n, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            problems.Add($"More than one port would make an interface called '{clash.Key}'.");

        foreach (var port in ports.Where(p => !p.IsUser && reserved.Contains(CodeName.From(p.Name), StringComparer.OrdinalIgnoreCase)))
            problems.Add($"A port can't be called '{port.Name}': that name is taken by a project or namespace in the plan.");

        var classes = planned.Values.SelectMany(a => a).Where(a => a.Class is not null).ToList();
        // Every type in the adapters' namespace, and every one in the fakes'.
        var types = classes.Select(a => (Fake: a.Project == $"{s}.Fakes", Name: a.Class!))
            .Concat(seams.SelectMany(x => new[] { (Fake: false, x.Name), (Fake: false, Name: x.RealFitting), (Fake: true, Name: x.FakeFitting) }))
            .ToList();
        foreach (var clash in types.GroupBy(t => t, (key, g) => (key.Name, Count: g.Count())).Where(g => g.Count > 1))
            problems.Add($"More than one adapter or Seam would make a class called '{clash.Name}'. Rename an adapter or a Seam.");

        foreach (var adapter in classes.Where(a => interfaces.Contains(a.Class!, StringComparer.OrdinalIgnoreCase)))
            problems.Add($"The adapter '{adapter.Name}' would make a class called '{adapter.Class}', the same as a port interface. Rename it or give it a technology.");
        foreach (var seam in seams.Where(x => interfaces.Contains(x.Name, StringComparer.OrdinalIgnoreCase)))
            problems.Add($"The Seam on '{seam.Adapters[0]}' would make an interface called '{seam.Name}', the same as a port's. Rename the Seam.");
    }

    /// <summary>The project template an adapter needs, from its technology.</summary>
    public static string TemplateFor(Side side, string? technology, bool blazorSolution)
    {
        var tech = technology?.ToLowerInvariant() ?? "";
        if (side == Side.Driving)
        {
            if (tech.Contains("blazor")) return "blazorwasm";
            if (tech.Contains("wpf")) return "wpf";
            if (tech.Contains("cli") || tech.Contains("console")) return "console";
            if (tech.Contains("web") || tech.Contains("http")) return "web";
            return "classlib";
        }
        // In a browser app, a driven adapter reaches the browser through a JS module it ships as a static web asset.
        return blazorSolution ? "razorclasslib" : "classlib";
    }

    [GeneratedRegex(@"\([^)]*\)")]
    private static partial Regex Brackets();
}
