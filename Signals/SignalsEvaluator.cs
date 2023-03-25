using System.Reactive.Disposables;
using Microsoft.Extensions.Logging;

namespace Meander.Signals;

internal sealed class SignalsEvaluator : ISignalsEvaluator
{
    private const int SamplesPerSignal = 2048;

    private readonly ILogger _logger;
    private readonly IDictionary<Guid, Subscription> _subscriptions = new Dictionary<Guid, Subscription>();

    private SignalDataAdapter _adapter;
    private SamplesBuffer _bufferBack, _bufferFront;
    private CancellationTokenSource _cancellation;

    public SignalsEvaluator(ILoggerProvider logger)
    {
        _logger = logger.CreateLogger(nameof(SignalsEvaluator));

        _bufferBack = new();
        _bufferFront = new();
    }

    public SignalDataAdapter Adapter
    { 
        get => _adapter;
        set => SetAdapter(value);
    }

    public IDisposable SubscribeSignalInterpolatorUpdates(Guid id, Action<ISignalInterpolator> callback)
    {
        if (callback is null)throw new ArgumentNullException(nameof(callback));

        _subscriptions.TryGetValue(id, out var s);
        _subscriptions[id] = s = s with
        {
            Callback = callback,
            SignalInterpolator = s.BufferOffset.HasValue && s.SignalInterpolator == null
                ? new BufferSignalInterpolator(_bufferFront, s.BufferOffset.Value)
                : s.SignalInterpolator,
        };

        SafeDispatch(id, ref s);

        return Disposable.Create(() => _subscriptions.Remove(id));
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
            var offsets = await SampleDataAsync(_cancellation.Token);

            if (_cancellation.IsCancellationRequested) return;

            foreach (var (id, ws) in offsets)
            {
                _subscriptions.TryGetValue(id, out var s);
                _subscriptions[id] = s = s with
                {
                    BufferOffset = ws.BufferOffset,
                    SignalInterpolator = new BufferSignalInterpolator(_bufferFront, ws.BufferOffset),
                };

                SafeDispatch(id, ref s);
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

    private void SafeDispatch(in Guid id, ref Subscription s)
    {
        if (s.Callback == null || s.SignalInterpolator == null) return;

        try
        {
            s.Callback(s.SignalInterpolator);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "{} signal subscription callback:", id);
        }
    }

    private async Task<IDictionary<Guid, WeightedSignal>> SampleDataAsync(CancellationToken cancellation)
    {
        var signalsCount = _adapter.SignalsCount;
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

        lock (_bufferBack)
        {
            _bufferBack.EnsureCapacity(signalsCount * SamplesPerSignal);

            for (var i = 0; i < plan.Count; ++i)
            {
                var ws = plan[i];
                var sampler = GetSignalSampler(ws.Data.Kind);

                var ofs = i * SamplesPerSignal;
                for (var j = 0; j < SamplesPerSignal; ++j)
                {
                    cancellation.ThrowIfCancellationRequested();

                    _bufferBack[ofs + j] = sampler(ws.Data, j / (double)SamplesPerSignal);
                }

                ws.BufferOffset = ofs;
            }

            cancellation.ThrowIfCancellationRequested();

            _bufferBack = Interlocked.Exchange(ref _bufferFront, _bufferBack);
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

    private class BufferSignalInterpolator : ISignalInterpolator
    {
        private readonly SamplesBuffer _buffer;
        private readonly int _offset;

        public BufferSignalInterpolator(SamplesBuffer buffer, int offset)
        {
            _buffer = buffer;
            _offset = offset;
        }

        public double Interpolate(double t)
        {
            if (t <= 0) return _buffer[_offset];
            if (t >= 1) return _buffer[_offset + SamplesPerSignal];

            t *= (SamplesPerSignal - 1);

            var tMin = Math.Floor(t);
            var a = _buffer[_offset + (int)tMin];
            return a + (_buffer[_offset + (int)Math.Ceiling(t)] - a) * (t - tMin);
        }
    }

    private class EmptySignalInterpolator : ISignalInterpolator
    {
        public double Interpolate(double t) => 0d;
    }

    private class SamplesBuffer
    {
        private double[] _data = Array.Empty<double>();

        public ref double this[int index] => ref _data[index];

        public void EnsureCapacity(int capacity)
        {
            if (_data.Length < capacity
                    || _data.Length > capacity + capacity >> 2)
                Array.Resize(ref _data, capacity);
        }
    }

    private readonly record struct Subscription(
        int? BufferOffset,
        Action<ISignalInterpolator> Callback,
        ISignalInterpolator SignalInterpolator);

    private record class WeightedSignal(Guid Id, ISignalData Data)
    {
        public int BufferOffset { get; set; } = -1;
        public int Weight { get; set; }
    }
}
