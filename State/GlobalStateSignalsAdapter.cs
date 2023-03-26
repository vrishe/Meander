using System.Reactive.Linq;
using Meander.Signals;
using ReduxSimple;

namespace Meander.State;

internal sealed class GlobalStateSignalsAdapter : SignalDataAdapter, IDisposable
{
    private readonly ReduxStore<GlobalState> _store;
    private readonly IList<IDisposable> _subscriptions = new List<IDisposable>();

    private ILookup<Guid, ISignalData> _lookup;
    private int _samplesCount;

    public GlobalStateSignalsAdapter(ReduxStore<GlobalState> store)
    {
        _store = store;
        _store.Select(state => (state.SamplesCount, state.Tracks))
            .DistinctUntilChanged()
            .Subscribe(data =>
            {
                var (samplesCount, tracks) = data;
                _lookup = tracks.ToLookup(t => t.Id, t => t.SignalData);
                _samplesCount = samplesCount;
                NotifyDataChanged();
            })
            .PutOnRecord(_subscriptions);
    }

    public void Dispose()
    {
        _subscriptions.DisposeAll().Clear();
    }

    public override int SamplesCount => _samplesCount;

    public override int SignalsCount => _lookup.Count;

    public override IEnumerable<Signal> EnumerateAllSignals()
    {
        foreach (var g in _lookup)
            yield return new Signal(g.Key, g.First());
    }
}
