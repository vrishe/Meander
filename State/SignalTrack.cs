using Meander.Signals;

namespace Meander.State;

public sealed class SignalTrack
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Color { get; init; }
    public SignalKind SignalKind { get; init; }
    public ISignalData SignalData { get; init; }
    public int Version { get; init; }

    public override bool Equals(object obj)
    {
        return base.Equals(obj)
            || obj is SignalTrack other
                && Version == other.Version
                && Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
