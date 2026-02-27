using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UAV_Assistive_Operation.Models
{
    public class SpeedTelemetryModel : INotifyPropertyChanged
    {
        private double? _horizontal;
        private double? _vertical;
        private const double _MsMph = 2.23694;
        private bool _useMetric = true;


        public bool UseMetric
        {
            get => _useMetric;
            set 
            {
                _useMetric = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayHorizontal));
                OnPropertyChanged(nameof(DisplayVertical)); 
            }
        }


        public double? Horizontal 
        {
            get => _horizontal;
            set 
            { 
                if (Set(ref _horizontal, value)) 
                    OnPropertyChanged(nameof(DisplayHorizontal)); 
            } 
        }

        public double? Vertical
        {
            get => _vertical;
            set 
            { 
                if (Set(ref _vertical, value))
                    OnPropertyChanged(nameof(DisplayVertical)); 
            } 
        }


        public string DisplayHorizontal => !Horizontal.HasValue ? "H.S: --" :
        UseMetric ? $"H.S: {Horizontal.Value:F1} m/s" : $"H.S: {Horizontal.Value * _MsMph:F1} mph";
        public string DisplayVertical => !Vertical.HasValue ? "V.S: --" :
        UseMetric ? $"V.S: {Vertical.Value:+0.0;-0.0} m/s" : $"V.S: {Vertical.Value * _MsMph:+0.0;-0.0} mph";

        public event PropertyChangedEventHandler PropertyChanged;


        private bool Set<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
