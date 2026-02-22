using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace UAV_Assistive_Operation.Converters
{
    public class IndexToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            int selected = (int)value;
            var param = parameter.ToString().Split('|');
            int index = int.Parse(param[0]);

            bool invert = param.Length > 1 && param[1].Equals("Invert", StringComparison.OrdinalIgnoreCase);
            bool indexMatch = selected == index;

            if (invert)
                indexMatch = !indexMatch;

            return indexMatch ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility && parameter != null)
            {
                var param = parameter.ToString().Split('|');
                int index = int.Parse(param[0]);

                bool invert = param.Length > 1 && param[1].Equals("Invert", StringComparison.OrdinalIgnoreCase);
                bool isVisible = visibility == Visibility.Visible;

                if (invert)
                    isVisible = !isVisible;

                if (isVisible)
                    return index;
            }
            return DependencyProperty.UnsetValue;
        }
    }
}
