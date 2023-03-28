using Meander.State.Actions;
using ReduxSimple;

namespace Meander.State;

using static ReduxSimple.Reducers;

internal static class ReducersImpl
{
    public static IEnumerable<On<GlobalState>> Build()
    {
        yield return On<CreateProjectAction, GlobalState>(
                (_, action) => new GlobalState { ProjectName = action.ProjectName, SamplesCount = action.SamplesCount });

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

        yield return On<DeleteSignalTrackAction, GlobalState>(
            (state, action) => state with
            {
                Tracks = state.Tracks.Where(t => t.Id != action.TrackId).ToList()
            });

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
