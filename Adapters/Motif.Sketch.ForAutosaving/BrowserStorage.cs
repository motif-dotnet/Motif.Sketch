namespace Motif.Sketch.Adapters;

/// <summary>
/// The Browser Storage Seam: where this browser keeps autosaved work, and the
/// lock its tabs share, one call each. Every Fitting must behave as these
/// comments say.
/// </summary>
public interface BrowserStorage
{
    /// <summary>
    /// Whether this browser's tabs can share a lock. Where they can't, the
    /// lock members aren't called.
    /// </summary>
    Task<bool> CanLock();

    /// <summary>
    /// Takes the lock if no tab holds it, this one included. False, and
    /// nothing taken, if one does.
    /// </summary>
    Task<bool> TryLock();

    /// <summary>
    /// Takes the lock, waiting for as long as another tab holds it.
    /// </summary>
    Task Lock();

    /// <summary>
    /// Lets the lock go, so a tab waiting for it gets it.
    /// </summary>
    Task Unlock();

    /// <summary>
    /// Tells every other tab that this one wants the lock.
    /// </summary>
    Task AskForTheLock();

    /// <summary>
    /// Calls <paramref name="asked"/> whenever another tab asks for the lock,
    /// whether or not this tab holds it, and waits for it to finish.
    /// </summary>
    void WhenAskedForTheLock(Func<Task> asked);

    /// <summary>
    /// The work kept, or null if none has been. A file handle kept with it
    /// comes back under a new handle id that stands for the same file.
    /// </summary>
    Task<AutosavedWork?> Read();

    /// <summary>
    /// Keeps the work in place of whatever was kept before, with the file its
    /// handle id stands for, or with no file if the id stands for none.
    /// </summary>
    Task Write(AutosavedWork work);
}
