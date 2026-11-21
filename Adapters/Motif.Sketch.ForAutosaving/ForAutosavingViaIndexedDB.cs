namespace Motif.Sketch.Adapters;

/// <summary>
/// Autosave in the browser's IndexedDB, over the <see cref="BrowserStorage"/>
/// Seam. The tab holding the storage's lock owns autosave. "Use this tab
/// instead" asks for the lock, and the owner writes its last autosave before
/// letting go. Where tabs can't share a lock, every tab owns it.
/// </summary>
public sealed class ForAutosavingViaIndexedDB : ForAutosaving
{
    private readonly BrowserStorage storage;

    private bool owns;

    /// <summary>
    /// The newest work not yet written.
    /// </summary>
    private AutosavedWork? waiting;

    /// <summary>
    /// Done once every write asked for so far has landed.
    /// </summary>
    private Task written = Task.CompletedTask;

    public ForAutosavingViaIndexedDB(BrowserStorage storage)
    {
        this.storage = storage;
        storage.WhenAskedForTheLock(HandOver);
    }

    public event Action? Released;

    public async Task<bool> Claim()
    {
        if (!await storage.CanLock()) return true;
        owns = await storage.TryLock();
        return owns;
    }

    public async Task TakeOver()
    {
        if (!await storage.CanLock()) return;
        // The wait for the lock starts before the owner is asked, so the
        // lock comes here once it's let go.
        var locked = storage.Lock();
        await storage.AskForTheLock();
        await locked;
        owns = true;
    }

    public Task<AutosavedWork?> Restore() => storage.Read();

    /// <summary>
    /// Writes are coalesced: while one is in flight, only the newest work
    /// waiting is written next.
    /// </summary>
    public Task Store(AutosavedWork work)
    {
        waiting = work;
        if (written.IsCompleted) written = WriteWaiting();
        return written;
    }

    private async Task WriteWaiting()
    {
        while (waiting is { } next)
        {
            waiting = null;
            await storage.Write(next);
        }
    }

    private async Task HandOver()
    {
        if (!owns) return;
        owns = false;
        try
        {
            await written;
        }
        catch (Exception)
        {
            // The other tab gets the lock all the same: it would otherwise
            // wait for it for ever.
        }
        Released?.Invoke();
        await storage.Unlock();
    }
}
