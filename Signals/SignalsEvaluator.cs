using System.Reactive.Disposables;
using Microsoft.Extensions.Logging;

namespace Meander.Signals;

internal sealed partial class SignalsEvaluator : ISignalsEvaluator
{
    private readonly ILogger _logger;

    private SignalDataAdapter _adapter;
    private Dictionary<Guid, DispatchInfo> _dispatchInfo;
    private Dictionary<Guid, Subscription<ISignalInterpolator>> _subscriptions;

    public SignalsEvaluator(ILoggerProvider logger)
    {
        _logger = logger.CreateLogger(nameof(SignalsEvaluator));
    }

    public SignalDataAdapter Adapter
    {
        get => _adapter;
        set => SetAdapter(value);
    }

    public IDisposable SubscribeSignalInterpolatorUpdates(Guid id, Action<ISignalInterpolator> callback)
    {
        if (callback is null)throw new ArgumentNullException(nameof(callback));

        _subscriptions ??= new();
        _subscriptions.TryGetValue(id, out var s);
        _subscriptions[id] = s = s with { Callback = callback };

        SafeDispatch(id, s);

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
            var results = await SampleDataAsync(
                _adapter.SamplesCount,
                _adapter.EnumerateAllSignals(),
                _cancellation.Token);

            if (!_cancellation.IsCancellationRequested)
            {
                _dispatchInfo = results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToDispatchInfo());

                if (_subscriptions != null)
                {
                    foreach (var id in _subscriptions.Keys.ToArray())
                        SafeDispatch(id, _subscriptions[id]);
                }
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
        var arg = s.Arg;
        var version = _dispatchInfo?.GetHashCode() ?? 0;
        if (arg.Version != version)
        {
            arg = new Versioned<ISignalInterpolator>
            {
                Version = version,
                Value = _dispatchInfo?.TryGetValue(id, out var ofs) == true
                    ? CreateInterpolator(ofs)
                    : null,
            };

            _subscriptions[id] = s with { Arg = arg };
        }

        if (arg.Value is null) return;

        try
        {
            s.Callback(arg.Value);
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

    private readonly record struct DispatchInfo(SignalKind SignalKind, Offsets Offsets);

    private readonly record struct Offsets(int StatsOffset, int ValuesOffset);

    private readonly record struct Subscription<T>(Versioned<T> Arg, Action<T> Callback);

    private readonly record struct Versioned<T>(T Value, int Version);


}
