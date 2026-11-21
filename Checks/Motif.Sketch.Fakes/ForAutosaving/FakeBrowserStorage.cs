using Motif.Sketch.Adapters;

namespace Motif.Sketch.Fakes;

/// <summary>
/// A Browser Storage Fitting with no browser: the work kept, and a lock that
/// this tab or a pretend other tab may hold. The other tab lets the lock go
/// as soon as it's asked for it.
/// </summary>
public sealed class FakeBrowserStorage : BrowserStorage
{
    private Func<Task> asked = () => Task.CompletedTask;
    private bool heldHere;
    private TaskCompletionSource? lockFreed;
    private TaskCompletionSource? writesHeld;

    /// <summary>
    /// False for a browser whose tabs can't share a lock.
    /// </summary>
    public bool Lockable { get; set; } = true;

    /// <summary>
    /// True while another tab holds the lock.
    /// </summary>
    public bool HeldElsewhere { get; set; }

    public AutosavedWork? Work { get; set; }

    /// <summary>
    /// Every write that landed, in order.
    /// </summary>
    public List<AutosavedWork> Writes { get; } = [];

    /// <summary>
    /// Every read and write fails, as a full or broken IndexedDB does.
    /// </summary>
    public bool Fail { get; set; }

    /// <summary>
    /// Holds every write until <see cref="ReleaseWrites"/>, so a spec can act
    /// while one is in flight.
    /// </summary>
    public void HoldWrites() => writesHeld = new TaskCompletionSource();

    public void ReleaseWrites()
    {
        var gate = writesHeld;
        writesHeld = null;
        gate?.SetResult();
    }

    /// <summary>
    /// Another tab asks for the lock, as its "Use this tab instead" does, and
    /// takes it once it's let go.
    /// </summary>
    public async Task AnotherTabAsksForTheLock()
    {
        await asked();
        if (!heldHere) HeldElsewhere = true;
    }

    public Task<bool> CanLock() => Task.FromResult(Lockable);

    public Task<bool> TryLock()
    {
        if (HeldElsewhere || heldHere) return Task.FromResult(false);
        heldHere = true;
        return Task.FromResult(true);
    }

    public async Task Lock()
    {
        while (HeldElsewhere)
        {
            lockFreed ??= new TaskCompletionSource();
            await lockFreed.Task;
        }
        heldHere = true;
    }

    public Task Unlock()
    {
        heldHere = false;
        return Task.CompletedTask;
    }

    public Task AskForTheLock()
    {
        HeldElsewhere = false;
        var freed = lockFreed;
        lockFreed = null;
        freed?.SetResult();
        return Task.CompletedTask;
    }

    public void WhenAskedForTheLock(Func<Task> asked) => this.asked = asked;

    public Task<AutosavedWork?> Read() =>
        Fail ? throw new IOException("IndexedDB is unwell.") : Task.FromResult(Work);

    public async Task Write(AutosavedWork work)
    {
        if (writesHeld is { } gate) await gate.Task;
        if (Fail) throw new IOException("IndexedDB is full.");
        Work = work;
        Writes.Add(work);
    }
}
