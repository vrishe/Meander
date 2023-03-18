namespace Meander.State;

public sealed class SignalTrack
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Color { get; init; }

    public override bool Equals(object obj)
    {
        return base.Equals(obj)
            || obj is SignalTrack other
                && Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
