using DJI.WindowsSDK;
using DJI.WindowsSDK.Components;
using System;
using Windows.UI.Core;

namespace UAV_Assistive_Operation.Services
{
    public class DJIFlightDataService
    {
        private FlightControllerHandler _flightControllerHandler;
        private bool _running;


        public double? Latitude { get; private set; }
        public double? Longitude { get; private set; }


        public event Action PositionUpdated;

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
            Latitude = null;
            Longitude = null;

            UnsubscribeToFlightController();
        }


        //Subscribing to events
        private void SubscribeToFlightController()
        {
            _flightControllerHandler = DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0);
            if (_flightControllerHandler != null)
            {
                _flightControllerHandler.AircraftLocationChanged += AircraftLocationChanged;
            }
        }


        //Unsubscribing from events
        private void UnsubscribeToFlightController()
        {
            if (_flightControllerHandler != null)
            {
                _flightControllerHandler.AircraftLocationChanged -= AircraftLocationChanged;
                _flightControllerHandler = null;
            }
        }


        //Getting updates from subscriptions
        private void AircraftLocationChanged(object sender, LocationCoordinate2D? value)
        {
            if (!_running || value == null)
                return;

            Latitude = value.Value.latitude;
            Longitude = value.Value.longitude;

            PositionUpdated?.Invoke();
        }
    }
}
