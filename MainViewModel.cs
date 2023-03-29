using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Meander.State;
using Meander.State.Actions;
using Microsoft.Extensions.Logging;
using ReduxSimple;

namespace Meander;

public sealed partial class MainViewModel : ObservableObject, IEnableable
{
    private readonly Lazy<IFilePersistencyService> _filePersistency;
    private readonly ILogger _logger;
    private readonly IShellNavigation _navigation;
    private readonly ReduxStore<GlobalState> _store;
    private readonly IList<IDisposable> _subscriptions = new List<IDisposable>();

    [ObservableProperty]
    private string _projectName;

    public MainViewModel(Lazy<IFilePersistencyService> filePersistency, ILogger<App> logger, IShellNavigation navigation, ReduxStore<GlobalState> store)
    {
        _filePersistency = filePersistency;
        _logger = logger;
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
                var iOffset = 0;
                var rOffset = 0;
                var diff = Diff.Find(Tracks.SkipLast(1), t);
                foreach (var entry in diff)
                {
                    var n = entry.deletedA;
                    while (n-- > 0)
                        Tracks.RemoveAt(entry.StartA + rOffset);
                    rOffset -= entry.deletedA;

                    for (var i = 0; i < entry.insertedB; ++i)
                        Tracks.Insert(entry.StartA + i + iOffset, t[entry.StartB + i]);
                    iOffset += entry.insertedB;
                }
            })
            .PutOnRecord(_subscriptions);
    }

    [RelayCommand]
    private Task DoAddNewSignalTrack() =>
        _navigation.GoToAsync(Routes.EditSignalTrackUrl);

    [RelayCommand]
    private Task DoBeginNewProject() => _navigation.GoToAsync(Routes.ProjectSetupUrl);

    [RelayCommand]
    private void DoDeleteSignalTrack(SignalTrack track)
    {
        if (track == null) return;

        _store.Dispatch(new DeleteSignalTrackAction { TrackId = track.Id });
    }

    [RelayCommand]
    private Task DoEditSignalTrack(Guid trackId) =>
        _navigation.GoToAsync(Routes.EditSignalTrackUrl,
            new Dictionary<string, object> {
                [Routes.EditSignalTrackQueryParams.TrackId] = trackId
            });

    [RelayCommand]
    private Task DoExportProject() => _filePersistency.Value.ExportProjectAsync();

    [RelayCommand]
    private async Task DoImportProject()
    {
        try
        {
            await _filePersistency.Value.ImportProjectAsync();
        }
        catch (Exception e)
        {
            if (e is not OperationCanceledException)
                _logger.LogError(e, nameof(DoImportProjectCommand));

            return;
        }
    }

    [RelayCommand]
    private void DoQuit() => Application.Current.Quit();
}