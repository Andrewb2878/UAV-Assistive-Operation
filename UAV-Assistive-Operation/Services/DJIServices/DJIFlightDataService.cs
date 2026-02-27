using DJI.WindowsSDK;
using DJI.WindowsSDK.Components;
using System;
using UAV_Assistive_Operation.Models;

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
        public bool IsSimulatorStarted { get; private set; }
        public bool IsNearHeightLimit { get; private set; }

        public LocationFlightDataModel Location { get; } = new LocationFlightDataModel();


        //MapService relevant events for services to subscribe to
        public event Action<double, double> UavLocationUpdated;
        public event Action<double> UAVHeadingUpdated;

        //EvaluationServices relevant events for services to subscribe to
        public event Action<bool> FlyingChanged;
        public event Action<bool> LandingConfirmationChanged;
        public event Action<bool> HeightLimitReachedChanged;
        public event Action<bool> SeriousBatteryChanged;
        public event Action<bool> LowBatteryChanged;
        public event Action<FCMotorStartFailureError> MotorStartFailureChanged;
        public event Action<bool> NotEnoughForceChanged;
        public event Action<FCWindWarning> WindWarningChanged;
        public event Action<bool> SimulatorStartedChanged;

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

                _flightControllerHandler.AircraftLocationChanged += AircraftLocationChanged;
                _flightControllerHandler.AttitudeChanged += AircraftAttitudeChanged;

                _flightControllerHandler.IsFlyingChanged += AircraftFlyingChanged;
                _flightControllerHandler.IsLandingConfirmationNeededChanged += LandingConfirmationNeededChanged;
                _flightControllerHandler.IsNearHeightLimitChanged += NearHeightLimitChanged;
                _flightControllerHandler.IsSeriousLowBatteryWarningChanged += SeriousLowBattery;
                _flightControllerHandler.IsLowBatteryWarningChanged += LowBattery;
                _flightControllerHandler.MotorStartFailureErrorChanged += MotorStartFailure;
                _flightControllerHandler.HasNoEnoughForceChanged += NotEnoughForce;
                _flightControllerHandler.WindWarningChanged += WindWarning;

                _flightControllerHandler.IsSimulatorStartedChanged += SimulatorStarted;
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
                _flightControllerHandler.IsLandingConfirmationNeededChanged -= LandingConfirmationNeededChanged;
                _flightControllerHandler.IsSeriousLowBatteryWarningChanged -= SeriousLowBattery;
                _flightControllerHandler.IsLowBatteryWarningChanged -= LowBattery;
                _flightControllerHandler.MotorStartFailureErrorChanged -= MotorStartFailure;
                _flightControllerHandler.HasNoEnoughForceChanged -= NotEnoughForce;
                _flightControllerHandler.WindWarningChanged -= WindWarning;

                _flightControllerHandler.IsSimulatorStartedChanged -= SimulatorStarted;
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


        //Getting updates from subscriptions
        private async void AircraftLocationChanged(object sender, LocationCoordinate2D? value)
        {
            if (!IsAircraftConnected || value == null)
                return;

            var lat = value.Value.latitude; 
            var lon = value.Value.longitude;

            UavLocationUpdated?.Invoke(lat, lon);

            await App.RunOnUIThread(() =>
            {
                Location.Latitude = lat;
                Location.Longitude = lon;
            });
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

        private void LandingConfirmationNeededChanged(object sender, BoolMsg? value)
        {
            if (!IsAircraftConnected || value == null)
                return;

            var confirmation = value.Value.value;
            LandingConfirmationChanged?.Invoke(confirmation);
        }

        private void NearHeightLimitChanged(object sender, BoolMsg? value)
        {
            if (IsAircraftConnected || value == null)
                return;

            IsNearHeightLimit = value.Value.value;
            
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


        private void SimulatorStarted(object sender, BoolMsg? value) 
        {
            if (!IsAircraftConnected || value == null)
                return;

            IsSimulatorStarted = value.Value.value;
            SimulatorStartedChanged?.Invoke(IsSimulatorStarted);
        }

        //Method to check if aircraft can be configured
        public bool CanConfigureAircraft()
        {
            return IsAircraftConnected && !IsFlying;
        }
    }
}
