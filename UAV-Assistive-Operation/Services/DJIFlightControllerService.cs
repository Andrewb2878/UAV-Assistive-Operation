using DJI.WindowsSDK;
using DJI.WindowsSDK.Components;
using System;
using System.Threading.Tasks;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;

namespace UAV_Assistive_Operation.Services
{
    public class DJIFlightControllerService
    {
        private FlightControllerHandler _flightController;
        private FlightAssistantHandler _flightAssistant;
        private VirtualRemoteController _virtualController;
        private DJIFlightDataService _flightDataService;

        private bool _isConfigured;
        private int _configAttempts;


        public void AircraftConnected(DJIFlightDataService flightDataService)
        {
            _flightDataService = flightDataService;
            _flightController = DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0);
            _flightAssistant = DJISDKManager.Instance.ComponentManager.GetFlightAssistantHandler(0, 0);
            _virtualController = DJISDKManager.Instance.VirtualRemoteController;

            if (_flightController != null && _flightAssistant != null)
            {
                _configAttempts = 0;
                _ = InitializeAsync();
            }
        }

        public void AircraftDisconnected()
        {
            _isConfigured = false;
            _flightController = null;
            _flightAssistant = null;
            _virtualController = null;
        }


        private async Task InitializeAsync()
        {
            while (_configAttempts < 3)
            {
                _isConfigured = await ConfigureAircraftAsync();

                if (_isConfigured)
                {
                    EventLogService.Instance.Log(LogEventType.System, "Aircraft configured successfully");
                    return;
                }

                _configAttempts++;
                EventLogService.Instance.Log(LogEventType.Error, 
                    $"Aircraft Configuration failed: retrying {_configAttempts}/3");

                await Task.Delay(1000);
            }

            EventLogService.Instance.Log(LogEventType.Error,
                "Aircraft Configuration failed: reconnect the aircraft and try again");
        }


        //Configuring aircraft on connection
        private async Task<bool> ConfigureAircraftAsync()
        {
            var controller = _flightController;
            var assistant = _flightAssistant;
            var dataService = _flightDataService;

            if (controller == null || assistant == null || dataService == null)
                return false;

            try
            {
                if (dataService.CanConfigureAircraft())
                {
                    await controller.SetFailsafeActionAsync(new FCFailsafeActionMsg { value = FCFailsafeAction.LANDING });
                    await controller.SetLowBatteryWarningThresholdAsync(new IntMsg { value = 20 });
                    await controller.SetSeriousLowBatteryWarningThresholdAsync(new IntMsg { value = 10 });

                    await assistant.SetVisionAssistedPositioningEnabledAsync(new BoolMsg { value = true });
                    return true;
                }
                    return false;
            }
            catch (Exception error)
            {
                EventLogService.Instance.Log(LogEventType.Error, $"Configuration error: {error.Message}");
                return false;
            }
        }
        //Check about GPS requirement for flight operations. If not required use GPSStrengthTelemetryModel to force it to match the alert

        //Aircraft command methods
        private async Task ExecuteFlightCommandAsync(Func<Task<SDKError>> command, string commandName)
        {
            if (!ValidateCommandExecution())
                return;

            var result = await command();
            var message = DJIErrorDecoderModel.GetErrorMessage(result);

            if (message != null)
            {
                EventLogService.Instance.Log(LogEventType.Warning, $"Command failed: {message}");
            }
            else
            {
                EventLogService.Instance.Log(LogEventType.Info, $"Aircraft {commandName} successful");
            }
        }


        public async Task TakeoffAsync()
        {
            await ExecuteFlightCommandAsync(() => _flightController.StartTakeoffAsync(), "takeoff");
        }

        public async Task LandAsync()
        {
            await ExecuteFlightCommandAsync(() => _flightController.StartAutoLandingAsync(), "landing");
        }

        public async Task StopAsync()
        {
            if (!ValidateCommandExecution())
                return;

            _virtualController.UpdateJoystickValue(0f, 0f, 0f, 0f);

            _ = await _flightController.StopTakeoffAsync();
            _ = await _flightController.StopAutoLandingAsync();
        }

        public void VirtualStickCommand(float throttle, float yaw, float pitch, float roll)
        {
            if (!ValidateCommandExecution())
                return;

            throttle = Math.Clamp(throttle, -1f, 1f);
            yaw = Math.Clamp(yaw, -1f, 1f);
            pitch = Math.Clamp(pitch, -1f, 1f);
            roll = Math.Clamp(roll, -1f, 1f);

            _virtualController.UpdateJoystickValue(throttle, yaw, pitch, roll);
        }


        //Aircraft command validation method
        private bool ValidateCommandExecution()
        {
            if (_isConfigured && _virtualController != null && _flightController != null)
                return true;

            EventLogService.Instance.Log(LogEventType.Warning, "Flight controller error: can't issue flight command");
            return false;            
        }
    }
}
