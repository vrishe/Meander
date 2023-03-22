using System.Globalization;

namespace Meander.Controls;

internal class SingleObjectToArray : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => new object[] { value };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => ((object[])value)[0];
}
