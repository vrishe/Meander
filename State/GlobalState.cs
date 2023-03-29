using Newtonsoft.Json;

namespace Meander.State;

public sealed record class GlobalState
{
    [JsonIgnore]
    public string SourceFilePath { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public int SamplesCount { get; init; } = -1;
    public IReadOnlyList<SignalTrack> Tracks { get; init; } = new List<SignalTrack>();
}
