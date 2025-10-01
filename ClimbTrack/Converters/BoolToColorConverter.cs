using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace ClimbTrack.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isTrue && parameter is string options)
            {
                string[] parts = options.Split('|');
                if (parts.Length >= 2)
                {
                    string colorString = isTrue ? parts[0] : parts[1];
                    if (Color.TryParse(colorString, out Color color))
                    {
                        return color;
                    }
                }
            }
            return Colors.Black;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}