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

        private DJITelemetryService _telemetryService;
        private DJIFlightDataService _flightDataService;

        private bool _isConfigured;
        private int _configAttempts;


        public void AircraftConnected(DJITelemetryService telemetryService, DJIFlightDataService flightDataService)
        {
            _telemetryService = telemetryService;
            _flightDataService = flightDataService;

            _flightDataService.NotEnoughForceChanged += NotEnoughForceDetected;
            _flightDataService.MotorStartFailureChanged += MotorStartFailureDetected;

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
            if (_flightDataService != null)
            {
                _flightDataService.NotEnoughForceChanged -= NotEnoughForceDetected;
                _flightDataService.MotorStartFailureChanged -= MotorStartFailureDetected;
            }


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
        

        //Events to monitor for flight safety
        private async void NotEnoughForceDetected(bool notEnoughForce)
        {
            if (notEnoughForce)
            {
                EventLogService.Instance.Log(LogEventType.Error, "Insufficient force to fly");
                await LandAsync(false);
            }
        }

        private async void MotorStartFailureDetected(FCMotorStartFailureError error)
        {
            if (error != FCMotorStartFailureError.NONE)
                await StopAsync();
        }


        //Aircraft command methods
        private async Task ExecuteFlightCommandAsync(Func<Task<SDKError>> command, string commandName, bool logResult)
        {
            if (!ValidateCommandExecution())
                return;

            var result = await command();
            var message = DJIErrorDecoderModel.GetErrorMessage(result);

            if (message != null && logResult)
            {
                EventLogService.Instance.Log(LogEventType.Warning, $"Command failed{message}");
            }
            else if (logResult)
            {
                EventLogService.Instance.Log(LogEventType.Info, $"Aircraft {commandName} successful");
            }
        }


        public async Task TakeoffAsync(bool logResult=true)
        {
            await ExecuteFlightCommandAsync(() => _flightController.StartTakeoffAsync(), "takeoff", logResult);
        }

        public async Task LandAsync(bool logResult=true)
        {
            await ExecuteFlightCommandAsync(() => _flightController.StartAutoLandingAsync(), "landing", logResult);
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
            //Ensures sufficient GPS signal before flight
            if (_isConfigured && !_telemetryService.GPS.SufficientForFlight && !_flightDataService.IsFlying)
            {
                EventLogService.Instance.Log(LogEventType.Warning, "Low GPS: flight operations disabled");
                return false;
            }

            //Ensures required handlers exist
            if (_isConfigured && _virtualController != null && _flightController != null)
                return true;


            EventLogService.Instance.Log(LogEventType.Warning, "Flight controller error: cannot issue flight command");
            return false;            
        }
    }
}
