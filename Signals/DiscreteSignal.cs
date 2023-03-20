namespace Meander.Signals;

internal sealed class DiscreteSignal : ISignal
{
    private readonly double[] _samples;

    public DiscreteSignal(IEnumerable<double> samples, bool normalized = false)
    {
        if (normalized)
        {
            var (min, max) = (samples.Min(), samples.Max());
            _samples = samples.Select(v => (v - min) / (max - min)).ToArray();
        }
        else
        {
            _samples = samples.ToArray();
        }
    }

    public SignalKind Kind => SignalKind.Meander;

    public double this[int i] => _samples[i];

    public int Count => _samples.Length;
}
