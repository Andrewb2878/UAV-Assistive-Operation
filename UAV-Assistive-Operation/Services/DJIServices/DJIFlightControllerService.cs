using DJI.WindowsSDK;
using DJI.WindowsSDK.Components;
using System;
using System.Threading.Tasks;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;

namespace UAV_Assistive_Operation.Services
{
    /// <summary>
    /// <para> Sends data to the aircraft from the application </para>
    /// 
    /// <para> It configures aircraft parameters on connection, executes flight commands, sends virtual stick inputs, 
    /// enforces application safety systems and triggers emergency landings when required </para>
    /// </summary>
    public class DJIFlightControllerService
    {
        private FlightControllerHandler _flightController;
        private FlightAssistantHandler _flightAssistant;
        private VirtualRemoteController _virtualController;

        private DJIConnectionService _connectionService;
        private DJITelemetryService _telemetryService;
        private DJIFlightDataService _flightDataService;

        private ControllerConnectionService _controllerService;

        private bool _isConfigured;
        private int _configAttempts;

        //Joystick state tracking to reduce sending duplicate commands
        private float _lastThrottle;
        private float _lastYaw;
        private float _lastPitch;
        private float _lastRoll;
        private const float StickChangeThreshold = 0.01f;

        //state tracking to manage the stop command
        private bool _stopModeActive;
        private const float BrakingGain = 0.2f;
        private const double StopThreshold = 0.2;
        private const int BrakingUpdateFrequency = 80;


        private bool _motorStartFailure = false;
        private LandingState _landingState;
        private bool _notFlyingWarningLogged;


        /// <summary>
        /// Called when aircraft is connected to subscribe to required services and begins
        /// aircraft configuration
        /// </summary>
        public void AircraftConnected(DJIConnectionService connectionService, DJITelemetryService telemetryService,
            DJIFlightDataService flightDataService, ControllerConnectionService controllerService)
        {
            _connectionService = connectionService;
            _telemetryService = telemetryService;
            _flightDataService = flightDataService;
            _controllerService = controllerService;

            //subscribing to flight safety events
            _flightDataService.LandingConfirmationChanged += LandingConfirmationChangedAsync;
            _flightDataService.NotEnoughForceChanged += NotEnoughForceDetected;
            _flightDataService.MotorStartFailureChanged += MotorStartFailureDetected;
            _flightDataService.SeriousBatteryChanged += SeriousBatteryDetected;
            _controllerService.GamepadDisconnected += ControllerDisconnected;

            //Subscribing to events used during operation
            _flightDataService.FlyingChanged += FlyingChanged;
            _telemetryService.VisionAltitudeThresholdChanged += VisionAltitudeThresholdChanged;

            //DJI SDK handlers
            _flightController = DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0);
            _flightAssistant = DJISDKManager.Instance.ComponentManager.GetFlightAssistantHandler(0, 0);
            _virtualController = DJISDKManager.Instance.VirtualRemoteController;

            if (_flightController != null && _flightAssistant != null)
            {
                _configAttempts = 0;
                _ = InitializeAsync();
            }
        }

        /// <summary>
        /// Called when aircraft disconnects to unsubscribe from events and handlers and
        ///resets internal states
        /// </summary>
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
            if (_telemetryService != null)
            {
                _telemetryService.VisionAltitudeThresholdChanged -= VisionAltitudeThresholdChanged;
            }

            _controllerService.GamepadDisconnected -= ControllerDisconnected;

            _isConfigured = false;
            _motorStartFailure = false;
            _notFlyingWarningLogged = false;
            _flightController = null;
            _flightAssistant = null;
            _virtualController = null;
            _landingState = LandingState.None;

