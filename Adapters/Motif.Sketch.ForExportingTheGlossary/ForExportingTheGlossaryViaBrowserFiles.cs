namespace Motif.Sketch.Adapters;

/// <summary>
/// The project's glossary over the <see cref="GlossaryFiles"/> Seam, so Export
/// glossary writes back to the same <c>glossary.md</c> where the browser can
/// write in place; else it downloads a copy.
/// </summary>
public sealed class ForExportingTheGlossaryViaBrowserFiles(GlossaryFiles files)
    : ForExportingTheGlossary
{
    public bool CanSaveInPlace => files.CanWriteInPlace;

    public Task<OpenedFile?> Open() => files.Open();

    public async Task<SavedFile?> Save(
        string markdown, string suggestedName, string? handle)
    {
        if (!files.CanWriteInPlace)
        {
            await files.Download(markdown, suggestedName);
            return new SavedFile(suggestedName, null);
        }
        if (handle is not null)
        {
            // A glossary opened was opened to read: the browser asks for
            // leave to write to it.
            if (!await files.MayWrite(handle)
                && !await files.AskToWrite(handle))
                throw new UnauthorizedAccessException(
                    "the browser wasn't allowed to write to that file.");
            return new SavedFile(await files.Write(handle, markdown), handle);
        }
        if (await files.PickWhereToSave(suggestedName) is not { } picked)
            return null;
        await files.Write(picked.Handle!, markdown);
        return picked;
    }
}
