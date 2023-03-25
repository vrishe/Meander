using System.Reactive.Linq;
using Meander.Signals;
using ReduxSimple;

namespace Meander.State;

internal sealed class GlobalStateSignalsAdapter : SignalDataAdapter, IDisposable
{
    private readonly ReduxStore<GlobalState> _store;
    private readonly IList<IDisposable> _subscriptions = new List<IDisposable>();

    private ILookup<Guid, ISignalData> _lookup;

    public GlobalStateSignalsAdapter(ReduxStore<GlobalState> store)
    {
        _store = store;
        _store.Select(state => state.Tracks)
            .Subscribe(tracks =>
            {
                _lookup = tracks.ToLookup(t => t.Id, t => t.SignalData);

                NotifyDataChanged();
            })
            .PutOnRecord(_subscriptions);
    }

    public void Dispose()
    {
        _subscriptions.DisposeAll().Clear();
    }

    public override int SignalsCount => _lookup.Count;

    public override IEnumerable<Signal> EnumerateAllSignals()
    {
        foreach (var g in _lookup)
        {
            yield return new Signal(g.Key, g.First());
        }
    }
}
