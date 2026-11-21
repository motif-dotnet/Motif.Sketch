using Motif.Sketch.Adapters;

namespace Motif.Sketch.Fakes;

/// <summary>
/// A Project Files Fitting with no browser.
/// </summary>
public sealed class FakeProjectFiles()
    : FakeFiles("handle", "Picked.motifsketch.json"), ProjectFiles
{
    /// <summary>
    /// A project file the fake can hand to Open.
    /// </summary>
    public OpenedFile FileOf(string name, string text) => Opened(name, text);

    /// <summary>
    /// The same files after a refresh, for the next visit's app.
    /// </summary>
    public FakeProjectFiles AfterARefresh()
    {
        var next = new FakeProjectFiles();
        next.Refreshed(this);
        return next;
    }
}
