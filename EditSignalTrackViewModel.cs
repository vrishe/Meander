using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private Color _trackColor;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DoSubmitCommand))]
    private string _trackName;

    [ObservableProperty]
    public bool _newTrack;

    public EditSignalTrackViewModel(IShellNavigation navigation, IStringLocalizer<App> localizer, ILoggerProvider loggerProvider, ReduxStore<GlobalState> store)
    {
        _navigation = navigation;
        _localizer = localizer;
        _logger = loggerProvider.CreateLogger(nameof(EditSignalTrackViewModel));
        _store = store;
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
}
