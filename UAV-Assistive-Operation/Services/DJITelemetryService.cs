using DJI.WindowsSDK;
using DJI.WindowsSDK.Components;
using UAV_Assistive_Operation.Models;
using System;
using System.Diagnostics;
using Windows.UI.Core;

namespace UAV_Assistive_Operation.Services
{
    public class DJITelemetryService
    {
        private readonly CoreDispatcher _dispatcher;
        private BatteryHandler _batteryHandler;
        private FlightControllerHandler _flightControllerHandler;
        private bool _running;
        private const double _MsMph = 2.23694;


        public BatteryTelemetryModel Battery { get; } = new BatteryTelemetryModel();
        public FlightModeTelemetryModel FlightMode { get; } = new FlightModeTelemetryModel();
        public GPSStrengthTelemetryModel GPS { get; } = new GPSStrengthTelemetryModel();
        public AltitudeTelemetryModel Altitude { get; } = new AltitudeTelemetryModel();
        public SpeedTelemetryModel Speed { get; } = new SpeedTelemetryModel();


        public DJITelemetryService(CoreDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void AircraftConnected()
        {
            if (_running)
                return;

            _running = true;

            SubscribeToBattery();
            SubscribeToFlightController();
        }

        public void AircraftDisconnected()
        {
            if (!_running)
                return;

            _running = false;
            Battery.Percentage = null;
            FlightMode.FlightMode = null;
            GPS.SignalLevel = null;
            Altitude.Altitude = null;
            Speed.Horizontal = null;
            Speed.Vertical = null;

            UnsubscribeFromBattery();
            UnsubscribeFromFlightController();
        }


        //Subscribing to events
        private void SubscribeToBattery()
        {
            _batteryHandler = DJISDKManager.Instance.ComponentManager.GetBatteryHandler(0, 0);
            if (_batteryHandler != null)
            {
                _batteryHandler.ChargeRemainingInPercentChanged += BatteryPercentChanged;
                Debug.WriteLine("Subscribed to battery percentage updates");
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
            if ( _flightControllerHandler != null)
            {
                _flightControllerHandler.FlightModeChanged -= FlightModeChanged;
                _flightControllerHandler.GPSSignalLevelChanged -= GPSSignalLevelChanged;
                _flightControllerHandler.AltitudeChanged -= AltitudeChanged;
                _flightControllerHandler.VelocityChanged -= VelocityChanged;
                _flightControllerHandler = null;
            }
        }


        //Initialising telemetry data
        private async void InitFlightMode()
        {
            var flightMode = await _flightControllerHandler.GetFlightModeAsync();
            if (flightMode.value != null)
            {
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
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
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
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
                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
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
                var velocityNorth = velocity.value.Value.x;
                var velocityEast = velocity.value.Value.y;
                var velocityDown = velocity.value.Value.z;

                double horizontalMs = Math.Sqrt(velocityNorth * velocityNorth + velocityEast * velocityEast);
                double horizontalMph = horizontalMs * _MsMph;
                double verticalMph = (-velocityDown) * _MsMph;

                await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Speed.Horizontal = horizontalMph;
                    Speed.Vertical = verticalMph;
                });
            }
        }


        //Getting updates from subscriptions
        private async void BatteryPercentChanged(object sender, IntMsg? value)
        {
            if (!_running || value == null)
                return;

            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Battery.Percentage = value.Value.value;
            });
        }

        private async void FlightModeChanged(object sender, FCFlightModeMsg? value)
        {
            if (!_running || value == null)
                return;

            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                FlightMode.FlightMode = value.Value.value;
            });
        }

        private async void GPSSignalLevelChanged(object sender, FCGPSSignalLevelMsg? value)
        {
            if (!_running || value == null)
                return;

            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                GPS.SignalLevel = value.Value.value;
            });
        }

        private async void AltitudeChanged(object sender, DoubleMsg? value)
        {
            if (!_running || value == null)
                return;

            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Altitude.Altitude = value.Value.value;
            });
        }

        private async void VelocityChanged(object sender, Velocity3D? value)
        {
            if (!_running || value == null)
                return;

            var velocityNorth = value.Value.x;
            var velocityEast = value.Value.y;
            var velocityDown = value.Value.z;

            double horizontalMs = Math.Sqrt(velocityNorth * velocityNorth + velocityEast * velocityEast);
            double horizontalMph = horizontalMs * _MsMph;
            double verticalMph = (-velocityDown) * _MsMph;

            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Speed.Horizontal = horizontalMph;
                Speed.Vertical = verticalMph;
            });
        }
    }
}
