using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace UAV_Assistive_Operation.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool invert = parameter?.ToString() == "Invert";

            if (value is bool b && b)
            {
                if (invert)
                    b = !b;
                
                return b ? Visibility.Visible : Visibility.Collapsed;
            } 
            
            if (invert)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string langauge)
        {
            if (value is Visibility v)
                return v == Visibility.Visible;
            return false;
        }
    }
}
