using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Motif.Suite;

namespace Motif.Sketch.App;

/// <summary>The Web App: the app bar, the three layouts, keys, and every project-wide command.</summary>
public partial class Shell
{
    private const string WelcomeDismissed = "motif-welcome-dismissed";

    private DotNetObjectReference<Shell>? self;
    private bool started;
    private string? menu;
    private bool helpOpen;
    private bool renamingTitle;
    private string theme = "system";
    private bool phone, narrow;
    private string layout = "context";
    private double split = 50;
    private int fitKey;
    private bool welcomeDismissed;
    private bool viewingExample;
    private string active = "context";
    private ExportCheck? exportCheck;
    private bool selectSystem;
    private ContextPane? contextPane;
    private SketchPane? sketchPane;
    private ElementReference work, divider;
    private long? resizing;
    private string? manualLink;
    private bool glossaryDialog;

    private bool ReadOnlyUi => App.IsReadOnly || phone;

    /// <summary>Where a Viewer's project came from, for the read-only pill.</summary>
    private string ViewerSource => App.Repo is { } repo ? repo.ToString() : viewingExample ? "Example" : "Shared snapshot";
    private bool Editable => !ReadOnlyUi;

    private string EffectiveLayout => narrow && layout == "split" ? (active == "sketch" ? "sketch" : "context") : layout;

    private string ModeClass => (ReadOnlyUi ? "mode-viewer" : "mode-edit") + (narrow ? " narrow" : "") + (phone ? " phone" : "");

    /// <summary>The welcome card shows whenever editable work is empty, until it's dismissed for good.</summary>
    private bool ShowWelcome => started && Editable && App.IsEmpty && !welcomeDismissed;

    private string PageTitle =>
        (App.HasUnsavedChanges && App.Mode == AppMode.Architect ? "• " : "") + App.Title + " · Motif Sketch";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await Browser.Load();
            self = DotNetObjectReference.Create(this);
            theme = Browser.Theme();
            var viewport = Browser.WatchViewport(self);
            (phone, narrow) = (viewport[0], viewport[1]);
            Browser.ListenForKeys(self);
            Ui.Changed += Refresh;
            App.Changed += OnAppChanged;

