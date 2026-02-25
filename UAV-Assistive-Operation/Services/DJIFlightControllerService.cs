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

        private ControllerService _controllerService;

        private bool _isConfigured;
        private int _configAttempts;

        private float _lastThrottle;
        private float _lastYaw;
        private float _lastPitch;
        private float _lastRoll;
        private const float StickChangeThreshold = 0.01f;

        private bool _motorStartFailure = false;
        private LandingState _landingState;
        private bool _loggedNotFlying;


        public void AircraftConnected(DJIConnectionService connectionService, DJITelemetryService telemetryService,
            DJIFlightDataService flightDataService, ControllerService controllerService)
        {
            _connectionService = connectionService;
            _telemetryService = telemetryService;
            _flightDataService = flightDataService;
            _controllerService = controllerService;

            _flightDataService.LandingConfirmationChanged += LandingConfirmationChangedAsync;
            _flightDataService.FlyingChanged += FlyingChanged;
            _flightDataService.NotEnoughForceChanged += NotEnoughForceDetected;
            _flightDataService.MotorStartFailureChanged += MotorStartFailureDetected;
            _flightDataService.SeriousBatteryChanged += SeriousBatteryDetected;

            _controllerService.GamepadDisconnected += ControllerDisconnected;

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
                _flightDataService.FlyingChanged -= FlyingChanged;
                _flightDataService.NotEnoughForceChanged -= NotEnoughForceDetected;
                _flightDataService.MotorStartFailureChanged -= MotorStartFailureDetected;
                _flightDataService.SeriousBatteryChanged -= SeriousBatteryDetected;
            }

            _controllerService.GamepadDisconnected -= ControllerDisconnected;

            _isConfigured = false;
            _motorStartFailure = false;
            _loggedNotFlying = false;
            _flightController = null;
            _flightAssistant = null;
            _virtualController = null;
            _landingState = LandingState.None;

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
        private async void ControllerDisconnected()
        {
            if (_flightDataService.IsFlying)
            {
                _landingState = LandingState.RequiredLanding;
                await LandAsync(false);
            }
        }

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
            {
                _motorStartFailure = true;
                await StopAsync(false);
            }
            else if (error == FCMotorStartFailureError.NONE)
            {
                _motorStartFailure = false;
            }
                
        }

        private async void SeriousBatteryDetected(bool seriousBattery)
        {
            if (_flightDataService.IsFlying)
            {
                _landingState = LandingState.RequiredLanding;
                await LandAsync(false);
            }
        }

        private void FlyingChanged(bool flying)
        {
            if (!flying)
                _landingState = LandingState.None;
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
            if (_landingState != LandingState.RequiredLanding)
                _landingState = LandingState.UserLanding;
            await ExecuteFlightCommandAsync(() => _flightController.StartAutoLandingAsync(), "landing", logResult);
        }

        public async Task StopAsync(bool logResult=true)
        {
            if (!ValidateCommandExecution(logResult))
                return;

            _virtualController.UpdateJoystickValue(0f, 0f, 0f, 0f);

            await _flightController.StopTakeoffAsync();
            if (_landingState == LandingState.RequiredLanding)
                return;
            
            await _flightController.StopAutoLandingAsync();
            _landingState = LandingState.None;           
        }

        public async void VirtualStickCommandAsync(float throttle, float yaw, float pitch, float roll)
        {
            if (_landingState == LandingState.RequiredLanding)
                return;

            bool changed = HasStickChanged(throttle, yaw, pitch, roll);
            if (!changed)
                return;

            if (_flightDataService == null || !_flightDataService.IsFlying)
            {
                if (!_loggedNotFlying)
                {
                    EventLogService.Instance.Log(LogEventType.Warning, "Aircraft not flying: cannot perform action");
                    _loggedNotFlying = true;
                }
                return;
            }
            else
            {
                _loggedNotFlying = false;
            }


            if (!ValidateCommandExecution())
                return;

            throttle = Math.Clamp(throttle, -1f, 1f);
            yaw = Math.Clamp(yaw, -1f, 1f);
            pitch = Math.Clamp(pitch, -1f, 1f);
            roll = Math.Clamp(roll, -1f, 1f);

            if (_landingState == LandingState.UserLanding)
            {
                _landingState = LandingState.None;
                await _flightController.StopAutoLandingAsync();
            }

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
            if (_landingState != LandingState.None && landingConfirmation)
            {
                _landingState = LandingState.None;
                await ExecuteFlightCommandAsync(() => _flightController.ConfirmLandingAsync(), "landing", false);
            }
        }


        //Aircraft command validation method
        private bool ValidateCommandExecution(bool logResult=true)
        {
            if (_motorStartFailure)
            {
                if (logResult)                
                    EventLogService.Instance.Log(LogEventType.Warning, "Flight operations disabled");
                
                return false;
            }

            //Ensures sufficient battery for flight
            if (_isConfigured && (_flightDataService.IsLowBattery || _flightDataService.IsSeriousLowBattery) &&
                !_flightDataService.IsFlying)
            {
                return false;
            }
                
            //Ensures sufficient GPS signal before flight
            if (_isConfigured && !_telemetryService.GPS.SufficientForFlight && !_flightDataService.IsFlying)
            {
                if (logResult)
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
