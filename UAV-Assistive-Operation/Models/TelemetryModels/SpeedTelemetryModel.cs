using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UAV_Assistive_Operation.Services;

namespace UAV_Assistive_Operation.Models
{
    public class SpeedTelemetryModel : INotifyPropertyChanged
    {
        private double? _velocityX;
        private double? _velocityY;
        private double? _velocityZ;

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


        public double? VelocityX
        {
            get => _velocityX;
            set
            {
                if (Set(ref _velocityX, value))
                {
                    OnPropertyChanged(nameof(Horizontal));
                    OnPropertyChanged(nameof(DisplayHorizontal));
                }
            }
        }

        public double? VelocityY
        {
            get => _velocityY;
            set
            {
                if (Set(ref _velocityY, value))
                {
                    OnPropertyChanged(nameof(Horizontal));
                    OnPropertyChanged(nameof(DisplayHorizontal));
                }
            }
        }

        public double? VelocityZ
        {
            get => _velocityZ;
            set 
            {
                if (Set(ref _velocityZ, value))
                {
                    OnPropertyChanged(nameof(Vertical));
                    OnPropertyChanged(nameof(DisplayVertical));
                }
            }
        }


        public double? Horizontal => VelocityX.HasValue && VelocityY.HasValue ?
            (double?)Math.Sqrt((VelocityX.Value * VelocityX.Value) + (VelocityY.Value * VelocityY.Value)) : null;

        public double? Vertical => VelocityZ.HasValue ?
            (double?)-VelocityZ.Value : null;



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
