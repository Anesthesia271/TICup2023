using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using AForge.Video.DirectShow;

namespace TICup2023.Tool.Converter;

public class FilterInfoCollection2StringArrayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not FilterInfoCollection filterInfoCollection)
            return Array.Empty<string>();
        var cameraDevices = new string[filterInfoCollection.Count];
        for (var i = 0; i < filterInfoCollection.Count; i++)
            cameraDevices[i] = filterInfoCollection[i].Name;
        return (from FilterInfo filterInfo in filterInfoCollection select filterInfo.Name).ToList();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new Exception("单向绑定的数据无法进行反向值转换");
    }
}