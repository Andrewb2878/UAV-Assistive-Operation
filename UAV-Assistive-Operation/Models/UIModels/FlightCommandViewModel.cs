using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UAV_Assistive_Operation.Models
{
    public class FlightCommandViewModel : INotifyPropertyChanged
    {
        private bool _takeoffActive;
        private bool _landActive;
        private bool _stopActive;

        public bool TakeoffActive
        {
            get => _takeoffActive;
            set => Set(ref _takeoffActive, value);
        }

        public bool LandActive
        {
            get => _landActive;
            set => Set(ref _landActive, value);
        }

        public bool StopActive
        {
            get => _stopActive;
            set => Set(ref _stopActive, value);
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T field, T value, [CallerMemberName] string  propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
