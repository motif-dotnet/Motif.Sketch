namespace Motif.Sketch;

/// <summary>Driven port: writes the context diagram and the sketch as pictures.</summary>
public interface ForExportingDiagrams
{
    /// <summary>Writes an SVG. False if the user cancelled.</summary>
    Task<bool> Write(string suggestedName, string svg);
}
