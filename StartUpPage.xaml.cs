namespace Meander;

public partial class StartUpPage : ContentPage
{
	public string Foo { get; set; }

	public StartUpPage(StartUpViewModel vm)
	{
		InitializeComponent();

		BindingContext = vm;
		Foo = Meander.Resources.Strings.AppTitle;
    }
}