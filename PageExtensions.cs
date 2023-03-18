namespace Meander;

internal static class PageExtensions
{
    public static void SubscribeBindingContextHandlers(this Page page)
    {
        if (page?.BindingContext is IEnableable enableable)
        {
            page.Appearing += (_, _) => enableable.OnEnable();
            page.Disappearing += (_, _) => enableable.OnDisable();
        }
    }
}
