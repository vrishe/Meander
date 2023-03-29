using CommunityToolkit.Maui.Views;

namespace Meander;

public partial class ColorPickerPopup : Popup
{
	public ColorPickerPopup(Color initialColor)
	{
		InitializeComponent();
		this.ApplyPopupStyle();

		PickedColor = initialColor;
	}

    public Color PickedColor
	{
		get => Picker.PickedColor;
		private set => Picker.PickedColor = value;
	}

    private void Button_Clicked(object sender, EventArgs e)
    {
		Close(PickedColor);
    }
}