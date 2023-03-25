namespace Meander.Signals;

public interface ISignalsEvaluator
{
    IDisposable SubscribeSignalInterpolatorUpdates(Guid id, Action<ISignalInterpolator> callback);
}
