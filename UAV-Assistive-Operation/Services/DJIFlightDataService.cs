using DJI.WindowsSDK;
using DJI.WindowsSDK.Components;
using System;

namespace UAV_Assistive_Operation.Services
{
    public class DJIFlightDataService
    {
        private FlightControllerHandler _flightControllerHandler;
        private bool IsAircraftConnected => App.DJIConnectionService.IsAircraftConnected;


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
            SubscribeToFlightController();
        }

        public void AircraftDisconnected()
        {
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


        //Getting updates from subscriptions
        private void AircraftLocationChanged(object sender, LocationCoordinate2D? value)
        {
            if (!IsAircraftConnected || value == null)
                return;

            var lat = value.Value.latitude; 
            var lon = value.Value.longitude;
           UavLocationUpdated?.Invoke(lat, lon);
        }

        private void AircraftAttitudeChanged(object sender, Attitude? attitude)
        {
            if (!IsAircraftConnected || attitude == null)
                return;

            var yaw = attitude.Value.yaw;
            UAVHeadingUpdated?.Invoke(yaw);
        }

        private void AircraftFlyingChanged(object sender, BoolMsg? value)
        {
            if (!IsAircraftConnected || value == null)
                return;

            var flying = value.Value.value;
            FlyingChanged?.Invoke(flying);
        }

        private void SeriousLowBattery(object sender, BoolMsg? value)
        {
            if (!IsAircraftConnected || value == null)
                return;

            var seriousBattery = value.Value.value;
            SeriousBatteryChanged?.Invoke(seriousBattery);
        }

        private void LowBattery(object sender, BoolMsg? value)
        {
            if (!IsAircraftConnected || value == null) 
                return;

            var lowBattery = value.Value.value;
            LowBatteryChanged?.Invoke(lowBattery);
        }

        private void MotorStartFailure(object sender, FCMotorStartFailureErrorMsg? value)
        {
            if (!IsAircraftConnected || value == null)
                return;

            var motorStartFailure = value.Value.value;
            MotorStartFailureChanged?.Invoke(motorStartFailure);
        }

        private void WindWarning(object sender, FCWindWarningMsg? value)
        {
            if (!IsAircraftConnected || value == null)
                return;

            var level = value.Value.value;
            WindWarningChanged.Invoke(level);
        }
    }
}
