using System.Globalization;
using CommunityToolkit.Maui.Converters;

namespace Meander;

public partial class MainPage : ContentPage
{
	public MainPage(MainViewModel vm)
	{
		InitializeComponent();

		BindingContext = vm;
        this.SubscribeBindingContextHandlers();
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

internal sealed class FromArgbToColorConverter : BaseConverterOneWay<string, Color>
{
    public override Color DefaultConvertReturnValue { get; set; } = Colors.Magenta;

    public override Color ConvertFrom(string value, CultureInfo culture) => Color.FromArgb(value);
}
