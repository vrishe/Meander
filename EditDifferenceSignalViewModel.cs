using CommunityToolkit.Mvvm.ComponentModel;
using Meander.Signals;
using Meander.State;
using ReduxSimple;
using static Meander.EditSignalTrackViewModel;

namespace Meander;

using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public sealed partial class EditDifferenceSignalViewModel : ObservableObject, IEnableable, ISignalDataFactory
{
    private readonly SignalTrack _editedTrack;
    private readonly Action _onIsDataReadyChanged;
    private readonly DifferenceSignalData _signalData;
    private readonly ReduxStore<GlobalState> _store;
    private readonly IList<IDisposable> _subscriptions = new List<IDisposable>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFirstTrack))]
    [NotifyPropertyChangedFor(nameof(IsDataReady))]
    private SignalTrack _firstTrack;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSecondTrack))]
    [NotifyPropertyChangedFor(nameof(IsDataReady))]
    private SignalTrack _secondTrack;

    [ObservableProperty]
    private IEnumerable<SignalTrack> _tracks;

    public EditDifferenceSignalViewModel(ReduxStore<GlobalState> store, SignalTrack track, Action onIsDataReadyChanged)
    {
        _editedTrack = track;
        _onIsDataReadyChanged = onIsDataReadyChanged;
        _signalData = track?.SignalData as DifferenceSignalData;
        _store = store;
    }

    public bool HasFirstTrack => FirstTrack != null;
    public bool HasSecondTrack => SecondTrack != null;

    public bool IsDataReady => HasFirstTrack && HasSecondTrack;

    public void OnDisabled()
    {
        _subscriptions.DisposeAll().Clear();
    }

    public void OnEnabled()
    {
        if (_signalData != null)
        {
            Tracks = FilterTracks(_store.State.Tracks);
            FirstTrack = Tracks.FirstOrDefault(t => t.Id == _signalData.MinuendSignalId);
            SecondTrack = Tracks.FirstOrDefault(t => t.Id == _signalData.SubtrahendSignalId);
        }

        _store.Select(state => state.Tracks)
            .Subscribe(tracks => Tracks = FilterTracks(tracks))
            .PutOnRecord(_subscriptions);
    }

    ISignalData ISignalDataFactory.CreateSignalData() => new DifferenceSignalData(FirstTrack.Id, SecondTrack.Id);

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(IsDataReady):
                _onIsDataReadyChanged?.Invoke();
                break;
        }
    }

    private IEnumerable<SignalTrack> FilterTracks(IEnumerable<SignalTrack> tracks)
    {
        if (_editedTrack != null)
            return tracks.Where(t => t.Id != _editedTrack.Id);

        return tracks;
    }

    [RelayCommand]
    private void DoSelectFirstTrack(SignalTrack track)
    {
        if (track is null || track == FirstTrack) return;
        if (track != SecondTrack) FirstTrack = track;
        else (FirstTrack, SecondTrack) = (SecondTrack, FirstTrack);
    }

    [RelayCommand]
    private void DoSelectSecondTrack(SignalTrack track)
    {
        if (track is null || track == SecondTrack) return;
        if (track != FirstTrack) SecondTrack = track;
        else (FirstTrack, SecondTrack) = (SecondTrack, FirstTrack);
    }
}
