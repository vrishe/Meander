using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace Meander;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp
			.CreateBuilder()
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseSkiaSharp()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

		builder.Services.AddSingleton<IShellNavigation, CurrentShellNavigation>();
		builder.Services.AddSingleton(StoreSetup.NewStore());

		builder.Services.AddTransient<EditSignalTrackPage>();
		builder.Services.AddTransient<EditSignalTrackViewModel>();
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<MainViewModel>();
		builder.Services.AddTransient<ProjectSetupPage>();
		builder.Services.AddTransient<ProjectSetupViewModel>();
		builder.Services.AddTransient<StartUpPage>();
		builder.Services.AddTransient<StartUpViewModel>();

		return builder.Build();
	}
}
