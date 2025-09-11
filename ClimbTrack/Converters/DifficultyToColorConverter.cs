using System.Globalization;

namespace ClimbTrack.Converters
{
    public class DifficultyToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int difficulty)
            {
                return difficulty switch
                {
                    
                    1 => Color.FromArgb("#FF80AB"),  // Rosa 
                    2 => Color.FromArgb("#F5F5F5"),  // Bianco 
                    4 => Color.FromArgb("#43A047"),  // Verde
                    3 => Color.FromArgb("#29B6F6"),  // Azzurro 
                    5 => Color.FromArgb("#1565C0"),  // Blu 
                    6 => Color.FromArgb("#757575"),  // Grigio 
                    7 => Color.FromArgb("#795548"),  // Marrone 
                    8 => Color.FromArgb("#FDD835"),  // Giallo  
                    9 => Color.FromArgb("#F57C00"),  // Arancione 
                    _ => Color.FromArgb("#FF0000"),  // Red 
                };
            }
            return Color.FromArgb("#9E9E9E");  // Grigio - Default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}