            _lastThrottle = 0f;
            _lastYaw = 0f;
            _lastPitch = 0f;
            _lastRoll = 0f;
        }


        /// <summary>
        /// Attempts aircraft configuration up to 3 times, setting failsafe events, battery
        /// thresholds and positioning settings
        /// </summary>
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
            // Safety check ensuring that the UAV isn't left hovering if configuration failure due to control lockout.
            // It should be impossible for the UAV to become airborne prior to configuration, this acts as a failsafe
            if (!_isConfigured && _flightDataService.IsFlying)
            {
                _ = _flightController.StartAutoLandingAsync();
            }
        }


        /// <summary>
        /// Configures the aircraft
        /// </summary>
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
                    await controller.SetHeightLimitAsync(new IntMsg { value = 120 });
                    await controller.SetLowBatteryWarningThresholdAsync(new IntMsg { value = 20 });
                    await controller.SetSeriousLowBatteryWarningThresholdAsync(new IntMsg { value = 10 });
                    await controller.SetMultipleFlightModeEnabledAsync(new BoolMsg { value = false });

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


        //Events to monitor for flight operations
        private void FlyingChanged(bool flying)
        {
            if (!flying)
                _landingState = LandingState.None;
        }

        private async void VisionAltitudeThresholdChanged(bool aboveThreshold)
        {
            if (_flightAssistant == null || !_flightDataService.IsFlying)
                return;

            try
            {
                await _flightAssistant.SetVisionAssistedPositioningEnabledAsync(new BoolMsg { value = !aboveThreshold });
            }
            catch (Exception error)
            {
                EventLogService.Instance.Log(LogEventType.Error, $"Vision assisted positioning change failed: {error.Message}");
            }
        }


        /// <summary>
        /// Validates safety constraints before sending the command
        /// </summary>
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


        /// <summary>
        /// Triggers aircraft takeoff
        /// </summary>
        public async Task TakeoffAsync(bool logResult=true)
        {
            await ExecuteFlightCommandAsync(() => _flightController.StartTakeoffAsync(), "takeoff", logResult);
        }

        /// <summary>
        /// Triggers aircraft landing sequence, can be triggered by user of safety systems
        /// </summary>
        public async Task LandAsync(bool logResult=true)
        {
            if (_landingState != LandingState.RequiredLanding)
                _landingState = LandingState.UserLanding;
            await ExecuteFlightCommandAsync(() => _flightController.StartAutoLandingAsync(), "landing", logResult);
        }

        /// <summary>
        /// Triggers aircraft final landing stage, only when landing is triggered by LandAsync
        /// </summary>
        private async void LandingConfirmationChangedAsync(bool landingConfirmation)
        {
            if (_landingState != LandingState.None && landingConfirmation)
            {
                _landingState = LandingState.None;
                await ExecuteFlightCommandAsync(() => _flightController.ConfirmLandingAsync(), "landing", false);
            }
        }

        /// <summary>
        /// Stops all aircraft movements including takeoff and landing based on safety requirements
        /// </summary>
        public async Task StopAsync(bool logResult=true)
        {
            if (!ValidateCommandExecution(logResult))
                return;

            _stopModeActive = true;

            await _flightController.StopTakeoffAsync();
            if (_landingState != LandingState.RequiredLanding)
            {
                await _flightController.StopAutoLandingAsync();
                _landingState = LandingState.None;
            }

            _ = Task.Run(() => RunBrakingLoop());        
        }

        // Calculates and applies an opposite force to counteract aircraft momentum
        private async Task RunBrakingLoop()
        {
            while (_stopModeActive)
            {
                var velocityData = _telemetryService.Speed;

                if (!velocityData.VelocityX.HasValue || !velocityData.VelocityY.HasValue || !velocityData.Horizontal.HasValue)
                {
                    await Task.Delay(BrakingUpdateFrequency);
                    continue;
                }

                //Checking if the aircraft has stopped
                if (velocityData.Horizontal.Value < StopThreshold)
                {
                    break;
                }

                //Calculating opposite force to be applied
                float counterPitch = (float)(-velocityData.VelocityX.Value * BrakingGain);
                float counterRoll = (float)(-velocityData.VelocityY.Value * BrakingGain);

                _virtualController.UpdateJoystickValue(0f, 0f, counterPitch, counterRoll);
                await Task.Delay(BrakingUpdateFrequency);
            }
            _virtualController.UpdateJoystickValue(0f, 0f, 0f, 0f);
            _stopModeActive = false;
        }

        /// <summary>
        /// Sends real-time joystick commands to the aircraft
        /// </summary>
        public async void VirtualStickCommandAsync(float throttle, float yaw, float pitch, float roll)
        {
            bool changed = HasStickChanged(throttle, yaw, pitch, roll);
            if (changed && _stopModeActive)
            {
                _stopModeActive = false;
            }
            else if (!changed)
            {
                return;
            }

            if (_landingState == LandingState.RequiredLanding)
                return;


            if (_flightDataService == null || !_flightDataService.IsFlying)
            {
                if (!_notFlyingWarningLogged)
                {
                    EventLogService.Instance.Log(LogEventType.Warning, "Aircraft not flying: cannot perform action");
                    _notFlyingWarningLogged = true;
                }
                return;
            }
            else
            {
                _notFlyingWarningLogged = false;
            }

            if (!ValidateCommandExecution())
                return;

            //Clamping input ensuring values are within the range accepted by UpdateJoystickValue
            throttle = Math.Clamp(throttle, -1f, 1f);
            yaw = Math.Clamp(yaw, -1f, 1f);
            pitch = Math.Clamp(pitch, -1f, 1f);
            roll = Math.Clamp(roll, -1f, 1f);

            if (_landingState == LandingState.UserLanding)
            {
                _landingState = LandingState.None;
                await _flightController.StopAutoLandingAsync();
            }

            //Stops the aircraft from ascending if it is at the configured height limit
            if (_flightDataService.IsNearHeightLimit && throttle > 0)
                throttle = 0;

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


        /// <summary>
        /// <para> Ensures that flight commands are safe to execute </para>
        /// 
        /// <para> Blocks commands during motor start failures, low battery (not flying), low GPS
        /// and handler misconfiguration </para>
        /// </summary>
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
