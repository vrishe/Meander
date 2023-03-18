using CommunityToolkit.Maui.Behaviors;

namespace Meander;

public partial class ProjectSetupPage : ContentPage
{
	public ProjectSetupPage(ProjectSetupViewModel vm)
	{
		InitializeComponent();

		BindingContext = vm;

        ProjectNameEntry.Text = ProjectNameEntry.GetDefaultText();
        ProjectNameEntry.CursorPosition = ProjectNameEntry.Text?.Length ?? 0;
        ProjectNameEntry.Focus();
        SamplesCountEntry.Text = SamplesCountEntry.GetDefaultText();
    }
}