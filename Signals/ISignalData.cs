namespace Meander.Signals;

public interface ISignalData
{
    SignalKind Kind { get; }

    double Evaluate(double t);
}
