namespace Meander;

public partial class StartUpPage : ContentPage
{
	public StartUpPage(StartUpViewModel vm)
	{
		InitializeComponent();

		BindingContext = vm;
	}
}