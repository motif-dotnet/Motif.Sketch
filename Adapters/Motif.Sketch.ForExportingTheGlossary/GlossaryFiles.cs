namespace Motif.Sketch.Adapters;

/// <summary>
/// The Glossary Files Seam: the project's glossary on the Architect's
/// computer, one call each. Every Fitting must behave as these comments say.
/// Handle ids are ones it gave out, and each stands for one file.
/// </summary>
public interface GlossaryFiles
{
    /// <summary>
    /// Whether a file can be written in place, through a save picker and
    /// handles. Where it can't, the members that take or give a handle aren't
    /// called, and saving downloads a copy.
    /// </summary>
    bool CanWriteInPlace { get; }

    /// <summary>
    /// Asks the Architect for a glossary file, and reads it. It has a handle
    /// only where files can be written in place. Null if they decline.
    /// </summary>
    Task<OpenedFile?> Open();

    /// <summary>
    /// Asks the Architect where to save, suggesting a name: the file picked,
    /// with its handle, which may be written to without asking. Null if they
    /// decline.
    /// </summary>
    Task<SavedFile?> PickWhereToSave(string suggestedName);

    /// <summary>
    /// Whether the file may be written to without asking. A file opened may
    /// not be.
    /// </summary>
    Task<bool> MayWrite(string handle);

    /// <summary>
    /// Asks the Architect for leave to write to the file. False if it's
    /// refused.
    /// </summary>
    Task<bool> AskToWrite(string handle);

    /// <summary>
    /// Replaces the file's text, and gives the file's name.
    /// </summary>
    Task<string> Write(string handle, string text);

    /// <summary>
    /// Downloads the text as a file with this name.
    /// </summary>
    Task Download(string text, string name);
}
