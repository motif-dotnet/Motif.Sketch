namespace Motif.Sketch.Adapters;

/// <summary>
/// Project files over the <see cref="ProjectFiles"/> Seam: written back to the
/// same file where the browser can write in place, else downloaded.
/// </summary>
public sealed class ForStoringProjectsViaBrowserFiles(ProjectFiles files)
    : ForStoringProjects
{
    public bool CanSaveInPlace => files.CanWriteInPlace;

    public Task<OpenedFile?> Open() => files.Open();

    public async Task<SavedFile?> Save(
        string json, string suggestedName, string? handle)
    {
        if (!files.CanWriteInPlace)
        {
            await files.Download(json, suggestedName);
            return new SavedFile(suggestedName, null);
        }
        if (handle is not null)
        {
            // After an open or a refresh, the browser asks once more for
            // leave to write to the same file.
            if (!await files.MayWrite(handle)
                && !await files.AskToWrite(handle))
                throw new UnauthorizedAccessException(
                    "the browser wasn't allowed to write to that file. "
                    + "Use Save as to pick it again.");
            return new SavedFile(await files.Write(handle, json), handle);
        }
        if (await files.PickWhereToSave(suggestedName) is not { } picked)
            return null;
        await files.Write(picked.Handle!, json);
        return picked;
    }
}
