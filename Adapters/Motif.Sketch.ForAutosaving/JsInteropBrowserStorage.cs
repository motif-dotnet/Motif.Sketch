using Microsoft.JSInterop;

namespace Motif.Sketch.Adapters;

/// <summary>
/// The browser's Browser Storage Fitting (wwwroot/autosave.js): IndexedDB for
/// the work, Web Locks for the lock, and a BroadcastChannel to ask for it.
/// </summary>
public sealed class JsInteropBrowserStorage(IJSRuntime js)
    : BrowserStorage, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> module = new(() => js
        .InvokeAsync<IJSObjectReference>(
            "import", "./_content/Motif.Sketch.ForAutosaving/autosave.js")
        .AsTask());

    private DotNetObjectReference<JsInteropBrowserStorage>? self;

    private Func<Task> asked = () => Task.CompletedTask;

    public async Task<bool> CanLock() =>
        await (await Module()).InvokeAsync<bool>("canLock");

    public async Task<bool> TryLock() =>
        await (await Module()).InvokeAsync<bool>("tryLock");

    public async Task Lock() => await (await Module()).InvokeVoidAsync("lock");

    public async Task Unlock() =>
        await (await Module()).InvokeVoidAsync("unlock");

    public async Task AskForTheLock() =>
        await (await Module()).InvokeVoidAsync("askForTheLock");

    public void WhenAskedForTheLock(Func<Task> asked) => this.asked = asked;

    public async Task<AutosavedWork?> Read() =>
        await (await Module()).InvokeAsync<AutosavedWork?>("read");

    public async Task Write(AutosavedWork work) =>
        await (await Module()).InvokeVoidAsync("write", work);

    [JSInvokable]
    public Task Asked() => asked();

    /// <summary>
    /// The module, listening for other tabs from the first call on.
    /// </summary>
    private async Task<IJSObjectReference> Module()
    {
        var loaded = await module.Value;
        if (self is null)
        {
            self = DotNetObjectReference.Create(this);
            await loaded.InvokeVoidAsync("listen", self);
        }
        return loaded;
    }

    public async ValueTask DisposeAsync()
    {
        if (module.IsValueCreated) await (await module.Value).DisposeAsync();
        self?.Dispose();
    }
}
