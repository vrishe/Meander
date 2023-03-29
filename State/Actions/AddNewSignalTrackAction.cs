using Meander.Signals;

namespace Meander.State.Actions;

internal sealed class AddNewSignalTrackAction
{
    public string Name;
    public string Color;
    public SignalKind SignalKind;
    public ISignalData SignalData;
}
