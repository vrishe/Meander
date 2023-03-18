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

    public static void PropagateResourceValuesToBindingContext(this Page page)
    {
        if (page.BindingContext is null) return;

        var bc = page.BindingContext;
        var bcType = bc.GetType();
        foreach (var (p, v) in page.Resources
            .Select(kvp => (p: bcType.GetProperty(kvp.Key), v: kvp.Value))
            .Where(t => t.p?.CanWrite == true && !t.p.SetMethod.IsStatic
                    && (
                        (t.v is null && t.p.PropertyType.IsClass)
                            || (t.v is not null && t.p.PropertyType.IsAssignableFrom(t.v.GetType())))
                    ))
        {
            p.SetValue(bc, v);
        }
    }
}
