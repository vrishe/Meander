using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Meander;

public sealed partial class StartUpViewModel
{
    private readonly Lazy<IFilePersistencyService> _filePersistency;
    private readonly ILogger<App> _logger;
    private readonly IShellNavigation _navigation;

    public StartUpViewModel(Lazy<IFilePersistencyService> filePersistency, ILogger<App> logger, IShellNavigation navigation)
    {
        _filePersistency = filePersistency;
        _logger = logger;
        _navigation = navigation;
    }

    [RelayCommand]
    private Task DoBeginNewProject() => _navigation.GoToAsync(Routes.ProjectSetupUrl);

    [RelayCommand]
    private async Task DoImportProject()
    {
        try
        {
            await _filePersistency.Value.ImportProjectAsync();
        }
        catch (Exception e)
        {
            if (e is not OperationCanceledException)
                _logger.LogError(e, nameof(DoImportProjectCommand));

            return;
        }

        await _navigation.GoToAsync(Routes.MainPageUrl);
    }
}
