namespace Meander;

internal static class AttachedProperties
{
    public static readonly BindableProperty RegisteredRoutesProperty
        = BindableProperty.CreateAttached("RegisteredRoutes", typeof(IEnumerable<RegisteredRoute>), typeof(Shell), Enumerable.Empty<RegisteredRoute>());

    public static IEnumerable<RegisteredRoute> GetRegisteredRoutes(this Shell target) => target.GetValue(RegisteredRoutesProperty) as IEnumerable<RegisteredRoute>;
    public static void SetRegisteredRoutes(this Shell target, IEnumerable<RegisteredRoute> value) => target.SetValue(RegisteredRoutesProperty, value);

    public static readonly BindableProperty SelectorContext = BindableProperty.CreateAttached("_SelectorContext", typeof(object), typeof(AttachedProperties), null);
}

internal sealed class RegisteredRoute
{
    public string Route { get; set; }
    public Type PageType { get; set; }
}