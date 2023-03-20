using System.Globalization;

namespace Meander;

[ContentProperty(nameof(InnerConverter))]
internal sealed class RevertedConversion : IValueConverter
{
    public IValueConverter InnerConverter { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => InnerConverter.ConvertBack(value, targetType, parameter, culture);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => InnerConverter.ConvertBack(value, targetType, parameter, culture);
}
