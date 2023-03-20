using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Meander.Signals;
using Meander.State;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using ReduxSimple;

namespace Meander;

using static Routes.EditSignalTrackQueryParams;

public sealed partial class EditSignalTrackViewModel : ObservableObject, IQueryAttributable
{
    private readonly IShellNavigation _navigation;
    private readonly IStringLocalizer _localizer;
    private readonly ILogger _logger;
    private readonly ReduxStore<GlobalState> _store;

    [ObservableProperty]
    private bool _newTrack;

    [ObservableProperty]
    private Color _trackColor;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DoSubmitCommand))]
    private string _trackName;

    [ObservableProperty]
    private SignalKind _trackSignalKind;

    [ObservableProperty]
    private object _trackEditor;

    public EditSignalTrackViewModel(IShellNavigation navigation, IStringLocalizer<App> localizer, ILoggerProvider loggerProvider, ReduxStore<GlobalState> store)
    {
        _navigation = navigation;
        _localizer = localizer;
        _logger = loggerProvider.CreateLogger(nameof(EditSignalTrackViewModel));
        _store = store;

        UpdateTrackSignalEditor();
    }

    public string DefaultTrackName { get; set; }
    public Color DefaultTrackColor { get; set; }

    public bool CanSubmit => !string.IsNullOrEmpty(TrackName);

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(TrackId, out var trackId))
        {
            var trackName = _localizer[DefaultTrackName];
            var similarNamedTracksCount = _store.State.Tracks.Count(t => t.Name.Equals(trackName, StringComparison.CurrentCultureIgnoreCase));
            TrackName = similarNamedTracksCount > 0 ? $"{trackName} ({similarNamedTracksCount})" : trackName;
            TrackColor = RandomizeColor(new Random(GetHashCode() ^ Environment.TickCount).NextSingle(), .64f, .7f);
            NewTrack = true;
            return;
        }

        var track = _store.State.Tracks.FirstOrDefault(t => trackId.Equals(t.Id));
        TrackName = track.Name;
        TrackColor = Color.FromArgb(track.Color);
        NewTrack = false;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(TrackSignalKind):
                UpdateTrackSignalEditor();
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Color RandomizeColor(float r, float s, float v)
    {
        const float phiRecip = (float)(1 / 1.61803398875);
        var h = (r + phiRecip) % 1;
        return Color.FromHsv(h, s, v);
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private Task DoSubmit()
    {
        if (NewTrack)
        {
            _store.Dispatch(new Actions.AddNewSignalTrackAction {
                Color = TrackColor.ToHex(),
                Name = TrackName
            });
        }
        else
        {
        }

        return _navigation.GoToAsync("../");
    }

    private void UpdateTrackSignalEditor()
    {
        TrackEditor = TrackSignalKind switch
        {
            SignalKind.Meander => new EditMeanderSignalViewModel(_store),
            _ => throw new NotImplementedException()
        };
    }
}
