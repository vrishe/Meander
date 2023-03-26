using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Meander.Signals;
using Meander.State;
using Meander.State.Actions;
using ReduxSimple;

namespace Meander;

public sealed partial class MainViewModel : ObservableObject, IEnableable
{
    private readonly IShellNavigation _navigation;
    private readonly ReduxStore<GlobalState> _store;
    private readonly IList<IDisposable> _subscriptions = new List<IDisposable>();

    [ObservableProperty]
    private string _projectName;

    public MainViewModel(IShellNavigation navigation, ReduxStore<GlobalState> store)
    {
        _navigation = navigation;
        _store = store;

        Tracks.Add(this);
    }

    public ObservableCollection<object> Tracks { get; } = new();

    public void OnDisabled()
    {
        _subscriptions.DisposeAll().Clear();
    }

    public void OnEnabled()
    {
        _store.Select(s => s.ProjectName)
            .DistinctUntilChanged()
            .Subscribe(pn => ProjectName = pn)
            .PutOnRecord(_subscriptions);

        _store.Select(s => s.Tracks)
            .DistinctUntilChanged()
            .Subscribe(t =>
            {
                var diff = Diff.Find(Tracks.SkipLast(1), t);
                foreach (var entry in diff)
                {
                    var n = entry.deletedA;
                    while (n-- > 0)
                        Tracks.RemoveAt(entry.StartA);
                    for (var i = 0; i < entry.insertedB; ++i)
                        Tracks.Insert(entry.StartA + i, t[entry.StartB + i]);
                }
            })
            .PutOnRecord(_subscriptions);
    }

    [RelayCommand]
    private Task DoAddNewSignalTrack()
    {
        var rng = new Random();
        _store.Dispatch(new AddNewSignalTrackAction
        {
            Name = "テストのツレック",
            Color = Colors.Teal.ToHex(),
            SignalKind = SignalKind.Meander,
            SignalData = new MeanderSignalData(Enumerable
        .Range(0, _store.State.SamplesCount)
        .Select(_ => 2 * (rng.NextDouble() - .5)))
        });

        return Task.CompletedTask;

        //return _navigation.GoToAsync(Routes.EditSignalTrackUrl);
    }

    [RelayCommand]
    private Task DoEditSignalTrack(Guid trackId)
    {
        return _navigation.GoToAsync(Routes.EditSignalTrackUrl,
            new Dictionary<string, object> {
                [Routes.EditSignalTrackQueryParams.TrackId] = trackId
            });
    }
}