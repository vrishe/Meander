using CommunityToolkit.Maui;
using Meander.Signals;
using Meander.State;
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

		builder.Services.AddLocalization();

		builder.Services.AddSingleton(StoreSetup.NewStore());
		builder.Services.AddSingleton<IShellNavigation, CurrentShellNavigation>();
		builder.Services.AddSingleton<ISignalsEvaluator>(serviceProvider
			=> new SignalsEvaluator(serviceProvider.GetRequiredService<ILoggerProvider>())
			{
				Adapter = serviceProvider.GetRequiredService<GlobalStateSignalsAdapter>()
			});

		builder.Services.AddTransient<GlobalStateSignalsAdapter>();
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
