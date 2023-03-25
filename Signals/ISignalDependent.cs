namespace Meander.Signals;

internal interface ISignalDependent
{
    IReadOnlyCollection<Guid> Dependencies { get; }
}
