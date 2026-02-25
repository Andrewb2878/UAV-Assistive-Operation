using DJI.WindowsSDK;
using DJI.WindowsSDK.Components;
using System;
using System.Threading.Tasks;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;
using Windows.Devices.Geolocation;

namespace UAV_Assistive_Operation.Services
{
    public class DJISimulatorService
    {
        private FlightControllerHandler _flightControllerHandler;
        private SimulatorInitializationSettings? _simulatorSettings;

        //Events
        public event Action<bool> SimulatorStateChanged;


        public void AircraftConnected()
        {
            _flightControllerHandler = DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0);
        }

        public void AircraftDisconnected()
        {
            _flightControllerHandler = null;
        }


        //Initializes the simulator
        public async Task InitializeSimulatorAsync()
        {
            if (_flightControllerHandler == null)
                return;

            double latitude = 51.50141;
            double longitude = -0.14208;

            try
            {
                var geoLocator = new Geolocator { DesiredAccuracy = PositionAccuracy.High };
                var position = await geoLocator.GetGeopositionAsync();
                latitude = position.Coordinate.Point.Position.Latitude;
                longitude = position.Coordinate.Point.Position.Longitude;
            }
            catch
            {
                EventLogService.Instance.Log(LogEventType.Error, "SimulatorService: Location failed");
            }

            _simulatorSettings = new SimulatorInitializationSettings
            {
                latitude = latitude,
                longitude = longitude,
                satelliteCount = 20
            };

            EventLogService.Instance.Log(LogEventType.System, "Simulator initialized");
        }


        //Starts the simulator using the initialized settings
        public async Task StartSimulatorAsync()
        {
            if (_flightControllerHandler == null)
                return;

            var result = await _flightControllerHandler.StartSimulatorAsync(_simulatorSettings.Value);
            var message = DJIErrorDecoderModel.GetErrorMessage(result);

            if (message != null)
            {
                EventLogService.Instance.Log(LogEventType.Warning, $"Simulator failed{message}");
                return;
            }
            EventLogService.Instance.Log(LogEventType.System, "Simulator started");
            SimulatorStateChanged?.Invoke(true);
        }

        //Stops the simulator
        public async Task StopSimulatorAsync()
        {
            if (_flightControllerHandler == null)
                return;

            var result = await _flightControllerHandler.StopSimulatorAsync();
            var message = DJIErrorDecoderModel.GetErrorMessage(result);

            if (message != null)
            {
                EventLogService.Instance.Log(LogEventType.Warning, $"Simulator failed{message}");
                return;
            }
            EventLogService.Instance.Log(LogEventType.System, "Simulator stopped");
            _simulatorSettings = null;
            SimulatorStateChanged?.Invoke(false);
        }
    }
}
