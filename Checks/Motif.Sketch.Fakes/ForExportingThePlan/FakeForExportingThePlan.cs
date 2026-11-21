namespace Motif.Sketch.Fakes;

public sealed class FakeForExportingThePlan : ForExportingThePlan
{
    public List<(string Name, string Markdown)> Written { get; } = [];

    public Task<bool> Write(string suggestedName, string markdown)
    {
        Written.Add((suggestedName, markdown));
        return Task.FromResult(true);
    }
}
