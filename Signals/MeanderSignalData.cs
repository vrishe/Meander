namespace Meander.Signals;

internal sealed class MeanderSignalData : ISignalData
{
    private readonly double[] _values;

    public MeanderSignalData(IEnumerable<double> values)
    {
        _values = values.ToArray();
    }

    public SignalKind Kind => SignalKind.Meander;

    public int SamplesCount => _values.Length;

    public double this[int i] => _values[i];

    public double SampleAt(double t) => _values[(long)Math.Clamp(Math.Floor(t * _values.Length), 0, _values.Length)];
}
