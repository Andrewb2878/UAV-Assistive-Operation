using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UAV_Assistive_Operation.Models
{
    public class BatteryTelemetryModel : INotifyPropertyChanged
    {
        private int? _percentage;


        public int? Percentage
        {
            get => _percentage; 
            set
            {
                if (_percentage != value)
                {
                    _percentage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayText));
                }
            }
        }

        public string DisplayText => Percentage.HasValue ? $"Battery: {Percentage.Value}%" : "Battery: --%";

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
