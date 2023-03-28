using CommunityToolkit.Maui.Views;
namespace Meander;

public partial class EditDifferenceSignalView : ContentView
{
	public EditDifferenceSignalView()
	{
		InitializeComponent();
        this.WithEnableable();
    }

	private Page Page { get; set; }

	protected override void OnParentSet()
	{
		base.OnParentSet();

		var parent = Parent;
		while (parent != null)
		{
			if (parent is Page page)
				Page = page;

			parent = parent.Parent;
		}
	}

    private void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
		var popup = (e.Parameter as DataTemplate)?.CreateContent() as TrackSelectorPopup;
		if (popup != null)
		{
			popup.BindingContext = BindingContext;
			Page.ShowPopup(popup);
		}

        //if (sender == FirstTrackSelect
        //    || sender == SecondTrackSelect)
        //{
        //    Page.ShowPopup(new TrackSelectorPopup(BindingContext, e.Parameter as IDictionary<string, Binding>));
        //    return;
        //}
    }
}
