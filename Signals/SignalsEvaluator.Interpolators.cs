namespace Meander.Signals;

internal partial class SignalsEvaluator
{
    private ISignalInterpolator CreateInterpolator(in DispatchInfo info)
    {
        var (stats, values, valuesStride) = _swapChain.Front;
        return new SignalInterpolator
        {
            Stats = stats[info.Offsets.StatsOffset],
            View = new ArrayView<double>(values, info.Offsets.ValuesOffset, valuesStride)
        };
    }

    private class SignalInterpolator : ISignalInterpolator
    {
        public SignalStats Stats { get; init; }

        public ArrayView<double> View { get; init; }

        public double Interpolate(double t)
        {
            var values = View.Values;
            if (t <= 0) return values[Index.Start];
            if (t >= 1) return values[Index.End];

            t *= (values.Length - 1);

            var tMin = Math.Floor(t);
            var a = values[(int)tMin];
            return a + (values[(int)Math.Ceiling(t)] - a) * (t - tMin);
        }
    }
}
