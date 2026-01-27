using DJI.WindowsSDK;
using DJI.WindowsSDK.Components;
using System;
using System.Diagnostics;

namespace UAV_Assistive_Operation.Services
{
    public class DJIFlightDataService
    {
        private FlightControllerHandler _flightControllerHandler;
        private readonly MapService _mapService;
        private bool _running;


        public event Action<double, double> UavLocationUpdated;
        public event Action<double> UAVHeadingUpdated;

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
                _flightControllerHandler.AircraftLocationChanged += AircraftLocationChanged;
                _flightControllerHandler.AttitudeChanged += AircraftAttitudeChanged;
            }
        }


        //Unsubscribing from events
        private void UnsubscribeToFlightController()
        {
            if (_flightControllerHandler != null)
            {
                _flightControllerHandler.AircraftLocationChanged -= AircraftLocationChanged;
                _flightControllerHandler.AttitudeChanged -= AircraftAttitudeChanged;
                _flightControllerHandler = null;
            }
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
    }
}
