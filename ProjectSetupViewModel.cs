using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Meander.Actions;
using Meander.State;
using Microsoft.Extensions.Localization;
using ReduxSimple;

namespace Meander;

public partial class ProjectSetupViewModel : ObservableObject, IFinishedApplyResourceValues
{
    private readonly IShellNavigation _navigation;
    private readonly IStringLocalizer<App> _localizer;
    private readonly ReduxStore<GlobalState> _store;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DoCreateNewProjectCommand))]
    private string _projectName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DoCreateNewProjectCommand))]
    private string _samplesCountText;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DoCreateNewProjectCommand))]
    private int _samplesCount;

    public ProjectSetupViewModel(IShellNavigation navigation, IStringLocalizer<App> localizer, ReduxStore<GlobalState> store)
    {
        _localizer = localizer;
        _navigation = navigation;
        _store = store;
    }

    public bool CanCreateNewProject => !string.IsNullOrEmpty(ProjectName) && SamplesCount > 0;

    [ResourceValue]
    public string DefaultProjectName { get; set; }

    [ResourceValue]
    public int DefaultSamplesCount { get; set; }

    [RelayCommand(CanExecute = nameof(CanCreateNewProject))]
    public Task DoCreateNewProject()
    {
        _store.Reset();
        _store.Dispatch(new CreateProjectAction {
            ProjectName = ProjectName, SamplesCount = SamplesCount });

        return _navigation.GoToAsync(Routes.MainPageUrl);
    }

    public void OnResourceValuesApplied()
    {
        SamplesCount = DefaultSamplesCount;
        ProjectName = _localizer.GetString(DefaultProjectName);
    }
}
