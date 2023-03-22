using CommunityToolkit.Mvvm.ComponentModel;
using Meander.Signals;
using Meander.State;
using ReduxSimple;

namespace Meander;

using static EditSignalTrackViewModel;

public sealed partial class EditMeanderSignalViewModel : ObservableObject, IEnableable, ISignalDataFactory
{
    private readonly MeanderSignalData _signalData;
    private readonly ReduxStore<GlobalState> _store;
    private readonly IList<IDisposable> _subscriptions = new List<IDisposable>();

    [ObservableProperty]
    private string _testProp;

    [ObservableProperty]
    private ICollection<LevelInputData> _steps;

    public EditMeanderSignalViewModel(ReduxStore<GlobalState> store, ISignalData signalData)
    {
        _signalData = signalData as MeanderSignalData;
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
            .Select((n, i) => new LevelInputData {
                Number = n,
                Value = _signalData?.SamplesCount > i ? _signalData[i] : 0
            })
            .ToList();
    }

    ISignalData ISignalDataFactory.CreateSignalData()
    {
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        return new MeanderSignalData(_steps.Select(d => d.Value));
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
    }
}

public sealed class LevelInputData
{
    public int Number { get; set; }

    public double Value { get; set; }
}