            welcomeDismissed = Browser.Preference(WelcomeDismissed) == "1";
            // A phone can't edit, so empty work there would show nothing useful: it gets the example instead.
            var result = await App.Start(Browser.Fragment(), exampleIfEmpty: phone);
            started = true;
            viewingExample = result.StartedAs == StartedAs.Example;
            Ui.Problem(result.Problem);
            if (result.StartedAs == StartedAs.Fresh) selectSystem = !phone;
            Ui.Tell(result.Note);
            layout = App.HasSketch && !narrow ? "split" : "context";
            fitKey++;
            StateHasChanged();
            return;
        }

        if (selectSystem && contextPane is not null)
        {
            selectSystem = false;
            contextPane.Select("element", App.Context.Elements.Single(e => e.Type == ElementType.System).Id);
            contextPane.Rename();
        }
        if (renamingTitle && focusTitle)
        {
            focusTitle = false;
            Browser.Focus("#title-edit");
        }
    }

    private bool focusTitle;

    private void OnAppChanged()
    {
        exportCheck = null;
        Browser.SetUnsaved(App.HasUnsavedChanges && App.Mode == AppMode.Architect);
        Refresh();
    }

    private void Refresh() => InvokeAsync(StateHasChanged);

    [JSInvokable]
    public void OnViewport(bool isPhone, bool isNarrow)
    {
        (phone, narrow) = (isPhone, isNarrow);
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnKey(string key)
    {
        if (!started) return;
        switch (key)
        {
            case "Escape":
                if (Ui.Pending is not null) Ui.Answer(false);
                else if (glossaryDialog) glossaryDialog = false;
                else if (menu is not null || helpOpen) { menu = null; helpOpen = false; }
                else { contextPane?.Clear(); sketchPane?.Clear(); }
                break;
            case "Save" when Editable: await Save(); break;
            case "SaveAs" when Editable: await SaveAs(); break;
            case "Undo" when Editable: App.Undo(); break;
            case "Redo" when Editable: App.Redo(); break;
            case "Delete" when Editable:
                if (active == "sketch" && sketchPane is not null) await sketchPane.DeleteSelection();
                else contextPane?.DeleteSelection();
                break;
            case "Rename" when Editable:
                if (active == "sketch") sketchPane?.Rename(); else contextPane?.Rename();
                break;
            case "Layout1": SetLayout("context"); break;
            case "Layout2": SetLayout("split"); break;
            case "Layout3": SetLayout("sketch"); break;
        }
        StateHasChanged();
    }

    // ---- Layout --------------------------------------------------------------------------------

    private void SetLayout(string value)
    {
        if (value == "split" && narrow) value = active == "sketch" ? "sketch" : "context";
        if (value != "split") active = value;
        if (layout == value) return;
        layout = value;
        fitKey++;
    }

    private void StartResize(PointerEventArgs e)
    {
        resizing = e.PointerId;
        Browser.Capture(divider, e.PointerId);
    }

    private void Resize(PointerEventArgs e)
    {
        if (resizing != e.PointerId) return;
        var r = Browser.Rect(work);
        if (r[2] <= 0) return;
        split = Math.Clamp((e.ClientX - r[0]) / r[2] * 100, 20, 80);
    }

    private void EndResize(PointerEventArgs e)
    {
        if (resizing != e.PointerId) return;
        resizing = null;
        fitKey++;
    }

    // ---- Menus -------------------------------------------------------------------------------

    private void Toggle(string name)
    {
        menu = menu == name ? null : name;
        helpOpen = false;
    }

    private void ToggleHelp()
    {
        helpOpen = !helpOpen;
        menu = null;
    }

    private void OpenExportMenu()
    {
        exportCheck = App.CheckExport();
        Toggle("export");
    }

    private void CloseMenus() => menu = null;

    private void SetTheme(string value)
    {
        theme = value;
        Browser.SetTheme(value);
        menu = null;
    }

    private void StartTitleRename()
    {
        renamingTitle = true;
        focusTitle = true;
    }

    private void RenameTitle(ChangeEventArgs e)
    {
        renamingTitle = false;
        var title = e.Value?.ToString() ?? "";
        if (title.Trim() != App.Title) Ui.Run(() => App.RenameProject(title));
    }

    private void DismissWelcome()
    {
        welcomeDismissed = true;
        Browser.SetPreference(WelcomeDismissed, "1");
    }

    private void SketchStarted(Note started)
    {
        Ui.Tell(started);
        SetLayout(narrow ? "sketch" : "split");
        fitKey++;
    }

    // ---- Project commands ----------------------------------------------------------------------

    private async Task<bool> MayDiscard(string doing)
    {
        menu = null;
        if (!App.HasUnsavedChanges) return true;
        return await Ui.Confirm("Discard unsaved changes?",
            $"{doing} replaces \"{App.Title}\", which has changes that aren't saved to a file.", "Discard changes", "Keep working");
    }

    private async Task New()
    {
        if (!await MayDiscard("A new project")) return;
        await App.NewProject();
        layout = "context";
        fitKey++;
        selectSystem = true;
    }

    private async Task Open()
    {
        if (!await MayDiscard("Opening a file")) return;
        var outcome = await App.Open();
        if (outcome.Cancelled) return;
        if (!outcome.Succeeded) { Ui.Report(outcome); return; }
        layout = App.HasSketch && !narrow ? "split" : "context";
        fitKey++;
    }

    private async Task OpenExample()
    {
        if (!await MayDiscard("The example")) return;
        var opened = await App.OpenExample();
        layout = narrow ? "context" : "split";
        fitKey++;
        Ui.Tell(opened);
    }

    private async Task Save()
    {
        menu = null;
        Ui.Report(await App.Save());
    }

    private async Task SaveAs()
    {
        menu = null;
        Ui.Report(await App.SaveAs());
    }

    private async Task MakeCopy()
    {
        if (!await Ui.Confirm("Make this your own work?",
                "The copy replaces the work autosaved in this browser. Save that to a file first if you want to keep it.",
                "Make a copy")) return;
        var copied = await App.MakeCopyToEdit();
        viewingExample = false;
        Browser.ClearFragment();
        Ui.Tell(copied);
    }

    private async Task UseThisTab()
    {
        var taken = await App.UseThisTab();
        fitKey++;
        Ui.Tell(taken);
    }

    private async Task CopyLink()
    {
        var outcome = await App.CopyLink(Browser.PageUrl());
        if (!outcome.Succeeded) manualLink = outcome.Link;
        else if (outcome.IsLong) Ui.Warn(outcome.Title, [outcome.Text]);
        else Ui.Say(outcome.Title, outcome.Text);
    }

    private async Task ExportContext()
    {
        menu = null;
        Ui.Report(await App.ExportContextDiagram());
    }

    private async Task ExportSketch()
    {
        menu = null;
        Ui.Report(await App.ExportSketch());
    }

    private async Task ExportPlan()
    {
        menu = null;
        Ui.Report(await App.ExportPlan());
    }

    private void ShowGlossaryExport()
    {
        menu = null;
        glossaryDialog = true;
    }

    private async Task ExportGlossary(bool intoExisting)
    {
        glossaryDialog = false;
        var outcome = await App.ExportGlossary(intoExisting);
        if (outcome.Cancelled) return;
        if (!outcome.Succeeded) Ui.Problem(outcome.Title, [outcome.Text]);
        else Ui.Say(outcome.Title, outcome.Text);
    }

    public void Dispose()
    {
        Ui.Changed -= Refresh;
        App.Changed -= OnAppChanged;
        self?.Dispose();
    }
}
