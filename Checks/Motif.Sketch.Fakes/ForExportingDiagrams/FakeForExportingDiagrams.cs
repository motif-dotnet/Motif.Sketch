namespace Motif.Sketch.Fakes;

public sealed class FakeForExportingDiagrams : ForExportingDiagrams
{
    public List<(string Name, string Svg)> Written { get; } = [];

    public bool Cancel { get; set; }

    public Task<bool> Write(string suggestedName, string svg)
    {
        if (Cancel) return Task.FromResult(false);
        Written.Add((suggestedName, svg));
        return Task.FromResult(true);
    }
}
