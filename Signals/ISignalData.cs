namespace Meander.Signals;

public interface ISignalData
{
    IReadOnlyCollection<Guid> Dependencies { get; }
    bool IsSampled { get; }
    SignalKind Kind { get; }
    double SampleAt(int i);
}
