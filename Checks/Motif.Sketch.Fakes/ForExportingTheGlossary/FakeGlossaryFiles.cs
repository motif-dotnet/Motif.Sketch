using Motif.Sketch.Adapters;

namespace Motif.Sketch.Fakes;

/// <summary>
/// A Glossary Files Fitting with no browser.
/// </summary>
public sealed class FakeGlossaryFiles()
    : FakeFiles("glossary", "glossary.md"), GlossaryFiles
{
    /// <summary>
    /// A glossary the fake can hand to Open.
    /// </summary>
    public OpenedFile FileOf(string text, string name = "glossary.md") =>
        Opened(name, text);
}
