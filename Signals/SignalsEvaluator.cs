using System.Reactive.Disposables;
using Microsoft.Extensions.Logging;

namespace Meander.Signals;

internal sealed partial class SignalsEvaluator : ISignalsEvaluator
{
    private readonly ILogger _logger;

    private SignalDataAdapter _adapter;
    private IDictionary<Guid, DispatchInfo> _dispatchInfo;
    private Dictionary<Guid, Subscription<ISignalInterpolator>> _subscriptions;

    public SignalsEvaluator(ILogger<App> logger)
    {
        _logger = logger;
    }

    public SignalDataAdapter Adapter
    {
        get => _adapter;
        set => SetAdapter(value);
    }

    public IDisposable SubscribeSignalInterpolatorUpdates(Guid id, Action<ISignalInterpolator> callback)
    {
        if (callback is null)throw new ArgumentNullException(nameof(callback));

        var subscription = new Subscription<ISignalInterpolator>(callback);

        _subscriptions ??= new();
        _subscriptions[id] = subscription;

        SafeDispatch(id, subscription);

        return Disposable.Create(() => _subscriptions.Remove(id));
    }

    private void AbortRecentDataChangeUpdate()
    {
        if (_cancellation is not null)
        {
            _cancellation.Cancel();
            _cancellation.Dispose();
            _cancellation = null;
        }
    }

    private async void OnAdapterDataChanged()
    {
        AbortRecentDataChangeUpdate();

        _cancellation = new CancellationTokenSource();

        try
        {
            _dispatchInfo = await SampleDataAsync(
                _adapter.SamplesCount,
                _adapter.EnumerateAllSignals(),
                _cancellation.Token);

            if (_subscriptions != null)
            {
                foreach (var (id, s) in _subscriptions)
                    SafeDispatch(id, s);
            }
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to sample signals data");
        }
    }

    private void SafeDispatch(in Guid id, in Subscription<ISignalInterpolator> s)
    {
        if (_dispatchInfo?.TryGetValue(id, out var info) != true) return;

        try
        {
            s.Callback(info.Interpolator);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{} signal subscription callback:", id);
        }
    }

    private void SetAdapter(SignalDataAdapter newAdapter)
    {
        if (_adapter == newAdapter) return;

        if (_adapter != null)
        {
            _adapter.OnDataChanged -= OnAdapterDataChanged;
        }

        _adapter = newAdapter;

        if (_adapter != null)
        {
            _adapter.OnDataChanged += OnAdapterDataChanged;
            OnAdapterDataChanged();

            return;
        }

        AbortRecentDataChangeUpdate();
    }

    private readonly record struct DispatchInfo(ISignalInterpolator Interpolator);

    private readonly record struct Offsets(int ValuesOffset);

    private readonly record struct Subscription<T>(Action<T> Callback);


}
