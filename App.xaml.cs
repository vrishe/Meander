namespace Meander;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		MainPage = new AppShell();
    }

    public static IMauiContext FindMauiContext(Element element = null)
    {
        while (element != null)
        {
            if (element.Handler is { MauiContext: not null })
                return element.Handler.MauiContext;

            element = element.Parent;
        }

        element = Current;
        while (element != null)
        {
            if (element.Handler is { MauiContext: not null })
                return element.Handler.MauiContext;

            element = element.Parent;
        }

        throw new InvalidOperationException($"Couldn't find {nameof(IMauiContext)} instance.");
    }
}
