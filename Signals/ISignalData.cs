namespace Meander.Signals;

public interface ISignalData
{
    SignalKind Kind { get; }

    double SampleAt(double t);
}
