using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Meander.Actions;
using Meander.State;
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

    public void OnDisable()
    {
        _subscriptions.DisposeAll().Clear();
    }

    public void OnEnable()
    {
        _store.Select(s => s.ProjectName)
            .DistinctUntilChanged()
            .Subscribe(pn => ProjectName = pn)
            .PutOnRecord(_subscriptions);

        _ = _store.Select(s => s.Tracks)
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
        return _navigation.GoToAsync(Routes.EditSignalTrackUrl);
    }
}