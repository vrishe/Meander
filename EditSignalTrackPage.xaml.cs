using CommunityToolkit.Maui.Views;

namespace Meander;

public partial class EditSignalTrackPage : ContentPage
{
	public EditSignalTrackPage(EditSignalTrackViewModel vm)
	{
		InitializeComponent();

		BindingContext = vm;
		this.FillBindingContextResourceValues();
    }

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
    {
        var box = sender as BoxView;
        var popup = new ColorPickerPopup { PickedColor = box.Color };

        await this.ShowPopupAsync(popup);

        box.Color = popup.PickedColor;
    }
}

internal sealed class EditSignalTrackTemplateSelector : DataTemplateSelector
{
    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        => item switch
        {
            EditMeanderSignalViewModel => GetOrCreateTemplate(container, typeof(EditMeanderSignalView)),
            _ => throw new NotImplementedException(),
        };

    private static DataTemplate GetOrCreateTemplate(BindableObject container, Type ofType)
    {
        var ctx = container.GetValue(AttachedProperties.SelectorContext) as Context;
        if (ctx == null)
        {
            ctx = new Context { MauiContext = App.FindMauiContext(container as Element) };
            container.SetValue(AttachedProperties.SelectorContext, ctx);
        }

        var cache = ctx.TemplatesCache;
        if (!cache.TryGetValue(ofType, out var template))
        {
            cache[ofType] = template = new DataTemplate(()
                => ctx.MauiContext.Services.GetService(ofType)
                    ?? Activator.CreateInstance(ofType));
        }

        return template;
    }

    private record class Context
    {
        // TODO: not sure it is OK caching MAUI context here..
        public required IMauiContext MauiContext { get; init; }
        public Dictionary<Type, DataTemplate> TemplatesCache { get; init; } = new();
    }
}

internal sealed class PickerDataSourceExtension : DictExtensionBase
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new
        {
            Items = Entries.ConvertAll(entry => entry.Key),
            DisplayMapping = Entries.ToDictionary(entry => entry.Key, entry => entry.Value),
        };
    }
}
