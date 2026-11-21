# Motif Sketch: solution plan

*Exported by Motif Sketch from the project "Motif Sketch". Stubs only: no functionality.*

## 1. Overview

- **System**: Motif Sketch (Blazor WebAssembly (GitHub Pages))
- **Purpose**: Sketch a context diagram, turn it into a ports-and-adapters sketch, then export the diagrams and a solution plan
- **Solution**: `Motif.Sketch.sln`, targeting `net10.0`
- **Architecture**: ports and adapters. The application core (Domain and UseCases) knows nothing about the outside world; every external concern sits behind a port and is reached through an adapter.
- **Ports**: 1 driving, 7 driven

## 2. Solution and project layout

Projects are grouped by role, and the solution folders in the `.sln` mirror the folders on disk. There are no `src/` or `tests/` folders.

```
Motif.Sketch.sln
  Motif.Sketch.App/
  Motif.Sketch/
    Motif.Sketch.Domain/
    Motif.Sketch.UseCases/
  Adapters/
    Motif.Sketch.ForStoringProjects/
    Motif.Sketch.ForAutosaving/
    Motif.Sketch.ForExportingDiagrams/
    Motif.Sketch.ForExportingThePlan/
    Motif.Sketch.ForCopying/
    Motif.Sketch.ForOpeningRepos/
    Motif.Sketch.ForExportingTheGlossary/
  Checks/
    Motif.Sketch.Fakes/
    Motif.Sketch.Specs/
```

| Project | Folder | Template | Namespace | Role |
|---|---|---|---|---|
| `Motif.Sketch.App` | `Motif.Sketch.App/` | `blazorwasm` | `Motif.Sketch.App` | The app: runnable adapter on the User port |
| `Motif.Sketch.Domain` | `Motif.Sketch/Motif.Sketch.Domain/` | `classlib` | `Motif.Sketch` | Domain-specific business rules |
| `Motif.Sketch.UseCases` | `Motif.Sketch/Motif.Sketch.UseCases/` | `classlib` | `Motif.Sketch` | Application rules (use cases) and all port interfaces |
| `Motif.Sketch.ForStoringProjects` | `Adapters/Motif.Sketch.ForStoringProjects/` | `razorclasslib` | `Motif.Sketch.Adapters` | Adapters on the For storing projects port |
| `Motif.Sketch.ForAutosaving` | `Adapters/Motif.Sketch.ForAutosaving/` | `razorclasslib` | `Motif.Sketch.Adapters` | Adapters on the For autosaving port |
| `Motif.Sketch.ForExportingDiagrams` | `Adapters/Motif.Sketch.ForExportingDiagrams/` | `razorclasslib` | `Motif.Sketch.Adapters` | Adapters on the For exporting diagrams port |
| `Motif.Sketch.ForExportingThePlan` | `Adapters/Motif.Sketch.ForExportingThePlan/` | `razorclasslib` | `Motif.Sketch.Adapters` | Adapters on the For exporting the plan port |
| `Motif.Sketch.ForCopying` | `Adapters/Motif.Sketch.ForCopying/` | `razorclasslib` | `Motif.Sketch.Adapters` | Adapters on the For copying port |
| `Motif.Sketch.ForOpeningRepos` | `Adapters/Motif.Sketch.ForOpeningRepos/` | `razorclasslib` | `Motif.Sketch.Adapters` | Adapters on the For opening repos port |
| `Motif.Sketch.ForExportingTheGlossary` | `Adapters/Motif.Sketch.ForExportingTheGlossary/` | `razorclasslib` | `Motif.Sketch.Adapters` | Adapters on the For exporting the glossary port |
| `Motif.Sketch.Fakes` | `Checks/Motif.Sketch.Fakes/` | `classlib` | `Motif.Sketch.Fakes` | Fake driven adapters, for specs |
| `Motif.Sketch.Specs` | `Checks/Motif.Sketch.Specs/` | `xunit` | `Motif.Sketch.Specs` | Specs: the checks on every driving port |

## 3. Project references (dependency rules)

| Project | References |
|---|---|
| `Motif.Sketch.App` | `Motif.Sketch.UseCases` |
| `Motif.Sketch.Domain` | nothing |
| `Motif.Sketch.UseCases` | `Motif.Sketch.Domain` |
| `Motif.Sketch.ForStoringProjects` | `Motif.Sketch.UseCases` |
| `Motif.Sketch.ForAutosaving` | `Motif.Sketch.UseCases` |
| `Motif.Sketch.ForExportingDiagrams` | `Motif.Sketch.UseCases` |
| `Motif.Sketch.ForExportingThePlan` | `Motif.Sketch.UseCases` |
| `Motif.Sketch.ForCopying` | `Motif.Sketch.UseCases` |
| `Motif.Sketch.ForOpeningRepos` | `Motif.Sketch.UseCases` |
| `Motif.Sketch.ForExportingTheGlossary` | `Motif.Sketch.UseCases` |
| `Motif.Sketch.Fakes` | `Motif.Sketch.UseCases`, `Motif.Sketch.ForStoringProjects`, `Motif.Sketch.ForAutosaving`, `Motif.Sketch.ForExportingTheGlossary` |
| `Motif.Sketch.Specs` | `Motif.Sketch.UseCases`, `Motif.Sketch.Fakes`, `Motif.Sketch.ForStoringProjects`, `Motif.Sketch.ForAutosaving`, `Motif.Sketch.ForExportingTheGlossary` |

