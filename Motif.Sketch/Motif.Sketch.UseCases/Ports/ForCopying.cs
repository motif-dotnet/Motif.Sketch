namespace Motif.Sketch;

/// <summary>Driven port: takes the snapshot link when the Architect copies it.</summary>
public interface ForCopying
{
    Task Copy(string text);
}
