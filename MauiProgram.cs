using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace Meander;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp
			.CreateBuilder()
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		builder.Services.AddLocalization();

		builder.Services.AddSingleton<IShellNavigation, CurrentShellNavigation>();
        builder.Services.AddSingleton(StoreSetup.NewStore());

		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<StartUpPage>();
        builder.Services.AddTransient<StartUpViewModel>();
        builder.Services.AddTransient<ProjectSetupPage>();
        builder.Services.AddTransient<ProjectSetupViewModel>();

        return builder.Build();
	}
}
