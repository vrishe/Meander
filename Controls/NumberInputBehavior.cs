using System.Globalization;

namespace Meander.Controls;

internal abstract class NumberInputBehaviourBase<T> : Behavior<InputView>
{
#pragma warning disable RCS1158 // Static member in generic type should use a type parameter.
    public static readonly BindableProperty ValueProperty =
#pragma warning restore RCS1158 // Static member in generic type should use a type parameter.
        BindableProperty.Create(nameof(Value), typeof(T), typeof(FloatNumberInputBehaviour), default(T), BindingMode.TwoWay,
            propertyChanged: OnValueChanged);

    private InputView _input;
    private string _validText;
    private bool _isInValidation;

    protected NumberInputBehaviourBase(string defaultValueText)
    {
        DefaultValueText = _validText = defaultValueText;
    }

    public T Value
    {
        get { return (T)GetValue(ValueProperty); }
        set { SetValue(ValueProperty, value); }
    }

    protected string DefaultValueText { get; }

    protected override void OnAttachedTo(InputView bindable)
    {
        base.OnAttachedTo(bindable);

        if (_input != null)
            throw new InvalidOperationException($"This {nameof(FloatNumberInputBehaviour)} ({GetHashCode()}) is already attahced.");

        _input = bindable;
        _input.RemoveBinding(InputView.TextProperty);
        _input.BindingContextChanged += OnInputBindingContextChanged;
        _input.Focused += OnFocused;
        _input.TextChanged += OnTextChanged;
        _input.Unfocused += OnUnfocused;

        OnInputBindingContextChanged(_input, null);
    }

    protected abstract string ToStringValue(T value);

    protected abstract bool TryParseValue(string text, out T value);

    protected override void OnDetachingFrom(InputView bindable)
    {
        if (_input == bindable)
        {
            _input.BindingContextChanged -= OnInputBindingContextChanged;
            _input.Focused -= OnFocused;
            _input.TextChanged -= OnTextChanged;
            _input.Unfocused -= OnUnfocused;
            _input = null;
        }

        base.OnDetachingFrom(bindable);
    }

    private static void OnValueChanged(BindableObject bo, object oldValue, object newValue)
    {
        (bo as NumberInputBehaviourBase<T>)?.PropagateText();
    }

    private void OnFocused(object sender, FocusEventArgs e)
    {
        ValidateInputText();
    }

    private void OnInputBindingContextChanged(object sender, EventArgs e)
    {
        SetInheritedBindingContext(this, _input.BindingContext);

        OnTextChanged(_input, null);
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ValidateInputText();

        if (!_input.IsFocused)
            _input.Text = _validText;
    }

    private void OnUnfocused(object sender, FocusEventArgs e)
    {
        _input.Text = _validText;
    }

    private void PropagateText()
    {
        if (_isInValidation) return;

        _input.Text = _validText = ToStringValue(Value);
    }

    private void ValidateInputText()
    {
        _isInValidation = true;

        try
        {
            if (string.IsNullOrEmpty(_input.Text))
            {
                _validText = DefaultValueText;
                SetValue(ValueProperty, ValueProperty.DefaultValue);
                return;
            }

            if (TryParseValue(_input.Text, out var value))
            {
                _validText = _input.Text;
                Value = value;
            }
        }
        finally
        {
            _isInValidation = false;
        }
    }
}

internal sealed class IntegerNumberInputBehaviour : NumberInputBehaviourBase<long>
{
    public IntegerNumberInputBehaviour()
        : base(string.Format(CultureInfo.CurrentUICulture, "{0:D}", ValueProperty.DefaultValue)) { }

    public NumberStyles NumberStyles { get; set; } = NumberStyles.Integer;

    protected override string ToStringValue(long value) => string.Format(CultureInfo.CurrentUICulture, "{0:D}", value);

    protected override bool TryParseValue(string text, out long value)
        => long.TryParse(text, NumberStyles, CultureInfo.CurrentUICulture, out value);
}

internal sealed class FloatNumberInputBehaviour : NumberInputBehaviourBase<double>
{
    public FloatNumberInputBehaviour()
        : base(string.Format(CultureInfo.CurrentUICulture, "{0:F1}", ValueProperty.DefaultValue)) { }

    public NumberStyles NumberStyles { get; set; } = NumberStyles.Float | NumberStyles.AllowThousands;

    protected override string ToStringValue(double value) => string.Format(CultureInfo.CurrentUICulture, "{0:F}", value);

    protected override bool TryParseValue(string text, out double value)
        => double.TryParse(text, NumberStyles, CultureInfo.CurrentUICulture, out value);
}
