using Meander.State;
using ReduxSimple;

namespace Meander;

using static ReduxSimple.Reducers;

internal static class StoreSetup
{
    public static ReduxStore<GlobalState> NewStore()
    {
        return new ReduxStore<GlobalState>(Reducers());
    }

    private static IEnumerable<On<GlobalState>> Reducers()
    {
        yield return On<Actions.CreateProjectAction, GlobalState>(
                (_, action) => new GlobalState { ProjectName = action.ProjectName, SamplesCount = action.SamplesCount });

        yield return On<Actions.AddNewSignalTrackAction, GlobalState>(
            (state, action) => state with {
                Tracks = state.Tracks.Concat(
                    FromOne(new SignalTrack {
                        Id = Guid.NewGuid(),
                        Name = action.Name, Color = action.Color }))
                .ToList()
            });
    }

    private static IEnumerable<T> FromOne<T>(T value)
    {
        yield return value;
    }
}
