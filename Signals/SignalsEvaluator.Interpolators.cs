namespace Meander.Signals;

internal partial class SignalsEvaluator
{
    private static DifferenceSignalInterpolator CreateDifferenceSignalInterpolator(SignalSamplingState state, IReadOnlyDictionary<Guid, DispatchInfo> dispatchInfo)
    {
        var data = state.Data as DifferenceSignalData;

        EmptyInterpolator fallback = default;
        dispatchInfo.TryGetValue(data!.MinuendSignalId, out var di);
        var m = di.Interpolator ?? (fallback ??= new EmptyInterpolator());
        dispatchInfo.TryGetValue(data!.SubtrahendSignalId, out di);
        var s = di.Interpolator ?? (fallback ?? new EmptyInterpolator());

        return new() { Minuend = m, Subtrahend = s };
    }

    private static MeanderSignalInterpolator CreateMeanderSignalInterpolator(SignalSamplingState state, in SwapchainSlot front)
        => new() { View = new ArrayView<double>(front.Values, state.Offsets.ValuesOffset, front.ValuesStride) };

    private ISignalInterpolator CreateInterpolator(in SwapchainSlot front, SignalSamplingState state, IReadOnlyDictionary<Guid, DispatchInfo> dispatchInfo)
    {
        return state.Data.Kind switch
        {
            SignalKind.Difference => CreateDifferenceSignalInterpolator(state, dispatchInfo),
            SignalKind.Meander => CreateMeanderSignalInterpolator(state, front),
            _ => throw new NotSupportedException($"{state.Data.Kind} interpolation is not supported.")
        };
    }

    private class EmptyInterpolator : ISignalInterpolator
    {
        public double Interpolate(double t) => 0;
    }
}
