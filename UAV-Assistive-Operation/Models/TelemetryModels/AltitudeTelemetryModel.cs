using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UAV_Assistive_Operation.Models
{
    public class AltitudeTelemetryModel : INotifyPropertyChanged
    {
        private double? _altitude;


        public double? Altitude
        {
            get => _altitude;
            set
            {
                if (_altitude != value)
                {
                    _altitude = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayText));
                }
            }
        }

        public string DisplayText => Altitude.HasValue ? $"Altitude: {Altitude.Value:F1} m" : "Altitude: -- m";

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
