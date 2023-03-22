using System.Collections;
using System.Globalization;

namespace Meander.Controls;

internal sealed class MappingConverter : IValueConverter
{
    public object FallbackValue { get; set; }

    public IDictionary Mapping { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var result = Mapping?[value];
        if (result is not null) return result;
        result = FallbackValue;
        if (result is string s) return string.Format(s, value);
        return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
