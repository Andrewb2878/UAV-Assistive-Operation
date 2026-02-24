using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace UAV_Assistive_Operation.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public Color TrueBrush { get; set; } = Colors.DarkSeaGreen;
        public SolidColorBrush TrueButtonBrush {  get; set; } = new SolidColorBrush(Colors.LightSlateGray);
        
        public Color FalseBrush { get; set; } = Colors.SlateGray;
        public SolidColorBrush FalseButtonBrush { get; set; } = new SolidColorBrush(Colors.White);


        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool buttonConvert = parameter?.ToString() == "Button";

            if (value is bool isEnabled)
            {
                if (buttonConvert)
                    return isEnabled ? TrueButtonBrush : FalseButtonBrush;

                return isEnabled ? TrueBrush : FalseBrush;
            }
            return FalseBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