Rules:

- `Motif.Sketch.Domain` references nothing.
- `Motif.Sketch.UseCases` references `Motif.Sketch.Domain` only.
- Every adapter project references `Motif.Sketch.UseCases` only. **Adapters never reference each other.**
- The fakes project also references the adapter projects whose Seams it fakes, and the specs project references them too, so specs run the real adapters over fake Fittings.
- These references are the dependency rule. Don't add others.

## 4. Namespaces

- `Motif.Sketch.Domain` and `Motif.Sketch.UseCases` both use the plain `Motif.Sketch` namespace, so the port interfaces are `Motif.Sketch.UserTasks` and so on.
- Every other project uses the namespace in section 2. Real and simulated adapters share one namespace, and so do the fakes.
- Folders inside a project don't change its namespace.
- Set `RootNamespace` in each `.csproj` to its namespace, so new files created in an IDE get it.
- Every namespace sits inside `Motif.Sketch`, so adapters, fakes and specs see the port interfaces without a `using`.

## 5. Ports

All port interfaces live in `Motif.Sketch.UseCases`, namespace `Motif.Sketch`, one file each. Interfaces take the plain name of the port, **with no `I` prefix**. Leave them empty: their members are design work still to do.

```
Motif.Sketch.UseCases/
  UserTasks.cs
  UserQueries.cs
  MotifSketchApplication.cs
  Ports/
    ForStoringProjects.cs
    ForAutosaving.cs
    ForExportingDiagrams.cs
    ForExportingThePlan.cs
    ForCopying.cs
    ForOpeningRepos.cs
    ForExportingTheGlossary.cs
```

### User (driving)

The User port: how people (and anything acting for them) use the application. It has two halves, commands and queries.

`UserTasks.cs`:

```csharp
namespace Motif.Sketch;

public interface UserTasks
{
}
```

`UserQueries.cs`:

```csharp
namespace Motif.Sketch;

public interface UserQueries
{
}
```

### For storing projects (driven)

Opens and saves project files

`Ports/ForStoringProjects.cs`:

```csharp
namespace Motif.Sketch;

public interface ForStoringProjects
{
}
```

### For autosaving (driven)

Keeps the work in progress and its file handle between visits

`Ports/ForAutosaving.cs`:

```csharp
namespace Motif.Sketch;

public interface ForAutosaving
{
}
```

### For exporting diagrams (driven)

Writes the context diagram and the sketch as pictures

`Ports/ForExportingDiagrams.cs`:

```csharp
namespace Motif.Sketch;

public interface ForExportingDiagrams
{
}
```

### For exporting the plan (driven)

Hands the solution stub on to be built; v2 may add a GitHub adapter that inflates it in a repo

`Ports/ForExportingThePlan.cs`:

```csharp
namespace Motif.Sketch;

public interface ForExportingThePlan
{
}
```

### For copying (driven)

Takes the snapshot link when the Architect copies it

`Ports/ForCopying.cs`:

```csharp
namespace Motif.Sketch;

public interface ForCopying
{
}
```

### For opening repos (driven)

Reads a public GitHub repo's sketch from its default branch

`Ports/ForOpeningRepos.cs`:

```csharp
namespace Motif.Sketch;

public interface ForOpeningRepos
{
}
```

### For exporting the glossary (driven)

Opens the project's glossary and writes it back with the sketch's new terms

`Ports/ForExportingTheGlossary.cs`:

```csharp
namespace Motif.Sketch;

public interface ForExportingTheGlossary
{
}
```

Also add one stub class, `MotifSketchApplication` in `Motif.Sketch.UseCases` (namespace `Motif.Sketch`), implementing the driving interfaces (`UserTasks`, `UserQueries`). It is the application itself; its constructor will later take the driven ports.

## 6. Adapters

Kinds: **Real** is the production adapter; **Fake** is a simple in-process stand-in for fast specs; **Simulated** is a higher-fidelity stand-in for people running the whole app locally; **Checks** drive the application through a driving port and check what it does.

