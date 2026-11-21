using Microsoft.JSInterop;

namespace Motif.Sketch.Adapters;

/// <summary>Hands the plan on as a Markdown file: saved through the File System Access API where there is one, else downloaded.</summary>
public sealed class ForExportingThePlanViaMarkdownFile(IJSRuntime js) : ForExportingThePlan, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> module =
        new(() => js.InvokeAsync<IJSObjectReference>("import", "./_content/Motif.Sketch.ForExportingThePlan/solutionstubs.js").AsTask());

    public async Task<bool> Write(string suggestedName, string markdown) =>
        await (await module.Value).InvokeAsync<bool>("save", markdown, suggestedName);

    public async ValueTask DisposeAsync()
    {
        if (module.IsValueCreated) await (await module.Value).DisposeAsync();
    }
}
