using Meander.Signals;

namespace Meander.State.Actions;

internal class UpdateSignalTrackAction
{
    public Guid Id;
    public string Name;
    public string Color;
    public SignalKind SignalKind;
    public ISignalData SignalData;
}
