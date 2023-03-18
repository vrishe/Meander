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
    private readonly Random _rng = new();
    private readonly ReduxStore<GlobalState> _store;
    private readonly IList<IDisposable> _subscriptions = new List<IDisposable>();

    [ObservableProperty]
    private string _projectName;

    public MainViewModel(ReduxStore<GlobalState> store)
    {
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
    private void DoAddNewSignalTrack()
    {
        _store.Dispatch(new AddNewSignalTrackAction
        {
            Name = "puk-puk",
            Color = RandomizeColor(_rng.NextSingle(), .64f, .7f).ToHex()
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Color RandomizeColor(float r, float s, float v)
    {
        const float phiRecip = (float)(1 / 1.61803398875);
        var h = (r + phiRecip) % 1;
        return Color.FromHsv(h, s, v);
    }
}