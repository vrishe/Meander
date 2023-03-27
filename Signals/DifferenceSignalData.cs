namespace Meander.Signals;

internal sealed class DifferenceSignalData : ISignalData
{
    public DifferenceSignalData(Guid minuendSignalId, Guid subtrahendSignalId)
    {
        MinuendSignalId = minuendSignalId;
        SubtrahendSignalId = subtrahendSignalId;
        Dependencies = new Guid[] { minuendSignalId, subtrahendSignalId };
    }

    public Guid MinuendSignalId { get; }
    public Guid SubtrahendSignalId { get; }

    public IReadOnlyCollection<Guid> Dependencies { get; }

    public bool IsSampled => false;

    public SignalKind Kind => SignalKind.Difference;

    public double SampleAt(int i) => throw new NotSupportedException();
}
