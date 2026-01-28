using System;
using System.Diagnostics;
using UAV_Assistive_Operation.Enums;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace UAV_Assistive_Operation.Converters
{
    public class EventTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is LogEventType eventType)
                switch (eventType)
                {
                    case LogEventType.Connection:
                        return new SolidColorBrush(Colors.MediumSeaGreen);
                    case LogEventType.Warning:
                        return new SolidColorBrush(Colors.Orange);
                    case LogEventType.Error:
                        return new SolidColorBrush(Colors.Firebrick);
                    case LogEventType.Info:
                        return new SolidColorBrush(Colors.DarkSlateBlue);
                    case LogEventType.System:
                        return new SolidColorBrush(Colors.Black);
                    case LogEventType.Debug:
                        return new SolidColorBrush(Colors.Purple);
                    default:
                        return new SolidColorBrush(Colors.Black);
                }

            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) 
        {
            throw new NotImplementedException();
        }
    }
}
