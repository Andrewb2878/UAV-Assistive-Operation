using DJI.WindowsSDK;
using DJI.WindowsSDK.Components;
using System;

namespace UAV_Assistive_Operation.Services
{
    public class DJIFlightDataService
    {
        private FlightControllerHandler _flightControllerHandler;
        private bool IsAircraftConnected => App.DJIConnectionService.IsAircraftConnected;


        //Public variables for services to use
        public bool IsFlying {  get; private set; }
        public bool IsSeriousLowBattery { get; private set; }
        public bool IsLowBattery { get; private set; }


        //MapService relevant events for services to subscribe to
        public event Action<double, double> UavLocationUpdated;
        public event Action<double> UAVHeadingUpdated;

        //EvaluationServices relevant events for services to subscribe to
        public event Action<bool> FlyingChanged;
        public event Action<bool> SeriousBatteryChanged;
        public event Action<bool> LowBatteryChanged;
        public event Action<FCMotorStartFailureError> MotorStartFailureChanged;
        public event Action<bool> NotEnoughForceChanged;
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
                InitSeriousLowBatteryChanged();
                InitLowBatteryChanged();

                _flightControllerHandler.AircraftLocationChanged += AircraftLocationChanged;
                _flightControllerHandler.AttitudeChanged += AircraftAttitudeChanged;

                _flightControllerHandler.IsFlyingChanged += AircraftFlyingChanged;
                _flightControllerHandler.IsSeriousLowBatteryWarningChanged += SeriousLowBattery;
                _flightControllerHandler.IsLowBatteryWarningChanged += LowBattery;
                _flightControllerHandler.MotorStartFailureErrorChanged += MotorStartFailure;
                _flightControllerHandler.HasNoEnoughForceChanged += NotEnoughForce;
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
                _flightControllerHandler.HasNoEnoughForceChanged -= NotEnoughForce;
                _flightControllerHandler.WindWarningChanged -= WindWarning;
                _flightControllerHandler = null;
            }
        }

        //Initialising critical data
        private async void InitAircraftFlyingChanged()
        {
            var flying = await _flightControllerHandler.GetIsFlyingAsync();
            if (flying.value != null)
            {
                IsFlying = flying.value.Value.value;
                FlyingChanged?.Invoke(IsFlying);
            }    
        }

        private async void InitSeriousLowBatteryChanged()
        {
            var seriousBattery = await _flightControllerHandler.GetIsSeriousLowBatteryWarningAsync();
            if (seriousBattery.value != null)
            {
                IsSeriousLowBattery = seriousBattery.value.Value.value;
                SeriousBatteryChanged?.Invoke(IsSeriousLowBattery);
            }
        }

        private async void InitLowBatteryChanged()
        {
            var lowBattery = await _flightControllerHandler.GetIsLowBatteryWarningAsync();
            if (lowBattery.value != null)
            {
                IsLowBattery = lowBattery.value.Value.value;
                LowBatteryChanged?.Invoke(IsLowBattery);
            }
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

            IsFlying = value.Value.value;
            FlyingChanged?.Invoke(IsFlying);
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

        private void NotEnoughForce(object sender, BoolMsg? value)
        {
            if (!IsAircraftConnected || value == null)
                return;

            var enoughForce = value.Value.value;
            NotEnoughForceChanged?.Invoke(enoughForce);
        }

        private void WindWarning(object sender, FCWindWarningMsg? value)
        {
            if (!IsAircraftConnected || value == null)
                return;

            var level = value.Value.value;
            WindWarningChanged.Invoke(level);
        }

        //Method to check if aircraft can be configured
        public bool CanConfigureAircraft()
        {
            return IsAircraftConnected && !IsFlying;
        }
    }
}
