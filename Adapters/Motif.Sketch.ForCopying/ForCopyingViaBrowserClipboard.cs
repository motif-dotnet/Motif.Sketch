using Microsoft.JSInterop;

namespace Motif.Sketch.Adapters;

/// <summary>The browser's Clipboard API.</summary>
public sealed class ForCopyingViaBrowserClipboard(IJSRuntime js) : ForCopying, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> module =
        new(() => js.InvokeAsync<IJSObjectReference>("import", "./_content/Motif.Sketch.ForCopying/clipboard.js").AsTask());

    public async Task Copy(string text) => await (await module.Value).InvokeVoidAsync("copy", text);

    public async ValueTask DisposeAsync()
    {
        if (module.IsValueCreated) await (await module.Value).DisposeAsync();
    }
}
