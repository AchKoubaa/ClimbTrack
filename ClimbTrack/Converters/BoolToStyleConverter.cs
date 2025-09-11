using System.Globalization;

namespace ClimbTrack.Converters
{
    public class BoolToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isTrue && parameter is Style trueStyle)
            {
                return isTrue ? trueStyle : Application.Current.Resources["RouteItemStyle"];
            }
            return Application.Current.Resources["RouteItemStyle"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}