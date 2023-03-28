namespace Meander.Signals;

internal sealed class MeanderSignalInterpolator : ISignalInterpolator
{
    public ArrayView<double> View { get; init; }

    public double Interpolate(double t)
    {
        var values = View.Values;
        if (t <= 0) return values[Index.Start];
        if (t >= 1) return values[Index.End];
        return values[(int)Math.Floor(t * values.Length)];
    }
}