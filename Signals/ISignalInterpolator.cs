namespace Meander.Signals;

public interface ISignalInterpolator
{
    public SignalStats Stats { get; }

    double Interpolate(double t);
}
