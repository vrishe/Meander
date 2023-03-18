namespace Meander;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		var registeredRoutes = this.GetRegisteredRoutes();
		if (registeredRoutes is not null)
		{
			foreach (var route in registeredRoutes)
				Routing.RegisterRoute(route.Route, route.PageType);
		}
    }
}
