using System;
using System.Globalization;
using System.Windows.Data;

namespace TICup2023.Tool.Converter;

public class MultiIntConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length == 0)
            throw new ArgumentNullException(nameof(values));
        return (int)values[0] == (int)values[1] ? values[0] : -1;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return new[] { value, value };
    }
}