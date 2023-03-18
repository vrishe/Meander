namespace Meander.Signals;

internal class DiscreteSignalEvaluator : ISignalEvaluator
{
    private readonly double[] _values;

    public DiscreteSignalEvaluator(double[] values)
    {
        _values = values;
    }

    public double Evaluate(double t)
    {
        var a = Math.Clamp(Math.Floor(t), 0, _values.Length);
        var b = Math.Clamp(Math.Ceiling(t), 0, _values.Length);
        return a + (b - a) * (t - a);
    }
}
