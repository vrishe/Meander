using System.Globalization;
using CommunityToolkit.Maui.Converters;
using CommunityToolkit.Maui.Views;

namespace Meander;

public partial class EditSignalTrackPage : ContentPage
{
	public EditSignalTrackPage(EditSignalTrackViewModel vm)
	{
		InitializeComponent();

		BindingContext = vm;
		this.PropagateResourceValuesToBindingContext();
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        var box = sender as BoxView;
        var popup = new ColorPickerPopup { PickedColor = box.Color };

        await this.ShowPopupAsync(popup);

        box.Color = popup.PickedColor;
    }
}

internal sealed class BooleanConverter : BaseConverterOneWay<bool, object>
{
    public object TrueValue { get; set; }
    public object FalseValue { get; set; }

    public override object DefaultConvertReturnValue { get; set; }

    public override object ConvertFrom(bool value, CultureInfo culture) => value ? TrueValue : FalseValue;
}