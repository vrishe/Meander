namespace Meander.Signals;

internal sealed class MeanderSignalData : ISignalData
{
    private readonly double[] _values;

    public MeanderSignalData(IEnumerable<double> values)
    {
        _values = values.ToArray();
    }

    public IReadOnlyCollection<Guid> Dependencies => Array.Empty<Guid>();

    public SignalKind Kind => SignalKind.Meander;

    public int SamplesCount => _values.Length;

    public double this[int i] => _values[i];

    public double SampleAt(int i, IEnumerable<ArrayView<double>> lookup) => _values[i < 0 ? 0 : i >= _values.Length ? _values.Length - 1 : i];
}
