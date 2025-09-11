using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace ClimbTrack.Converters
{
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isTrue && parameter is string options)
            {
                string[] parts = options.Split('|');
                if (parts.Length >= 2)
                {
                    return isTrue ? parts[0] : parts[1];
                }
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}