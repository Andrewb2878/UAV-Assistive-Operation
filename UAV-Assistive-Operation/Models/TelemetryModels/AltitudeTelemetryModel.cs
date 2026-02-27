using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UAV_Assistive_Operation.Models
{
    public class AltitudeTelemetryModel : INotifyPropertyChanged
    {
        private double? _altitude;
        private const double _mFt = 3.28084;
        private bool _useMetric = true;


        public bool UseMetric
        {
            get => _useMetric;
            set 
            {
                _useMetric = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText)); 
            }
        }

        public double? Altitude 
        {
            get => _altitude;
            set 
            {
                if (Set(ref _altitude, value))
                    OnPropertyChanged(nameof(DisplayText)); 
            } 
        }


        public string DisplayText => !Altitude.HasValue ? "Altitude: --" : 
        UseMetric ? $"Altitude: {Altitude.Value:F1} m" : $"Altitude: {Altitude.Value * _mFt:F1} ft";

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