| Port | Adapter | Kind | Technology | Project | Class | Source |
|---|---|---|---|---|---|---|
| User | Web App | Real | Blazor WebAssembly UI | `Motif.Sketch.App` | - | Context diagram: Architect, Viewer |
| User | Specs | Checks | xUnit | `Motif.Sketch.Specs` | - | Added in the sketch |
| For storing projects | Browser Files | Real | File System Access API, download fallback | `Motif.Sketch.ForStoringProjects` | `ForStoringProjectsViaBrowserFiles` | Context diagram: Project Files |
| For autosaving | IndexedDB | Real | Browser storage | `Motif.Sketch.ForAutosaving` | `ForAutosavingViaIndexedDB` | Context diagram: Browser Storage |
| For exporting diagrams | SVG Files | Real | Browser Files (File System Access API, download fallback) | `Motif.Sketch.ForExportingDiagrams` | `ForExportingDiagramsViaSvgFiles` | Context diagram: Diagram Exports |
| For exporting diagrams | Fake for exporting diagrams | Fake | - | `Motif.Sketch.Fakes` (`ForExportingDiagrams/`) | `FakeForExportingDiagrams` | Added in the sketch |
| For exporting the plan | Markdown File | Real | Browser Files (File System Access API, download fallback) | `Motif.Sketch.ForExportingThePlan` | `ForExportingThePlanViaMarkdownFile` | Context diagram: Solution Plans |
| For exporting the plan | Fake for exporting the plan | Fake | - | `Motif.Sketch.Fakes` (`ForExportingThePlan/`) | `FakeForExportingThePlan` | Added in the sketch |
| For copying | Browser Clipboard | Real | Browser Clipboard API (JS interop) | `Motif.Sketch.ForCopying` | `ForCopyingViaBrowserClipboard` | Context diagram: Clipboard |
| For copying | Fake for copying | Fake | - | `Motif.Sketch.Fakes` (`ForCopying/`) | `FakeForCopying` | Added in the sketch |
| For opening repos | GitHub | Real | raw.githubusercontent.com (HttpClient) | `Motif.Sketch.ForOpeningRepos` | `ForOpeningReposViaGitHub` | Context diagram: GitHub |
| For opening repos | Fake for opening repos | Fake | - | `Motif.Sketch.Fakes` (`ForOpeningRepos/`) | `FakeForOpeningRepos` | Added in the sketch |
| For exporting the glossary | Browser Files | Real | File System Access API, download fallback | `Motif.Sketch.ForExportingTheGlossary` | `ForExportingTheGlossaryViaBrowserFiles` | Context diagram: Glossary Files |

For each driven adapter, add the stub class named above implementing its port's interface, in its project's namespace and in the folder shown. For example:

```csharp
namespace Motif.Sketch.Adapters;

public sealed class ForStoringProjectsViaBrowserFiles : ForStoringProjects
{
}
```

Deep adapters: an adapter with a Seam keeps its own rules, and reaches the outside world through the Seam, an interface it owns. The Seam's Fitting makes one outside call per member, with no branches worth testing. Specs run the real adapter over its fake Fitting, so its port has no fake of its own. The Seam and its real Fitting go in the adapter's project and namespace; the fake Fitting goes in the fakes project, in the port's folder.

| Port | Adapters | Seam | Real Fitting | Fake Fitting |
|---|---|---|---|---|
| For storing projects | Browser Files | `ProjectFiles` | `JsInteropProjectFiles` | `FakeProjectFiles` (`ForStoringProjects/`) |
| For autosaving | IndexedDB | `BrowserStorage` | `JsInteropBrowserStorage` | `FakeBrowserStorage` (`ForAutosaving/`) |
| For exporting the glossary | Browser Files | `GlossaryFiles` | `JsInteropGlossaryFiles` | `FakeGlossaryFiles` (`ForExportingTheGlossary/`) |

For each Seam, add the empty interface, its real Fitting and its fake Fitting. Each adapter with a Seam takes it in its one constructor. For example:

```csharp
namespace Motif.Sketch.Adapters;

public interface ProjectFiles
{
}

public sealed class JsInteropProjectFiles : ProjectFiles
{
}

public sealed class ForStoringProjectsViaBrowserFiles(ProjectFiles projectFiles) : ForStoringProjects
{
}
```

```csharp
using Motif.Sketch.Adapters;

namespace Motif.Sketch.Fakes;

public sealed class FakeProjectFiles : ProjectFiles
{
}
```

Driving adapters (the app among them) keep their project template's default files. Checks and fakes on driving ports are the specs (section 7).

Adapter notes:

