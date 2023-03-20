using System.Data;
using System.Runtime.CompilerServices;
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

internal sealed class EditSignalTrackTemplateSelector : DataTemplateSelector
{
    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        var ctx = GetOrCreateContext(container);

        switch (item)
        {
            case EditMeanderSignalViewModel vm:
                return GetOrCreateTemplate(ctx, typeof(EditMeanderSignalView), vm);
        }

        throw new NotImplementedException();
    }

    private static IMauiContext FindMauiContext(Element element)
    {
        bool visitApp;
        if (visitApp = element == null)
            element = Application.Current;

        loop:
        while (element != null)
        {
            if (element.Handler is { MauiContext: not null })
                return element.Handler.MauiContext;

            element = element.Parent;
        }

        if (!visitApp)
        {
            element = Application.Current;
            goto loop;
        }

        throw new InvalidOperationException($"Couldn't find {nameof(IMauiContext)} instance.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Context GetOrCreateContext(BindableObject container)
    {
        var ctx = container.GetValue(AttachedProperties.SelectorContext) as Context;
        if (ctx == null)
        {
            ctx = new Context { MauiContext = FindMauiContext(container as Element) };
            container.SetValue(AttachedProperties.SelectorContext, ctx);
        }

        return ctx;
    }

    private static DataTemplate GetOrCreateTemplate(Context context, Type ofType, object vm)
    {
        var cache = context.TemplatesCache;
        if (!cache.TryGetValue(ofType, out var template))
        {
            cache[ofType] = template = new DataTemplate(() => {
                var result = context.MauiContext.Services.GetService(ofType)
                    ?? Activator.CreateInstance(ofType);
                if (result is BindableObject bo)
                    bo.BindingContext = vm;
                return result;
            });
        }

        return template;
    }

    private record class Context
    {
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
