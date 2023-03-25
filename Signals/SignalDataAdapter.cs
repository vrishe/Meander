namespace Meander.Signals;

internal abstract class SignalDataAdapter
{
    public event Action OnDataChanged;

    public abstract int SignalsCount { get; }

    public abstract IEnumerable<Signal> EnumerateAllSignals();

    protected void NotifyDataChanged()
    {
        OnDataChanged?.Invoke();
    }

    public record struct Signal(Guid Id, ISignalData Data);
}
