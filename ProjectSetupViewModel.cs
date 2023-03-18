using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Meander.Actions;
using Meander.State;
using ReduxSimple;

namespace Meander;

public partial class ProjectSetupViewModel : ObservableObject
{
    private readonly IShellNavigation _navigation;
    private readonly ReduxStore<GlobalState> _store;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DoCreateNewProjectCommand))]
    private string _projectName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DoCreateNewProjectCommand))]
    private string _samplesCountText;

    public bool CanCreateNewProject => !string.IsNullOrEmpty(ProjectName)
        && Validation.CheckIsPositiveInteger(SamplesCountText);

    public ProjectSetupViewModel(IShellNavigation navigation, ReduxStore<GlobalState> store)
    {
        _navigation = navigation;
        _store = store;
    }

    [RelayCommand(CanExecute = nameof(CanCreateNewProject))]
    public Task DoCreateNewProject()
    {
        _store.Reset();
        _store.Dispatch(new CreateProjectAction {
            ProjectName = ProjectName, SamplesCount = int.Parse(SamplesCountText) });

        return _navigation.GoToAsync(Routes.MainPageUrl);
    }
}
