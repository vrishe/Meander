using Meander.Signals;
using Meander.State.Actions;
using ReduxSimple;

namespace Meander.State;

using static ReduxSimple.Reducers;

internal static class ReducersImpl
{
    public static IEnumerable<On<GlobalState>> Build()
    {
        yield return On<AddNewSignalTrackAction, GlobalState>(
            (state, action) => state with
            {
                Tracks = state.Tracks.Concat(
                    new SignalTrack
                    {
                        Id = Guid.NewGuid(),
                        Name = action.Name,
                        Color = action.Color,
                        SignalKind = action.SignalKind,
                        SignalData = action.SignalData,
                    })
                .ToList()
            });

        yield return On<CreateProjectAction, GlobalState>(
            (_, action) => new GlobalState
            {
                ProjectName = action.ProjectName,
                SamplesCount = action.SamplesCount
            });

        yield return On<DeleteSignalTrackAction, GlobalState>(
            (state, action) =>
            {
                var map = state.Tracks.ToLookup(t => t.Id);
                bool Unrelated(SignalTrack t) => t.Id != action.TrackId
                    && t.SignalData?.Dependencies.SelectMany(id => map[id]).All(Unrelated) != false;

                return state with { Tracks = state.Tracks.Where(Unrelated).ToList() };
            });

        yield return On<ReplaceStateAction, GlobalState>((state, action) => action.NewState ?? state);

        yield return On<UpdateSignalTrackAction, GlobalState>(
            (state, action) => state with
            {
                Tracks = state.Tracks.WithUpdatedElement(
                    t => t.Id == action.Id,
                    t => new SignalTrack
                    {
                        Id = t.Id,
                        Name = action.Name,
                        Color = action.Color,
                        SignalKind = action.SignalKind,
                        SignalData = action.SignalData,
                        Version = t.Version + 1,
                    })
            });
    }
}
