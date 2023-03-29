using Microsoft.Extensions.Logging;

namespace Meander.Signals;

internal partial class SignalsEvaluator
{
    private static readonly Offsets EmpltyOffsets = new(-1);

    private readonly object _swapChainMonitor = new();

    private CancellationTokenSource _cancellation;
    private DoubleBuffer<SwapchainSlot> _swapChain;

    private static T[] ResizeArray<T>(T[] array, int newSize, out bool resized)
    {
        if (resized = array == null
            || array.Length < newSize
            || array.Length > newSize + newSize >> 2)
        {
            return new T[newSize];
        }

        return array;
    }

    private Dictionary<Guid, SignalSamplingState> BeginDataSampling(IEnumerable<SignalDataAdapter.Signal> signals, CancellationToken cancellation)
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

            visited.Remove(ws.Id);
        }

        foreach (var (id, ws) in signalsMap)
        {
            UpdateWeights(ws);
            visited.Clear();
        }

        cancellation.ThrowIfCancellationRequested();
        return signalsMap;
    }

    private IDictionary<Guid, DispatchInfo> BuildDispatchInfo(IDictionary<Guid, SignalSamplingState> signalsMap)
    {
        var front = _swapChain.Front;
        var result = new Dictionary<Guid, DispatchInfo>();
        foreach (var ws in signalsMap.Select(kvp => kvp.Value).OrderByDescending(ws => ws.Weight))
            result.Add(ws.Id, new DispatchInfo(CreateInterpolator(front, ws, result)));

        return result;
    }

    private void SampleData(IDictionary<Guid, SignalSamplingState> signalsMap, int samplesCount, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        var plan = signalsMap.Select(kvp => kvp.Value).Where(ws => ws.Data.IsSampled).ToList();

        cancellation.ThrowIfCancellationRequested();
        var values = ResizeArray(_swapChain.Back.Values, plan.Count * samplesCount, out var valuesResized);

        cancellation.ThrowIfCancellationRequested();
        if (!valuesResized)
            Monitor.Enter(_swapChainMonitor);
        // you ought not to place any code here
        try
        {
            for (int i = 0; i < plan.Count; ++i)
            {
                cancellation.ThrowIfCancellationRequested();

                var ws = plan[i];
                if (ws.Data.IsSampled)
                {
                    var valuesOfs = i * samplesCount;
                    for (var j = 0; j < samplesCount; ++j)
                        values[valuesOfs + j] = ws.Data.SampleAt(j);

                    ws.Offsets = new Offsets(valuesOfs);
                }
            }

            cancellation.ThrowIfCancellationRequested();
            if (!Monitor.IsEntered(_swapChainMonitor))
                Monitor.Enter(_swapChainMonitor);

            cancellation.ThrowIfCancellationRequested();
            _swapChain.SetAndSwap(new(values, samplesCount));
        }
        finally
        {
            if (Monitor.IsEntered(_swapChainMonitor))
                Monitor.Exit(_swapChainMonitor);
        }
    }

    private Task<IDictionary<Guid, DispatchInfo>> SampleDataAsync(int samplesCount, IEnumerable<SignalDataAdapter.Signal> signals, CancellationToken token)
    {
        const TaskCreationOptions taskOptions = TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning;
        return Task.Factory.StartNew(() =>
        {
            var signalsMap = BeginDataSampling(signals, token);
            SampleData(signalsMap, samplesCount, token);
            return BuildDispatchInfo(signalsMap);
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
    }

    private readonly record struct SwapchainSlot(double[] Values, int ValuesStride);
}
