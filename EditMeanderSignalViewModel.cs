using CommunityToolkit.Mvvm.ComponentModel;
using Meander.State;
using ReduxSimple;

namespace Meander;

internal sealed partial class EditMeanderSignalViewModel : ObservableObject, IEnableable
{
    private readonly ReduxStore<GlobalState> _store;
    private readonly IList<IDisposable> _subscriptions = new List<IDisposable>();

    [ObservableProperty]
    private string _testProp;

    public EditMeanderSignalViewModel(ReduxStore<GlobalState> store)
    {
        _store = store;
    }

    public void OnDisabled()
    {
        _subscriptions.DisposeAll().Clear();
    }

    public void OnEnabled()
    {
        _store.Select(s => $"{s.ProjectName} @ {s.SamplesCount}").Subscribe(s => TestProp = s);
    }
}
