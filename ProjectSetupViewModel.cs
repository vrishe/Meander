using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Meander.State;
using Meander.State.Actions;
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
    private int _samplesCount;

    public ProjectSetupViewModel(IShellNavigation navigation, ReduxStore<GlobalState> store)
    {
        _navigation = navigation;
        _store = store;
    }

    public bool CanCreateNewProject => !string.IsNullOrEmpty(ProjectName) && SamplesCount > 0;

    [ResourceValue]
    public string DefaultProjectName { set => ProjectName = value; }

    [ResourceValue]
    public int DefaultSamplesCount { set => SamplesCount = value; }

    [RelayCommand(CanExecute = nameof(CanCreateNewProject))]
    public Task DoCreateNewProject()
    {
        _store.Reset();
        _store.Dispatch(new CreateProjectAction {
            ProjectName = ProjectName, SamplesCount = SamplesCount });

        return _navigation.GoToAsync(Routes.MainPageUrl);
    }
}
