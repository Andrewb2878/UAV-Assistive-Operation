using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UAV_Assistive_Operation.Models
{
    public class SpeedTelemetryModel : INotifyPropertyChanged
    {
        private double? _horizontal;
        private double? _vertical;


        public double? Horizontal
        {
            get => _horizontal;
            set
            {
                if (_horizontal != value)
                {
                    _horizontal = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayHorizontal));
                }
            }
        }

        public double? Vertical
        {
            get => _vertical;
            set
            {
                if (_vertical != value)
                {
                    _vertical = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayVertical));
                }
            }
        }

        public string DisplayHorizontal => Horizontal.HasValue ? $"H.S: {Horizontal.Value:F1} mph" : "H.S: -- mph";
        public string DisplayVertical => Vertical.HasValue ? $"V.S: {Vertical.Value:+0.0;-0.0} mph" : "V.S: -- mph";

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
