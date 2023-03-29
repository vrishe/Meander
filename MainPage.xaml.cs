namespace Meander;

public partial class MainPage : ContentPage
{
	public MainPage(MainViewModel vm)
	{
		InitializeComponent();

		BindingContext = vm;
        this.WithMenuBarItemsFix()
            .WithEnableable();
	}
}

internal sealed class SignalTrackTemplateSelector : DataTemplateSelector
{
    public DataTemplate Regular { get; set; }
    public DataTemplate Trailing { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        return item is not MainViewModel ? Regular : Trailing;
    }
}