- **Web App** (`Motif.Sketch.App`): One app for the Architect (full editing) and the Viewer (read-only, opened from a snapshot link or a repo link)
- **Specs** (`Motif.Sketch.Specs`): Checks that drive the application through the User port, using the fake driven adapters
- **Browser Files** (`Motif.Sketch.ForStoringProjects`): Saved projects: one file per system holding the context diagram and, once started, its ports-and-adapters sketch
- **IndexedDB** (`Motif.Sketch.ForAutosaving`): Autosaved work in progress and its file handle, so a refresh or closed tab loses nothing and Save still goes to the same file
- **SVG Files** (`Motif.Sketch.ForExportingDiagrams`): The context diagram and the ports-and-adapters sketch as pictures
- **Markdown File** (`Motif.Sketch.ForExportingThePlan`): The solution stub plan a coding agent builds from
- **Browser Clipboard** (`Motif.Sketch.ForCopying`): Where Copy link puts a snapshot link, ready to paste
- **GitHub** (`Motif.Sketch.ForOpeningRepos`): Public repos, whose motif/ folder holds a project's sketch
- **Browser Files** (`Motif.Sketch.ForExportingTheGlossary`): The project's glossary, motif/glossary.md, which Export glossary adds the sketch's terms to

## 7. Fakes and specs

- `Motif.Sketch.Fakes` (`Checks/Motif.Sketch.Fakes/`): every fake Fitting, and every fake of a port whose adapters are their own Fittings, with a folder per port (`ForExportingDiagrams/`, `ForExportingThePlan/`, `ForCopying/`, `ForOpeningRepos/`, `ForStoringProjects/`, `ForAutosaving/`, `ForExportingTheGlossary/`). The namespace is flat: `Motif.Sketch.Fakes`.
- `Motif.Sketch.Specs` (`Checks/Motif.Sketch.Specs/`): xUnit. Packages: `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`, `coverlet.collector`, `AwesomeAssertions`. Keep the template's packages and add `AwesomeAssertions`.
  Add one placeholder spec class with a single passing `[Fact]`, so `dotnet test` runs. Specs wire the real adapters over their fake Fittings, and the other fakes, up by hand; there is no composition root yet.
- The browser adapters (`Motif.Sketch.ForStoringProjects`, `Motif.Sketch.ForAutosaving`, `Motif.Sketch.ForExportingDiagrams`, `Motif.Sketch.ForExportingThePlan`, `Motif.Sketch.ForCopying`, `Motif.Sketch.ForOpeningRepos`, `Motif.Sketch.ForExportingTheGlossary`) also take `Microsoft.JSInterop`: each reaches the browser through a small JS module it ships as a static web asset.
- Use the latest stable package versions. Use AwesomeAssertions (`.Should()` style) in specs.

## 8. Not placed

Every element of the context diagram is on the sketch.

## 9. Build this

Instructions for the coding agent:

1. Create `Motif.Sketch.sln` (classic `.sln` format: `dotnet new sln --format sln`).
2. Create every project in section 2 with its template, in its folder, and add it to the solution in the matching solution folder (`dotnet sln add <project> --solution-folder <folder>`, or `--in-root` for top-level projects). Set each project's `RootNamespace`.
3. Delete template placeholder files (`Class1.cs`, `UnitTest1.cs`, and the `razorclasslib` samples (`Component1.razor`, `ExampleJsInterop.cs`, the files in `wwwroot/`)), except `Class1.cs` in `Motif.Sketch.Domain`: keep it, with its namespace changed to `Motif.Sketch`, to show the namespace choice.
4. Add exactly the project references in section 3, and the packages in section 7.
5. Add a root `.editorconfig` with a naming rule that says interfaces are plain PascalCase, so the IDE doesn't ask for an `I` prefix:

   ```ini
   root = true

   [*.cs]
   dotnet_naming_rule.interfaces_no_prefix.symbols = interfaces
   dotnet_naming_rule.interfaces_no_prefix.style = pascal_case
   dotnet_naming_rule.interfaces_no_prefix.severity = warning

   dotnet_naming_symbols.interfaces.applicable_kinds = interface
   dotnet_naming_symbols.interfaces.applicable_accessibilities = *

   dotnet_naming_style.pascal_case.capitalization = pascal_case
   ```

6. Add the port interfaces and the application stub from section 5, and the adapter stub classes, and each Seam with its Fittings, from section 6.
7. Add the placeholder spec from section 7.
8. Check that `dotnet build` and `dotnet test` both succeed.

Ground rules:

- **Stubs only.** No functionality, no interface members, no wiring.
- **No composition root** (no host project, no dependency injection setup) in this version.
- **No `I` prefix** on interfaces. An adapter is named for its port, then its way (`ForStoringViaFileSystem`); other implementations carry the meaningful extra (`FileSystemFolder`), never `Impl`.
- **Two-letter acronyms stay in capitals** in code names (`IndexedDB`, `UI`).
- Don't add projects, references or packages the plan doesn't list.
