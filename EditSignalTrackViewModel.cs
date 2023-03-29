using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Meander.Signals;
using Meander.State;
using Meander.State.Actions;
using Microsoft.Extensions.Logging;
using ReduxSimple;

namespace Meander;

using static Routes.EditSignalTrackQueryParams;

public sealed partial class EditSignalTrackViewModel : ObservableObject, IQueryAttributable
{
    private readonly ILogger _logger;
    private readonly IShellNavigation _navigation;
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

    private ISignalDataFactory _signalDataFactory;
    private SignalTrack _signalTrack;

    public EditSignalTrackViewModel(ILogger<App> logger, IShellNavigation navigation, ReduxStore<GlobalState> store)
    {
        _logger = logger;
        _navigation = navigation;
        _store = store;

        UpdateSignalTrackEditor();
    }

    [ResourceValue]
    public string DefaultTrackName { get; set; }

    public bool CanSubmit => !string.IsNullOrEmpty(TrackName) && _signalDataFactory.IsDataReady;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue(TrackId, out var trackId))
        {
            var trackName = DefaultTrackName;
            var similarNamedTracksCount = _store.State.Tracks.Count(t => t.Name.Equals(trackName, StringComparison.CurrentCultureIgnoreCase));
            TrackName = similarNamedTracksCount > 0 ? $"{trackName} ({similarNamedTracksCount})" : trackName;
            TrackColor = RandomizeColor(new Random(GetHashCode() ^ Environment.TickCount).NextSingle(), .64f, .7f);
            TrackSignalKind = default;
            NewTrack = true;
        }
        else
        {
            _signalTrack = _store.State.Tracks.FirstOrDefault(t => trackId.Equals(t.Id));
            TrackName = _signalTrack.Name;
            TrackColor = Color.FromArgb(_signalTrack.Color);
            TrackSignalKind = _signalTrack.SignalKind;
            NewTrack = false;
        }

        UpdateSignalTrackEditor();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(TrackSignalKind):
                UpdateSignalTrackEditor();
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
            _store.Dispatch(new AddNewSignalTrackAction {
                Name = TrackName,
                Color = TrackColor.ToHex(),
                SignalKind = TrackSignalKind,
                SignalData = _signalDataFactory.CreateSignalData()
            });
        }
        else
        {
            _store.Dispatch(new UpdateSignalTrackAction
            {
                Id = _signalTrack.Id,
                Name = TrackName,
                Color = TrackColor.ToHex(),
                SignalKind = TrackSignalKind,
                SignalData = _signalDataFactory.CreateSignalData()
            });
        }

        return _navigation.GoToAsync("../");
    }

    private void UpdateSignalTrackEditor()
    {
        TrackEditor = _signalDataFactory = TrackSignalKind switch
        {
            SignalKind.Difference => new EditDifferenceSignalViewModel(_store, _signalTrack,
                DoSubmitCommand.NotifyCanExecuteChanged),
            SignalKind.Meander => new EditMeanderSignalViewModel(_store, _signalTrack?.SignalData),
            _ => throw new NotImplementedException()
        };

        DoSubmitCommand.NotifyCanExecuteChanged();
    }

    internal interface ISignalDataFactory
    {
        public bool IsDataReady { get; }

        ISignalData CreateSignalData();
    }
}
