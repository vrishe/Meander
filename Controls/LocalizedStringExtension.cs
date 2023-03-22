using Microsoft.Extensions.Localization;

namespace Meander.Controls;

[ContentProperty(nameof(Key))]
internal sealed class LocalizedStringExtension : IMarkupExtension
{
    public string Key { get; set; }

    public Type ResourceType { get; set; } = typeof(Resources.Strings);

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        var ctx = App.FindMauiContext(GetCurrentElement(serviceProvider));
        var localizer = ctx.Services.GetRequiredService(typeof(IStringLocalizer<>).MakeGenericType(ResourceType)) as IStringLocalizer;
        return localizer![Key];
    }

    private static Element GetCurrentElement(IServiceProvider serviceProvider)
    {
        var service = serviceProvider.GetService(typeof(IProvideValueTarget));
        if (service is IProvideValueTarget pvt) return pvt.TargetObject as Element;

        service = serviceProvider.GetService<IRootObjectProvider>();
        if (service is IRootObjectProvider rop) return rop.RootObject as Element;

        return null;
    }
}
