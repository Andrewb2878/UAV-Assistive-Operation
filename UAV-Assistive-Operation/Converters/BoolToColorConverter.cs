using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace UAV_Assistive_Operation.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public SolidColorBrush TrueBrush {  get; set; } = new SolidColorBrush(Colors.LightSlateGray);
        public SolidColorBrush FalseBrush { get; set; } = new SolidColorBrush(Colors.White);


        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isEnabled)
            {
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
