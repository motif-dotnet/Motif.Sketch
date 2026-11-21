using Microsoft.JSInterop;

namespace Motif.Sketch.Adapters;

/// <summary>
/// The browser's Project Files Fitting (wwwroot/projects.js): the File System
/// Access API where the browser has it, else upload and download. Call
/// <see cref="Load"/> once at startup, before anything else.
/// </summary>
public sealed class JsInteropProjectFiles(IJSRuntime js)
    : ProjectFiles, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> module = new(() => js
        .InvokeAsync<IJSObjectReference>(
            "import", "./_content/Motif.Sketch.ForStoringProjects/projects.js")
        .AsTask());

    public bool CanWriteInPlace { get; private set; }

    public async Task Load() => CanWriteInPlace =
        await (await module.Value).InvokeAsync<bool>("canWriteInPlace");

    public async Task<OpenedFile?> Open() =>
        await (await module.Value).InvokeAsync<OpenedFile?>("open");

    public async Task<SavedFile?> PickWhereToSave(string suggestedName) =>
        await (await module.Value).InvokeAsync<SavedFile?>(
            "pickWhereToSave", suggestedName);

    public async Task<bool> MayWrite(string handle) =>
        await (await module.Value).InvokeAsync<bool>("mayWrite", handle);

    public async Task<bool> AskToWrite(string handle) =>
        await (await module.Value).InvokeAsync<bool>("askToWrite", handle);

    public async Task<string> Write(string handle, string text) =>
        await (await module.Value).InvokeAsync<string>("write", handle, text);

    public async Task Download(string text, string name) =>
        await (await module.Value).InvokeVoidAsync("download", text, name);

    public async ValueTask DisposeAsync()
    {
        if (module.IsValueCreated) await (await module.Value).DisposeAsync();
    }
}
