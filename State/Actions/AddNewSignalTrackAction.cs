using Meander.Signals;

namespace Meander.State.Actions;

public sealed class AddNewSignalTrackAction
{
    public string Name;
    public string Color;
    public SignalKind SignalKind;
    public ISignalData SignalData;
}
