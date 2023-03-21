using CommunityToolkit.Mvvm.ComponentModel;
using Meander.State;
using ReduxSimple;

namespace Meander;

public sealed partial class EditMeanderSignalViewModel : ObservableObject, IEnableable
{
    private readonly ReduxStore<GlobalState> _store;
    private readonly IList<IDisposable> _subscriptions = new List<IDisposable>();

    [ObservableProperty]
    private string _testProp;

    [ObservableProperty]
    private ICollection<LevelInputData> _steps;

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
        var state = _store.State;
        Steps = Enumerable.Range(1, state.SamplesCount)
            .Select(n => new LevelInputData { Number = n })
            .ToList();
    }
}

public sealed class LevelInputData
{
    public int Number { get; set; }

    public double Value { get; set; }
}