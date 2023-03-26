using System.Reactive.Disposables;
using Microsoft.Extensions.Logging;

namespace Meander.Signals;

internal sealed class SignalsEvaluator : ISignalsEvaluator
{
    private const int SamplesPerSignal = 2048;

    private readonly ILogger _logger;
    private readonly object _swapChainMonitor = new();

    private SignalDataAdapter _adapter;
    private CancellationTokenSource _cancellation;
    private Dictionary<Guid, Offsets> _offsets;
    private Dictionary<Guid, Subscription<ISignalInterpolator>> _subscriptions;
    private DoubleBuffer<SwapchainSlot> _swapChain;

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

    private static T[] ResizeArray<T>(T[] array, int newSize, out bool resized)
    {
        if (resized = array == null)
            return new T[newSize];

        if (resized = array.Length < newSize
            || array.Length > newSize + newSize >> 2)
            Array.Resize(ref array, newSize);

        return array;
    }

    private ISignalInterpolator CreateInterpolator(in Offsets ofs)
    {
        var (stats, values, valuesStride) = _swapChain.Front;
        return new SignalInterpolator
        {
            Stats = stats[ofs.StatsOffset],
            Values = new ArrayView<double>(values, ofs.ValuesOffset, valuesStride)
        };
    }

    private Func<ISignalData, double, double> GetSignalSampler(SignalKind kind)
    {
        return (d, t) => d.SampleAt(t);
    }

    private async void OnAdapterDataChanged()
    {
        ResetState();

        _cancellation = new CancellationTokenSource();

        try
        {
            var results = await SampleDataAsync(_cancellation.Token);

            if (!_cancellation.IsCancellationRequested)
            {
                _offsets = results.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Offsets);

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

    private void ResetState()
    {
        if (_cancellation is not null)
        {
            _cancellation.Cancel();
            _cancellation.Dispose();
            _cancellation = null;
        }
    }

    private void SafeDispatch(in Guid id, in Subscription<ISignalInterpolator> s)
    {
        var arg = s.Arg;
        var version = _offsets?.GetHashCode() ?? 0;
        if (arg.Version != version)
        {
            arg = new Versioned<ISignalInterpolator>
            {
                Version = version,
                Value = _offsets?.TryGetValue(id, out var ofs) == true
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

    private async Task<IDictionary<Guid, WeightedSignal>> SampleDataAsync(CancellationToken cancellation)
    {
        var signals = _adapter.EnumerateAllSignals();
        var signalsMap = await Task.Run(() => signals.ToDictionary(s => s.Id, s => new WeightedSignal(s.Id, s.Data)), cancellation)
            .ConfigureAwait(false);

        void UpdateWeights(WeightedSignal ws)
        {
            if (ws.Data is not ISignalDependent dep) return;

            foreach (var otherId in dep.Dependencies)
            {
                cancellation.ThrowIfCancellationRequested();

                if (signalsMap.TryGetValue(otherId, out var other))
                {
                    UpdateWeights(other);
                    other.Weight++;
                    continue;
                }

                _logger.LogWarning("Missing data for signal with id: {}", otherId);
            }
        }

        foreach (var (id, ws) in signalsMap)
            UpdateWeights(ws);

        cancellation.ThrowIfCancellationRequested();

        var plan = signalsMap.Values.OrderByDescending(ws => ws.Weight).ToList();

        cancellation.ThrowIfCancellationRequested();

        var (stats, values, _) = _swapChain.Back;
        (stats, values) = (
            ResizeArray(stats, plan.Count, out var statsResized),
            ResizeArray(values, plan.Count * SamplesPerSignal, out var valuesResized));

        if (!statsResized && !valuesResized)
            Monitor.Enter(_swapChainMonitor);

        try
        {
            const double nRecip = 1d / SamplesPerSignal;
            for (var i = 0; i < plan.Count; ++i)
            {
                var ofs = new Offsets(i, i * SamplesPerSignal);

                var ws = plan[i];
                ws.Offsets = ofs;

                var max = double.MinValue;
                var min = double.MaxValue;
                var rms = 0d;

                var sampler = GetSignalSampler(ws.Data.Kind);
                for (var j = 0; j < SamplesPerSignal; ++j)
                {
                    cancellation.ThrowIfCancellationRequested();

                    var value = sampler(ws.Data, j / (double)SamplesPerSignal);

                    if (min > value) min = value;
                    if (max < value) max = value;
                    rms += nRecip * value * value;

                    values[ofs.ValuesOffset + j] = value;
                }

                stats[ofs.StatsOffset] = new SignalStats(max, min, Math.Sqrt(rms));
            }

            if (!Monitor.IsEntered(_swapChainMonitor))
                Monitor.Enter(_swapChainMonitor);

            _swapChain.SetAndSwap(new SwapchainSlot(stats, values, SamplesPerSignal));
        }
        finally
        {
            if (Monitor.IsEntered(_swapChainMonitor))
                Monitor.Exit(_swapChainMonitor);
        }

        return signalsMap;
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

        ResetState();
    }

    private readonly struct ArrayView<T>
    {
        private readonly T[] _array;
        private readonly int _offset;

        public ArrayView(T[] array, int start, int end)
        {
            _array = array;
            _offset = start;

            Length = end - start;
        }

        public int Length { get; }

        public ref T this[int index] => ref _array[_offset + index];

        public ref T this[Index index] => ref _array[_offset + index.GetOffset(Length)];
    }

    private class SignalInterpolator : ISignalInterpolator
    {
        public SignalStats Stats { get; init; }

        public ArrayView<double> Values { get; init; }

        public double Interpolate(double t)
        {
            if (t <= 0) return Values[Index.Start];
            if (t >= 1) return Values[Index.End];

            t *= (SamplesPerSignal - 1);

            var tMin = Math.Floor(t);
            var a = Values[(int)tMin];
            return a + (Values[(int)Math.Ceiling(t)] - a) * (t - tMin);
        }
    }

    private struct DoubleBuffer<T>
    {
        public T Front { get; private set; }
        public T Back { get; private set; }

        public void SetAndSwap(T back) => (Front, Back) = (back, Front);
    }

    private readonly record struct Offsets(int StatsOffset, int ValuesOffset);

    private readonly record struct Subscription<T>(Versioned<T> Arg, Action<T> Callback);

    private readonly record struct SwapchainSlot(SignalStats[] Stats, double[] Values, int ValuesStride);

    private readonly record struct Versioned<T>(T Value, int Version);

    private record class WeightedSignal(Guid Id, ISignalData Data)
    {
        public Offsets Offsets { get; set; } = new Offsets(-1, -1);
        public int Weight { get; set; }
    }
}
