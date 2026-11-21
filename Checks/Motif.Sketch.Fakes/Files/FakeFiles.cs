namespace Motif.Sketch.Fakes;

/// <summary>
/// A file written by a fake files Fitting: its text, where it went, and
/// whether it was written in place or downloaded.
/// </summary>
public sealed record WrittenFile(
    string Text, string Name, string? Handle, bool Downloaded);

/// <summary>
/// Files on a pretend computer, behind the Project Files and Glossary Files
/// fake Fittings. Behaves like a browser that writes in place, or like one
/// that only downloads. A file opened, like one after a refresh, may be
/// written to only once the Architect gives leave; a file picked to save to
/// may be written to at once.
/// </summary>
public abstract class FakeFiles(string prefix, string saveAsName)
{
    private readonly Dictionary<string, string> names = [];
    private readonly HashSet<string> writable = [];
    private int count;

    public bool CanWriteInPlace { get; set; } = true;

    /// <summary>
    /// What the next Open returns; null means the Architect declines.
    /// </summary>
    public OpenedFile? NextOpen { get; set; }

    /// <summary>
    /// The name the Architect picks in the next save picker; null means they
    /// decline.
    /// </summary>
    public string? NextSaveAsName { get; set; } = saveAsName;

    /// <summary>
    /// The Architect refuses the browser leave to write to a file.
    /// </summary>
    public bool RefuseWrites { get; set; }

    public bool Fail { get; set; }

    /// <summary>
    /// Every write, in order.
    /// </summary>
    public List<WrittenFile> Writes { get; } = [];

    public Task<OpenedFile?> Open() =>
        Fail ? throw new IOException("The disk is unwell.")
            : Task.FromResult(NextOpen);

    public Task<SavedFile?> PickWhereToSave(string suggestedName)
    {
        FailIfAsked();
        if (NextSaveAsName is not { } picked)
            return Task.FromResult<SavedFile?>(null);
        var handle = Keep(picked);
        writable.Add(handle);
        return Task.FromResult<SavedFile?>(new SavedFile(picked, handle));
    }

    public Task<bool> MayWrite(string handle) =>
        Task.FromResult(writable.Contains(handle));

    public Task<bool> AskToWrite(string handle)
    {
        if (RefuseWrites) return Task.FromResult(false);
        writable.Add(handle);
        return Task.FromResult(true);
    }

    public Task<string> Write(string handle, string text)
    {
        FailIfAsked();
        if (!writable.Contains(handle))
            throw new InvalidOperationException(
                "The browser would refuse a write it wasn't given leave for.");
        Writes.Add(new WrittenFile(text, names[handle], handle, false));
        return Task.FromResult(names[handle]);
    }

    public Task Download(string text, string name)
    {
        FailIfAsked();
        Writes.Add(new WrittenFile(text, name, null, true));
        return Task.CompletedTask;
    }

    /// <summary>
    /// A file the fake can hand to Open, with a handle where files can be
    /// written in place.
    /// </summary>
    protected OpenedFile Opened(string name, string text) =>
        new(name, text, CanWriteInPlace ? Keep(name) : null);

    /// <summary>
    /// These files as they are after a refresh: still there, but the browser
    /// asks again for leave to write to them.
    /// </summary>
    protected void Refreshed(FakeFiles from)
    {
        foreach (var (handle, name) in from.names) names[handle] = name;
        count = from.count;
        CanWriteInPlace = from.CanWriteInPlace;
    }

    private string Keep(string name)
    {
        var handle = $"{prefix}-{++count}";
        names[handle] = name;
        return handle;
    }

    private void FailIfAsked()
    {
        if (Fail) throw new IOException("The disk is full.");
    }
}
