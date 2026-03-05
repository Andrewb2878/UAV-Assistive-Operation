using DJI.WindowsSDK;
using DJI.WindowsSDK.Components;
using UAV_Assistive_Operation.Models;
using System;
using UAV_Assistive_Operation.Enums;

namespace UAV_Assistive_Operation.Services
{
    public class DJITelemetryService
    {
        private BatteryHandler _batteryHandler;
        private FlightControllerHandler _flightControllerHandler;

        private bool IsAircraftConnected => App.DJIConnectionService.IsAircraftConnected;
        
        private const double VisionAssistAltitudeThreshold = 4.0;
        private bool _isAboveVisionThreshold;


        public BatteryTelemetryModel Battery { get; } = new BatteryTelemetryModel();
        public FlightModeTelemetryModel FlightMode { get; } = new FlightModeTelemetryModel();
        public GPSStrengthTelemetryModel GPS { get; } = new GPSStrengthTelemetryModel();
        public AltitudeTelemetryModel Altitude { get; } = new AltitudeTelemetryModel();
        public SpeedTelemetryModel Speed { get; } = new SpeedTelemetryModel();


        //Events for Services to subscribe to
        public event Action<bool> VisionAltitudeThresholdChanged;


        public void AircraftConnected()
        {
            SubscribeToBattery();
            SubscribeToFlightController();
        }

        public void AircraftDisconnected()
        {
            Battery.Percentage = null;
            FlightMode.FlightMode = null;
            GPS.SignalLevel = null;
            Altitude.Altitude = null;
            Speed.VelocityX = null;
            Speed.VelocityY = null;
            Speed.VelocityZ = null;

            UnsubscribeFromBattery();
            UnsubscribeFromFlightController();
        }


        //Subscribing to events
        private void SubscribeToBattery()
        {
            _batteryHandler = DJISDKManager.Instance.ComponentManager.GetBatteryHandler(0, 0);
            if (_batteryHandler != null)
            {
                InitBatteryPercent();

                _batteryHandler.ChargeRemainingInPercentChanged += BatteryPercentChanged;
            }
        }

        private void SubscribeToFlightController()
        {
            _flightControllerHandler = DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0);
            if (_flightControllerHandler != null)
            {
                InitFlightMode();
                InitGPSSignalLevel();
                InitAltitude();
                InitVelocity();

                _flightControllerHandler.FlightModeChanged += FlightModeChanged;
                _flightControllerHandler.GPSSignalLevelChanged += GPSSignalLevelChanged;
                _flightControllerHandler.AltitudeChanged += AltitudeChanged;
                _flightControllerHandler.VelocityChanged += VelocityChanged;
            }
        }


        //Unsubscribing from events
        private void UnsubscribeFromBattery()
        {
            if (_batteryHandler != null)
            {
                _batteryHandler.ChargeRemainingInPercentChanged -= BatteryPercentChanged;
                _batteryHandler = null;
            }
        }

        private void UnsubscribeFromFlightController()
        {
            if (_flightControllerHandler != null)
            {
                _flightControllerHandler.FlightModeChanged -= FlightModeChanged;
                _flightControllerHandler.GPSSignalLevelChanged -= GPSSignalLevelChanged;
                _flightControllerHandler.AltitudeChanged -= AltitudeChanged;
                _flightControllerHandler.VelocityChanged -= VelocityChanged;
                _flightControllerHandler = null;
            }
        }


        //Initialising telemetry data
        private async void InitBatteryPercent()
        {
            var battery = await _batteryHandler.GetChargeRemainingInPercentAsync();
            if (battery.value != null)
            {
                await App.RunOnUIThread(() =>
                {
                    Battery.Percentage = battery.value.Value.value;
                });
                EventLogService.Instance.Log(LogEventType.Info, $"Battery charge at: {battery.value.Value.value}%");
            }
        }
        
        private async void InitFlightMode()
        {
            var flightMode = await _flightControllerHandler.GetFlightModeAsync();
            if (flightMode.value != null)
            {
                await App.RunOnUIThread(() =>
                {
                    FlightMode.FlightMode = flightMode.value.Value.value;
                });
            }
        }

        private async void InitGPSSignalLevel()
        {
            var gpsSignalLevel = await _flightControllerHandler.GetGPSSignalLevelAsync();
            if (gpsSignalLevel.value != null)
            {
                await App.RunOnUIThread(() =>
                {
                    GPS.SignalLevel = gpsSignalLevel.value.Value.value;
                });
            }
        }

        private async void InitAltitude()
        {
            var altitude = await _flightControllerHandler.GetAltitudeAsync();
            if (altitude.value != null)
            {
                await App.RunOnUIThread(() =>
                {
                    Altitude.Altitude = altitude.value.Value.value;
                });
            }
        }

        private async void InitVelocity()
        {
            var velocity = await _flightControllerHandler.GetVelocityAsync();
            if (velocity.value != null)
            {
                var velocityX = velocity.value.Value.x;
                var velocityY = velocity.value.Value.y;
                var velocityZ = velocity.value.Value.z;

                //double horizontalMs = Math.Sqrt(velocityNorth * velocityNorth + velocityEast * velocityEast);
                //double verticalMs = -velocityDown;

                await App.RunOnUIThread(() =>
                {
                    Speed.VelocityX = velocityX;
                    Speed.VelocityY = velocityY;
                    Speed.VelocityZ = velocityZ;
                });
            }
        }


        //Getting updates from subscriptions
        private async void BatteryPercentChanged(object sender, IntMsg? value)
        {
            if (!IsAircraftConnected || value == null)
                return;

            await App.RunOnUIThread(() =>
            {
                Battery.Percentage = value.Value.value;
            });
        }

        private async void FlightModeChanged(object sender, FCFlightModeMsg? value)
        {
            if (!IsAircraftConnected || value == null)
                return;

            await App.RunOnUIThread(() =>
            {
                FlightMode.FlightMode = value.Value.value;
            });
        }

        private async void GPSSignalLevelChanged(object sender, FCGPSSignalLevelMsg? value)
        {
            if (!IsAircraftConnected || value == null)
                return;

            await App.RunOnUIThread(() =>
            {
                GPS.SignalLevel = value.Value.value;
            });
        }

        private async void AltitudeChanged(object sender, DoubleMsg? value)
        {
            if (!IsAircraftConnected || value == null)
                return;

            double altitude = value.Value.value;

            await App.RunOnUIThread(() =>
            {
                Altitude.Altitude = altitude;
            });

            CheckVisionAssistThreshold(altitude);
        }

        private async void VelocityChanged(object sender, Velocity3D? value)
        {
            if (!IsAircraftConnected || value == null)
                return;

            var velocityX = value.Value.x;
            var velocityY = value.Value.y;
            var velocityZ = value.Value.z;

            await App.RunOnUIThread(() =>
            {
                Speed.VelocityX = velocityX;
                Speed.VelocityY = velocityY;
                Speed.VelocityZ = velocityZ;
            });
        }


        //Triggering events based on state changes
        private void CheckVisionAssistThreshold(double altitude)
        {
            bool aboveThreshold = altitude > VisionAssistAltitudeThreshold;

            if (aboveThreshold != _isAboveVisionThreshold)
            {
                _isAboveVisionThreshold = aboveThreshold;
                VisionAltitudeThresholdChanged?.Invoke(aboveThreshold);
            }
        }
    }
}
