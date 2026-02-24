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

        private DJIConnectionService _connectionService;
        private DJITelemetryService _telemetryService;
        private DJIFlightDataService _flightDataService;

        private bool _isConfigured;
        private int _configAttempts;

        private float _lastThrottle;
        private float _lastYaw;
        private float _lastPitch;
        private float _lastRoll;
        private const float StickChangeThreshold = 0.01f;

        private bool _autoLanding = false;


        public void AircraftConnected(DJIConnectionService connectionService, DJITelemetryService telemetryService,
            DJIFlightDataService flightDataService)
        {
            _connectionService = connectionService;
            _telemetryService = telemetryService;
            _flightDataService = flightDataService;

            _flightDataService.LandingConfirmationChanged += LandingConfirmationChangedAsync;

            _flightDataService.NotEnoughForceChanged += NotEnoughForceDetected;
            _flightDataService.MotorStartFailureChanged += MotorStartFailureDetected;
            _flightDataService.SeriousBatteryChanged += SeriousBatteryDetected;

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
                _flightDataService.LandingConfirmationChanged -= LandingConfirmationChangedAsync;

                _flightDataService.NotEnoughForceChanged -= NotEnoughForceDetected;
                _flightDataService.MotorStartFailureChanged -= MotorStartFailureDetected;
                _flightDataService.SeriousBatteryChanged -= SeriousBatteryDetected;
            }

            _isConfigured = false;
            _autoLanding = false;
            _flightController = null;
            _flightAssistant = null;
            _virtualController = null;

            _lastThrottle = 0f;
            _lastYaw = 0f;
            _lastPitch = 0f;
            _lastRoll = 0f;
        }


        private async Task InitializeAsync()
        {
            while (_configAttempts < 3)
            {
                _isConfigured = await ConfigureAircraftAsync();

                if (_isConfigured && _connectionService.IsAircraftConnected)
                {
                    EventLogService.Instance.Log(LogEventType.System, "Aircraft configured successfully");
                    return;
                }
                else if (!_connectionService.IsAircraftConnected)
                {
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
            if (error != FCMotorStartFailureError.NONE &&
                error != FCMotorStartFailureError.SIMULATOR_MODE &&
                error != FCMotorStartFailureError.SIMULATOR_STARTED)
                await StopAsync();
        }

        private async void SeriousBatteryDetected(bool seriousBattery)
        {
            if (_flightDataService.IsFlying)
                await LandAsync(false);
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
                EventLogService.Instance.Log(LogEventType.Info, $"Aircraft started {commandName} successfully");
            }
        }


        public async Task TakeoffAsync(bool logResult=true)
        {
            await ExecuteFlightCommandAsync(() => _flightController.StartTakeoffAsync(), "takeoff", logResult);
        }

        public async Task LandAsync(bool logResult=true)
        {
            _autoLanding = true;
            await ExecuteFlightCommandAsync(() => _flightController.StartAutoLandingAsync(), "landing", logResult);
        }

        public async Task StopAsync()
        {
            if (!ValidateCommandExecution())
                return;

            _virtualController.UpdateJoystickValue(0f, 0f, 0f, 0f);

            _ = await _flightController.StopTakeoffAsync();
            _ = await _flightController.StopAutoLandingAsync();
            _autoLanding = false;
        }

        public void VirtualStickCommand(float throttle, float yaw, float pitch, float roll)
        {
            if (_flightDataService == null || !_flightDataService.IsFlying)
                return;
            if (!ValidateCommandExecution())
                return;

            throttle = Math.Clamp(throttle, -1f, 1f);
            yaw = Math.Clamp(yaw, -1f, 1f);
            pitch = Math.Clamp(pitch, -1f, 1f);
            roll = Math.Clamp(roll, -1f, 1f);

            if (!HasStickChanged(throttle, yaw, pitch, roll))
                return;

            _virtualController.UpdateJoystickValue(throttle, yaw, pitch, roll);

            _lastThrottle = throttle;
            _lastYaw = yaw;
            _lastPitch = pitch;
            _lastRoll = roll;
        }

        private bool HasStickChanged(float throttle, float yaw, float pitch, float roll)
        {
            return Math.Abs(throttle - _lastThrottle) > StickChangeThreshold ||
                Math.Abs(yaw - _lastYaw) > StickChangeThreshold ||
                Math.Abs(pitch - _lastPitch) > StickChangeThreshold ||
                Math.Abs(roll - _lastRoll) > StickChangeThreshold;
        }

        //Landing management
        private async void LandingConfirmationChangedAsync(bool landingConfirmation)
        {
            if (_autoLanding && landingConfirmation)
            {
                _autoLanding = false;
                await ExecuteFlightCommandAsync(() => _flightController.ConfirmLandingAsync(), "landing", false);
            }
        }


        //Aircraft command validation method
        private bool ValidateCommandExecution()
        {
            //Ensures sufficient battery for flight
            if (_isConfigured && (_flightDataService.IsLowBattery || _flightDataService.IsSeriousLowBattery) &&
                !_flightDataService.IsFlying)
            {
                return false;
            }
                
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
