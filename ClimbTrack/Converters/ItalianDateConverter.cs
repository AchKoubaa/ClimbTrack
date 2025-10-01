using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Converters
{
    public class ItalianDateConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                // Format the date in Italian style (e.g., "01/01/2023")
                return dateTime.ToString("dd/MM/yyyy", new CultureInfo("it-IT"));
            }
            return value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string dateString)
            {
                // Try to parse the Italian formatted date
                if (DateTime.TryParse(dateString, new CultureInfo("it-IT"),
                    DateTimeStyles.None, out DateTime result))
                {
                    return result;
                }
            }
            return value;
        }
    }
}
