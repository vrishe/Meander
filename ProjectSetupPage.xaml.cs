using CommunityToolkit.Maui.Behaviors;

namespace Meander;

public partial class ProjectSetupPage : ContentPage
{
	public ProjectSetupPage(ProjectSetupViewModel vm)
	{
		InitializeComponent();

		BindingContext = vm;
        this.FillBindingContextResourceValues();

        ProjectNameEntry.CursorPosition = ProjectNameEntry.Text?.Length ?? 0;
    }
}