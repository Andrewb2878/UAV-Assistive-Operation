using DJI.WindowsSDK;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UAV_Assistive_Operation.Models
{
    public class GPSStrengthTelemetryModel : INotifyPropertyChanged
    {
        private FCGPSSignalLevel? _signalLevel;


        public FCGPSSignalLevel? SignalLevel
        {
            get => _signalLevel;
            set
            {
                if (_signalLevel != value)
                {
                    _signalLevel = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayText));
                    OnPropertyChanged(nameof(SufficientForFlight));
                    OnPropertyChanged(nameof(GPSBannerText));
                }
            }
        }

        public string DisplayText
        {
            get
            {
                if (!SignalLevel.HasValue)
                    return "---";

                switch (SignalLevel.Value)
                {
                    case FCGPSSignalLevel.LEVEL_5:
                        return "Excellent";
                    case FCGPSSignalLevel.LEVEL_4:
                        return "Good";
                    case FCGPSSignalLevel.LEVEL_3:
                        return "Moderate";
                    case FCGPSSignalLevel.LEVEL_2:
                        return "Weak";
                    case FCGPSSignalLevel.LEVEL_1:
                        return "Very Weak";
                    case FCGPSSignalLevel.LEVEL_0:
                        return "Critical";
                    case FCGPSSignalLevel.LEVEL_None:
                        return "No Signal";
                    case FCGPSSignalLevel.UNKNOWN:
                        return "Unknown";
                    default:
                        return "---";
                }
            }
        }

        public bool SufficientForFlight
        {
            get
            {
                if (!SignalLevel.HasValue)
                    return false;

                return SignalLevel.Value >= FCGPSSignalLevel.LEVEL_1;
            }
        }

        public string GPSBannerText
        {
            get
            {
                if (!SignalLevel.HasValue)
                    return "---";

                return SufficientForFlight ? "Ready to fly" : "Insufficient GPS signal";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
