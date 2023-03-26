namespace Meander.Signals;

public interface ISignalData
{
    SignalKind Kind { get; }

    IReadOnlyCollection<Guid> Dependencies { get; }

    double SampleAt(int i, IEnumerable<ArrayView<double>> views);
}
