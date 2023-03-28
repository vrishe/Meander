using System.Collections;
using CommunityToolkit.Maui.Views;
using System.Windows.Input;

namespace Meander;

public partial class TrackSelectorPopup : Popup
{
	public static readonly BindableProperty ItemsSourceProperty =
		BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(TrackSelectorPopup));

	public IEnumerable ItemsSource
	{
		get => (IEnumerable)GetValue(ItemsSourceProperty);
		set => SetValue(ItemsSourceProperty, value);
	}

	public static readonly BindableProperty SelectedItemProperty =
		BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(TrackSelectorPopup),
			defaultBindingMode: BindingMode.OneWay);

	public object SelectedItem
	{
		get => GetValue(SelectedItemProperty);
		set => SetValue(SelectedItemProperty, value);
	}

	public static readonly BindableProperty SelectionChangedCommandProperty =
		BindableProperty.Create(nameof(SelectionChangedCommand), typeof(ICommand), typeof(TrackSelectorPopup));

	public ICommand SelectionChangedCommand
	{
		get => (ICommand)GetValue(SelectionChangedCommandProperty);
		set => SetValue(SelectionChangedCommandProperty, value);
	}

	public TrackSelectorPopup()
	{
		InitializeComponent();
		Dispatcher.Dispatch(() => ItemsList.SelectionChanged += ItemsList_SelectionChanged);
	}

    private void ItemsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
		ItemsList.SelectionChanged -= ItemsList_SelectionChanged;
		BindingContext = null;
        Close();

	}
}