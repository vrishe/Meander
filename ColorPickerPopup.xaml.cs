using CommunityToolkit.Maui.Views;

namespace Meander;

public partial class ColorPickerPopup : Popup
{
	public ColorPickerPopup()
	{
		InitializeComponent();
	}

	public Color PickedColor
	{
		get => Picker.PickedColor;
		set => Picker.PickedColor = value;
	}

    private void Button_Clicked(object sender, EventArgs e)
    {
		Close();
    }
}