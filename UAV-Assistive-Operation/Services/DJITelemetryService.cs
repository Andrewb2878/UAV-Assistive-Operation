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


        public BatteryModel Battery { get; } = new BatteryModel();
        public AltitudeModel Altitude { get; } = new AltitudeModel();
        public SpeedModel Speed { get; } = new SpeedModel();


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
            else
            {
                Debug.WriteLine("Battery handler not available");
            }
        }

        private void SubscribeToFlightController()
        {
            _flightControllerHandler = DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0);
            if (_flightControllerHandler != null)
            {
                _flightControllerHandler.AltitudeChanged += AltitudeChanged;
                _flightControllerHandler.VelocityChanged += VelocityChanged;
                Debug.WriteLine("Subscribed to altitude and velocity updates");
            }
            else
            {
                Debug.WriteLine("Flight controller hander not available");
            }
        }

        //Unsubscribing from events
        private void UnsubscribeFromBattery()
        {
            if (_batteryHandler != null)
            {
                _batteryHandler.ChargeRemainingInPercentChanged -= BatteryPercentChanged;
                _batteryHandler = null;
                Debug.WriteLine("Unsubscribed to battery percentge updates");
            }
        }

        private void UnsubscribeFromFlightController()
        {
            if ( _flightControllerHandler != null)
            {
                _flightControllerHandler.AltitudeChanged -= AltitudeChanged;
                _flightControllerHandler.VelocityChanged -= VelocityChanged;
                _flightControllerHandler = null;
                Debug.WriteLine("Unsubscribed from altitude and velocity updates");
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

        private async void AltitudeChanged(object sender, DoubleMsg? value)
        {
            if (!_running || value == null)
                return;

            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Debug.WriteLine($"Altitude {value.Value.value:F1}");
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
