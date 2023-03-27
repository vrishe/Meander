namespace Meander.Signals;

internal sealed class DifferenceSignalInterpolator : ISignalInterpolator
{
    public ISignalInterpolator Minuend { get; init; }
    public ISignalInterpolator Subtrahend { get; init; }

    public double Interpolate(double t) => Minuend.Interpolate(t) - Subtrahend.Interpolate(t);
}
