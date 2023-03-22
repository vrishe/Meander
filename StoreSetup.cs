using Meander.State;
using ReduxSimple;

namespace Meander;

internal static class StoreSetup
{
    public static ReduxStore<GlobalState> NewStore()
    {
        return new ReduxStore<GlobalState>(ReducersImpl.Build());
    }
}
