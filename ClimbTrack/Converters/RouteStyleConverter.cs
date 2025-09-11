using System;
using System.Globalization;
using ClimbTrack.ViewModels;
using Microsoft.Maui.Controls;
using static ClimbTrack.ViewModels.TrainingViewModel;

namespace ClimbTrack.Converters
{
    public class RouteStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TrainingRouteItem item)
            {
                if (item.IsCompleted && item.IsSelected)
                    return Application.Current.Resources["CompletedSelectedRouteItemStyle"];
                else if (item.IsCompleted)
                    return Application.Current.Resources["CompletedRouteItemStyle"];
                else if (item.IsSelected)
                    return Application.Current.Resources["SelectedRouteItemStyle"];
                else
                    return Application.Current.Resources["RouteItemStyle"];
            }

            return Application.Current.Resources["RouteItemStyle"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}