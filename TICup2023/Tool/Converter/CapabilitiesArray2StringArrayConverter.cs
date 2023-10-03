using System;
using System.Globalization;
using System.Windows.Data;
using AForge.Video.DirectShow;

namespace TICup2023.Tool.Converter;

public class CapabilitiesArray2StringArrayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not VideoCapabilities[] capabilities)
            return Array.Empty<string>();
        var capabilitiesArray = new string[capabilities.Length];
        for (var i = 0; i < capabilities.Length; i++)
            capabilitiesArray[i] = capabilities[i].FrameSize.Width + "x" + capabilities[i].FrameSize.Height;
        return capabilitiesArray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new Exception("单向绑定的数据无法进行反向值转换");
    }
}