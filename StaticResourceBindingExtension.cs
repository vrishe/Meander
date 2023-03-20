namespace Meander;

[ContentProperty(nameof(Key))]
internal sealed class StaticResourceBindingExtension : IMarkupExtension
{
    private readonly StaticResourceExtension _inner = new();

    public BindingBase Binding { get; set; }

    public string Key
    {
        get => _inner.Key;
        set => _inner.Key = value;
    }

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        return new _(_inner.ProvideValue(serviceProvider), Binding).GetValue();
    }

    private class _ : BindableObject
    {
        private static readonly BindableProperty ValueProperty
            = BindableProperty.Create("Value", typeof(object), typeof(_));

        public _(object ctx, BindingBase binding)
        {
            BindingContext = ctx;
            SetBinding(ValueProperty, binding);
        }

        public object GetValue()
        {
            ApplyBindings();
            return GetValue(ValueProperty);
        }
    }
}
