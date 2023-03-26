using System.Reactive.Disposables;
using Microsoft.Extensions.Logging;

namespace Meander.Signals;

internal partial class SignalsEvaluator
{
    private static readonly Offsets EmpltyOffsets = new Offsets(-1, -1);

    private readonly object _swapChainMonitor = new();

    private CancellationTokenSource _cancellation;
    private DoubleBuffer<SwapchainSlot> _swapChain;

    private static T[] ResizeArray<T>(T[] array, int newSize, out bool resized)
    {
        if (resized = array == null)
            return new T[newSize];

        if (resized = array.Length < newSize
            || array.Length > newSize + newSize >> 2)
        {
            Array.Resize(ref array, newSize);
        }

        return array;
    }

    private IDictionary<Guid, SignalSamplingState> BeginDataSampling(IEnumerable<SignalDataAdapter.Signal> signals, CancellationToken cancellation)
    {
        var signalsMap = signals.ToDictionary(s => s.Id, s => new SignalSamplingState(s.Id, s.Data));

        var visited = new HashSet<Guid>();
        void UpdateWeights(SignalSamplingState ws)
        {
            if (!visited.Add(ws.Id))
                throw new InvalidOperationException($"Cyclic dependency on signal with id: {ws.Id}");

            foreach (var otherId in ws.Data.Dependencies)
            {
                cancellation.ThrowIfCancellationRequested();

                if (signalsMap.TryGetValue(otherId, out var other))
                {
                    UpdateWeights(other);
                    other.Weight++;
                    continue;
                }

                _logger.LogWarning("Unknown signal dependency with id: {}", otherId);
            }
        }

        foreach (var (id, ws) in signalsMap)
        {
            UpdateWeights(ws);
            visited.Clear();
        }

        cancellation.ThrowIfCancellationRequested();
        return signalsMap;
    }

    private Task<IDictionary<Guid, SignalSamplingState>> SampleDataAsync(CancellationToken cancellation)
    {
        var samplesCount = _adapter.SamplesCount;
        var signals = _adapter.EnumerateAllSignals();

        // It is important to capture signals up to here, so we observe a consistent adapter state next.
        return Task.Run(() => {
            var signalsMap = BeginDataSampling(signals, cancellation);
            SampleData(signalsMap, samplesCount, cancellation);
            return signalsMap;
        }, cancellation);
    }

    private void SampleData(IDictionary<Guid, SignalSamplingState> signalsMap, int samplesCount, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var plan = signalsMap.Values.OrderByDescending(ws => ws.Weight).ToList();

        cancellation.ThrowIfCancellationRequested();
        var (stats, values, _) = _swapChain.Back;
        (stats, values) = (
            ResizeArray(stats, plan.Count, out var statsResized),
            ResizeArray(values, plan.Count * samplesCount, out var valuesResized));

        double[] zeroSignal = null;
        ArrayView<double> SelectDependeciesViews(Guid id)
        {
            if (!signalsMap.TryGetValue(id, out var dep))
            {
                _logger.LogWarning("Missing sampling data for signal with id: {}", id);
                return new(zeroSignal ??= new double[samplesCount], 0, samplesCount);
            }

            if (dep.Offsets == EmpltyOffsets)
            {
                _logger.LogWarning("A signal with id: {} hasn't been sampled yet", id);
                return new(zeroSignal ??= new double[samplesCount], 0, samplesCount);
            }

            return new ArrayView<double>(values, dep.Offsets.ValuesOffset, samplesCount);
        }

        if (!statsResized && !valuesResized)
            Monitor.Enter(_swapChainMonitor);
        // you ought to not place any code here
        try
        {
            var nRecip = 1d / samplesCount;
            for (var i = 0; i < plan.Count; ++i)
            {
                var ofs = new Offsets(i, i * samplesCount);

                var ws = plan[i];
                ws.Offsets = ofs;

                var max = double.MinValue;
                var min = double.MaxValue;
                var rms = 0d;

                cancellation.ThrowIfCancellationRequested();
                for (var j = 0; j < samplesCount; ++j)
                {
                    var value = ws.Data.SampleAt(j, ws.Data.Dependencies.Select(SelectDependeciesViews));

                    if (min > value) min = value;
                    if (max < value) max = value;
                    rms += nRecip * value * value;

                    cancellation.ThrowIfCancellationRequested();
                    values[ofs.ValuesOffset + j] = value;
                }

                stats[ofs.StatsOffset] = new SignalStats(max, min, Math.Sqrt(rms));
            }

            cancellation.ThrowIfCancellationRequested();
            if (!Monitor.IsEntered(_swapChainMonitor))
                Monitor.Enter(_swapChainMonitor);

            _swapChain.SetAndSwap(new SwapchainSlot(stats, values, samplesCount));
        }
        finally
        {
            if (Monitor.IsEntered(_swapChainMonitor))
                Monitor.Exit(_swapChainMonitor);
        }
    }

    private Task<IDictionary<Guid, SignalSamplingState>> SampleDataAsync(int samplesCount, IEnumerable<SignalDataAdapter.Signal> signals, CancellationToken token)
    {
        const TaskCreationOptions taskOptions = TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning;
        return Task.Factory.StartNew(() =>
        {
            var signalsMap = BeginDataSampling(signals, token);
            SampleData(signalsMap, samplesCount, token);
            return signalsMap;
        }, token, taskOptions, TaskScheduler.Default);
    }

    private struct DoubleBuffer<T>
    {
        public T Front { get; private set; }
        public T Back { get; private set; }

        public void SetAndSwap(T back) => (Front, Back) = (back, Front);
    }

    private record class SignalSamplingState(Guid Id, ISignalData Data)
    {
        public Offsets Offsets { get; set; } = EmpltyOffsets;
        public int Weight { get; set; }

        public DispatchInfo ToDispatchInfo() => new(Data.Kind, Offsets);
    }

    private readonly record struct SwapchainSlot(SignalStats[] Stats, double[] Values, int ValuesStride);
}
