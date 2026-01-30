using DJI.WindowsSDK;
using DJI.WindowsSDK.Components;
using System;

namespace UAV_Assistive_Operation.Services
{
    public class DJIFlightDataService
    {
        private FlightControllerHandler _flightControllerHandler;        
        private bool _running;


        //MapService relevant events for services to subscribe to
        public event Action<double, double> UavLocationUpdated;
        public event Action<double> UAVHeadingUpdated;

        //EvaluationServices relevant events for servies to subscribe to
        public event Action<bool> FlyingChanged;
        public event Action<bool> SeriousBatteryChanged;
        public event Action<bool> LowBatteryChanged;
        public event Action<FCMotorStartFailureError> MotorStartFailureChanged;
        public event Action<bool> MotorStuckChanged;
        public event Action<FCWindWarning> WindWarningChanged;

        public void AircraftConnected()
        {
            if (_running)
                return;

            _running = true;
            SubscribeToFlightController();
        }

        public void AircraftDisconnected()
        {
            if (!_running)
                return;

            _running = false;

            UnsubscribeToFlightController();
        }


        //Subscribing to events
        private void SubscribeToFlightController()
        {
            _flightControllerHandler = DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0);
            if (_flightControllerHandler != null)
            {
                InitAircraftFlyingChanged();
                InitMotorStuck();
                InitSeriousLowBattery();
                InitLowBattery();

                _flightControllerHandler.AircraftLocationChanged += AircraftLocationChanged;
                _flightControllerHandler.AttitudeChanged += AircraftAttitudeChanged;

                _flightControllerHandler.IsFlyingChanged += AircraftFlyingChanged;
                _flightControllerHandler.IsSeriousLowBatteryWarningChanged += SeriousLowBattery;
                _flightControllerHandler.IsLowBatteryWarningChanged += LowBattery;
                _flightControllerHandler.MotorStartFailureErrorChanged += MotorStartFailure;
                _flightControllerHandler.WindWarningChanged += WindWarning;
            }
        }


        //Unsubscribing from events
        private void UnsubscribeToFlightController()
        {
            if (_flightControllerHandler != null)
            {
                _flightControllerHandler.AircraftLocationChanged -= AircraftLocationChanged;
                _flightControllerHandler.AttitudeChanged -= AircraftAttitudeChanged;

                _flightControllerHandler.IsFlyingChanged -= AircraftFlyingChanged;
                _flightControllerHandler.IsSeriousLowBatteryWarningChanged -= SeriousLowBattery;
                _flightControllerHandler.IsLowBatteryWarningChanged -= LowBattery;
                _flightControllerHandler.MotorStartFailureErrorChanged -= MotorStartFailure;
                _flightControllerHandler.WindWarningChanged -= WindWarning;
                _flightControllerHandler = null;
            }
        }

        //Initialising critical data
        private async void InitAircraftFlyingChanged()
        {
            var flying = await _flightControllerHandler.GetIsFlyingAsync();
            if (flying.value != null)
                FlyingChanged?.Invoke(flying.value.Value.value);
        }

        private async void InitMotorStuck()
        {
            var stuck = await _flightControllerHandler.GetIsMotorStuckAsync();
            if (stuck.value != null)
                MotorStuckChanged?.Invoke(stuck.value.Value.value);      
        }
        
        private async void InitSeriousLowBattery()
        {
            var seriousBattery = await _flightControllerHandler.GetIsSeriousLowBatteryWarningAsync();
            if (seriousBattery.value != null)
                SeriousBatteryChanged?.Invoke(seriousBattery.value.Value.value);
        }

        private async void InitLowBattery()
        {
            var lowBattery = await _flightControllerHandler.GetIsLowBatteryWarningAsync();
            if (lowBattery.value != null)
                LowBatteryChanged?.Invoke(lowBattery.value.Value.value);
        }


        //Getting updates from subscriptions
        private void AircraftLocationChanged(object sender, LocationCoordinate2D? value)
        {
            if (!_running || value == null)
                return;

            var lat = value.Value.latitude; 
            var lon = value.Value.longitude;
           UavLocationUpdated?.Invoke(lat, lon);
        }

        private void AircraftAttitudeChanged(object sender, Attitude? attitude)
        {
            if (!_running || attitude == null)
                return;

            var yaw = attitude.Value.yaw;
            UAVHeadingUpdated?.Invoke(yaw);
        }

        private void AircraftFlyingChanged(object sender, BoolMsg? value)
        {
            if (!_running || value == null)
                return;

            var flying = value.Value.value;
            FlyingChanged?.Invoke(flying);
        }

        private void SeriousLowBattery(object sender, BoolMsg? value)
        {
            if (!_running || value == null)
                return;

            var seriousBattery = value.Value.value;
            SeriousBatteryChanged?.Invoke(seriousBattery);
        }

        private void LowBattery(object sender, BoolMsg? value)
        {
            if (!_running || value == null) 
                return;

            var lowBattery = value.Value.value;
            LowBatteryChanged?.Invoke(lowBattery);
        }

        private void MotorStartFailure(object sender, FCMotorStartFailureErrorMsg? value)
        {
            if (_running || value == null)
                return;

            var motorStartFailure = value.Value.value;
            MotorStartFailureChanged?.Invoke(motorStartFailure);
        }

        private void WindWarning(object sender, FCWindWarningMsg? value)
        {
            if (!_running || value == null)
                return;

            var level = value.Value.value;
            WindWarningChanged.Invoke(level);
        }
    }
}
