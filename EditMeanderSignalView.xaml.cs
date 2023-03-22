using System.Collections;
using Microsoft.Extensions.Logging;

namespace Meander;

public partial class EditMeanderSignalView : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.CreateAttached(nameof(ItemsSource), typeof(ICollection), typeof(EditMeanderSignalView), null,
            propertyChanged: (bo, _, _) => (bo as EditMeanderSignalView)!.UpdateStepsGridContent());

    public EditMeanderSignalView()
    {
        InitializeComponent();
        this.WithEnableable();
    }

    public ICollection ItemsSource
    {
        get => GetItemsSource(this);
        set => SetItemsSource(this, value);
    }

    public static ICollection GetItemsSource(ContentView src) => (ICollection)src.GetValue(ItemsSourceProperty);
    public static void SetItemsSource(ContentView src, ICollection value) => src.SetValue(ItemsSourceProperty, value);

    private void UpdateStepsGridContent()
    {
        var itemsSource = ItemsSource;
        var toRemove = StepsGrid.Children.OfType<BindableObject>().Where(bo => Grid.GetRow(bo) >= itemsSource.Count);
        foreach (var item in toRemove.Cast<IView>())
            StepsGrid.Remove(item);
        while (StepsGrid.RowDefinitions.Count > itemsSource.Count)
            StepsGrid.RowDefinitions.RemoveAt(StepsGrid.RowDefinitions.Count - 1);
        while (StepsGrid.RowDefinitions.Count < itemsSource.Count)
        {
            var row = StepsGrid.RowDefinitions.Count;
            StepsGrid.RowDefinitions.Add(new RowDefinition());
            StepsGrid.Add(SetupGridView(FirstColumnTemplate.CreateContent(), row, 0));
            StepsGrid.Add(SetupGridView(SecondColumnTemplate.CreateContent(), row, 1));

            static IView SetupGridView(object viewBlank, int row, int column)
            {
                var result = (IView)viewBlank;
                var bo = viewBlank as BindableObject;
                Grid.SetColumn(bo, column);
                Grid.SetRow(bo, row);
                return result;
            }
        }

        var from = itemsSource.GetEnumerator();
        var to = StepsGrid.GetEnumerator();
        while (from.MoveNext())
        {
            to.MoveNext(); (to.Current as BindableObject)!.BindingContext = from.Current;
            to.MoveNext(); (to.Current as BindableObject)!.BindingContext = from.Current;
        }
    }
}
