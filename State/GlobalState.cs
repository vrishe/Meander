namespace Meander.State;

public sealed record class GlobalState
{
    public string ProjectName { get; init; } = string.Empty;
    public int SamplesCount { get; init; } = -1;
    public IReadOnlyList<SignalTrack> Tracks { get; init; } = new List<SignalTrack>();
}
