using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Linq;

namespace emotions_wpf;

[ValueConversion(typeof(Dictionary<string, float>), typeof(String))]
public class Converter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string converted = "";

        Dictionary<string, float> list = new Dictionary<string, float>((Dictionary<string, float>)value);
        // order emotions for each image in descending order
        var ordered = list.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

        foreach (var item in ordered)
            converted += $"{item.Key}: {item.Value}\n";
        return converted;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

