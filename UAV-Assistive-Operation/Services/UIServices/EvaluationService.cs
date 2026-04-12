using DJI.WindowsSDK;
using System.ComponentModel;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;
using Windows.Gaming.Input;

namespace UAV_Assistive_Operation.Services
{
    /// <summary>
    /// <para> Evaluation and safety service </para>
    /// 
    /// <para> Evaluating current flight state, safety conditions, hardware disconnections
    /// and aircraft warnings. Using this data to update AlertService, log events and
    /// calculate active flight status </para>
    /// </summary>
    public class EvaluationService
    {
        private readonly DJIConnectionService _connectionService;
        private readonly DJITelemetryService _telemetryService;
        private readonly DJIFlightDataService _flightDataService;
        private readonly ControllerConnectionService _controllerService;
        private readonly AlertService _alertService;


        /// <summary>
        /// Subscribes to all required services and performs an initial evaluation
        /// of aircraft and controller state
        /// </summary>
        public EvaluationService(
            DJIConnectionService connectionService,
            DJITelemetryService telemetryService,
            DJIFlightDataService flightDataService,
            ControllerConnectionService controllerService,
            AlertService alertService)
        {
            _connectionService = connectionService;
            _telemetryService = telemetryService;
            _flightDataService = flightDataService;
            _controllerService = controllerService;
            _alertService = alertService;

            //Aircraft monitoring
            _connectionService.AircraftConnected += AircraftConnected;
            _connectionService.AircraftDisconnected += AircraftDisconnected;
            _flightDataService.FlyingChanged += FlyingChanged;

            //Telemetry monitoring
            _telemetryService.GPS.PropertyChanged += GPSChanged;
            _flightDataService.HeightLimitReachedChanged += HeightLimitReachedChanged;
            _flightDataService.WindWarningChanged += WindWarningChanged;
            _flightDataService.VisionAssistedPositioningChanged += VisionAssistedPositioningChanged;

            //Battery and safety monitoring
            _flightDataService.SeriousBatteryChanged += SeriousLowBatteryChanged;
            _flightDataService.LowBatteryChanged += LowBatteryChanged;
            _flightDataService.MotorStartFailureChanged += MotorStartFailureChanged;

            //Mode monitoring
            _flightDataService.SimulatorStartedChanged += SimulatorChanged;

            //Controller monitoring
            _controllerService.GamepadConnected += ControllerConnected;
            _controllerService.GamepadDisconnected += ControllerDisconnected;

            //Initial evaluation 
            EvaluateFlightStatus();
            EvaluateControllerDisconnection();
        }


        /// <summary>
        /// Evaluates aircraft flight state
        /// </summary>
        private void EvaluateFlightStatus()
        {
            if (!_connectionService.IsAircraftConnected)
            {
                _alertService.ClearAlerts();
                _alertService.FlightStatus("Aircraft Disconnected"); return;
            }
            if (_flightDataService.IsFlying)
            {
                _alertService.FlightStatus(_flightDataService.IsVisionAssistedPositioningEnabled ? "In-Flight (Vision)" : "In-Flight"); return;
            }
            if (_telemetryService.GPS.SufficientForFlight)
            {
                _alertService.FlightStatus("Ready to Takeoff");
            }
            else
            {
                _alertService.FlightStatus("Low GPS: Cannot Takeoff");
            }
        }

        /// <summary>
        /// Evaluates battery alerts based on battery and flying state
        /// </summary>
        private void EvaluateBatteryStatus()
        {
            if (!_connectionService.IsAircraftConnected)
                return;

            if (_flightDataService.IsSeriousLowBattery)
            {
                _alertService.AlertState("SeriousBattery", _flightDataService.IsSeriousLowBattery,
                    _flightDataService.IsFlying ? "Critically Low Battery: Auto Landing" : "Critically Low Battery", 1);
                EventLogService.Instance.Log(LogEventType.Warning, "Critically low aircraft battery");
            }
            else if (_flightDataService.IsLowBattery)
            {
                if (_flightDataService.IsFlying)
                {
                    _alertService.AlertState("LowBattery", _flightDataService.IsLowBattery, "Low Battery Warning", 4);
                    EventLogService.Instance.Log(LogEventType.Warning, "Low aircraft battery: aircraft will auto land shortly");
                }
                else
                {
                    _alertService.AlertState("LowBattery", _flightDataService.IsLowBattery, "Low Battery: Cannot Takeoff", 2);
                    EventLogService.Instance.Log(LogEventType.Warning, "Low aircraft battery: cannot takeoff");
                }
            }
            else
            {
                _alertService.AlertState("SeriousBattery", false, "", 1);
                _alertService.AlertState("LowBattery", false, "", 4);
            }
        }

        /// <summary>
        /// Evaluates controller connection state
        /// </summary>
        private void EvaluateControllerDisconnection()
        {
            _alertService.AlertState("ControllerConnection", !_controllerService.IsControllerConnected, "Controller Disconnected", 2);
        }



        //Aircraft handlers
        private void AircraftConnected() 
        {
            EvaluateFlightStatus();
            EvaluateBatteryStatus();
        }

        private void AircraftDisconnected()
        {
            EvaluateFlightStatus();
        }

        private void FlyingChanged(bool flying)
        {
            EvaluateFlightStatus();
            EvaluateBatteryStatus();
        }



        //Telemetry handlers
        private void GPSChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName != nameof(GPSStrengthTelemetryModel.SufficientForFlight))
                return;

            EvaluateFlightStatus();
        }

        private void HeightLimitReachedChanged(bool heightLimitReached)
        {
            if (heightLimitReached)
            {
                _alertService.AlertState("HeightLimit", heightLimitReached, "Height Limit Reached", 4);
                EventLogService.Instance.Log(LogEventType.Warning, "Height limit reached");
            }
        }

        private void WindWarningChanged(FCWindWarning level)
        {
            switch (level)
            {
                case FCWindWarning.LEVEL_0:
                    _alertService.AlertState("Wind", false, string.Empty, 6); break;
                case FCWindWarning.LEVEL_1:
                    _alertService.AlertState("Wind", true, "High Wind: Fly with caution", 5);
                    EventLogService.Instance.Log(LogEventType.Warning, "High Wind: Fly with caution"); break;
                case FCWindWarning.LEVEL_2:
                    _alertService.AlertState("Wind", true, "Strong Wind: Land when possible", 3);
                    EventLogService.Instance.Log(LogEventType.Warning, "Strong Wind: Land when possible"); break;
                case FCWindWarning.UNKNOWN:
                    _alertService.AlertState("Wind", false, string.Empty, 6); break;
            }
        }

        private void VisionAssistedPositioningChanged(bool visionAssistedPositioning)
        {
            EventLogService.Instance.Log(LogEventType.Info, $"Vision assisted positioning {(visionAssistedPositioning ? "enabled" : "disabled")}");
            EvaluateFlightStatus();
        }



        //Battery handlers
        private void SeriousLowBatteryChanged(bool seriousBattery)
        {
            EvaluateBatteryStatus();
        }

        private void LowBatteryChanged(bool lowBattery)
        {
            EvaluateBatteryStatus();  
        }



        //Controller handlers
        private void ControllerConnected(Gamepad gamepad)
        {
            EvaluateControllerDisconnection();
        }

        private void ControllerDisconnected()
        {
            EvaluateControllerDisconnection();
        }

        
        //Aircraft error handling
        private void MotorStartFailureChanged(FCMotorStartFailureError error)
        {
            if (error == FCMotorStartFailureError.NONE || error == FCMotorStartFailureError.UNKNOWN)
            {
                _alertService.AlertState("MotorStart", false, string.Empty, 2);
                return;
            }

            string errorMsg = "";

            switch (error)
            {
                case FCMotorStartFailureError.COMPASS_ERROR:
                case FCMotorStartFailureError.COMPASS_NOT_WORKING:
                case FCMotorStartFailureError.COMPASS_ABNORMAL:
                case FCMotorStartFailureError.COMPASS_IMU_ORI_NOT_MATCH:
                    errorMsg = "Compass Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.COMPASS_CALIBRATING:
                case FCMotorStartFailureError.COMPASS_LARGE_MOD:
                case FCMotorStartFailureError.COMPASS_LARGE_NOISE:
                    errorMsg = "Recalibrate Compass: Cannot Takeoff"; break;
                case FCMotorStartFailureError.ASSISTANT_PROTECTED:
                case FCMotorStartFailureError.REMOTE_USB_CONNECTED:
                    errorMsg = "Aircraft Connected to USB: Cannot Takeoff"; break;
                case FCMotorStartFailureError.DEVICE_LOCKED:
                case FCMotorStartFailureError.LOCK_BY_APP:
                    errorMsg = "Aircraft Locked: Cannot Takeoff"; break;
                case FCMotorStartFailureError.DISTANCE_LIMIT:
                    errorMsg = "Aircraft too far from Home Point: Cannot Takeoff"; break;
                case FCMotorStartFailureError.START_FLY_HEIGHT_ERROR:
                    errorMsg = "Altitude Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.IMU_ORI_NOT_MATCH:
                case FCMotorStartFailureError.IMU_DISCONNECT:
                    errorMsg = "IMU Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.IMU_WARMING_UP:
                    errorMsg = "IMU Warming up: Cannot Takeoff"; break;
                case FCMotorStartFailureError.IMU_INIT_ERROR:
                    errorMsg = "IMU Initializing: Cannot Takeoff"; break;
                case FCMotorStartFailureError.IMU_NEED_CALIBRATION:
                case FCMotorStartFailureError.IMUING_ERROR:
                    errorMsg = "Recalibrate IMU: Cannot Takeoff"; break;
                case FCMotorStartFailureError.IMU_CALIBRATION_FINISHED:
                    errorMsg = "Reboot Required: Cannot Takeoff"; break;
                case FCMotorStartFailureError.ATTI_ERROR:
                    errorMsg = "Attitude Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.ATTITUDE_ABNORMAL:
                case FCMotorStartFailureError.ATTI_ANGLE_OVER:
                    errorMsg = "Aircraft Tilted: Cannot Takeoff"; break;
                case FCMotorStartFailureError.NOVICE_PROTECTED:
                    errorMsg = "Insufficient GPS: Cannot Takeoff"; break;
                case FCMotorStartFailureError.GPS_ABNORMAL:
                case FCMotorStartFailureError.GPS_DISCONNECT:
                case FCMotorStartFailureError.GPS_SIGN_INVALID:
                    errorMsg = "GPS Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.BATTERIES_VERSION_MISMATCH:
                case FCMotorStartFailureError.LOW_VERSION_OF_BATTERY:
                    errorMsg = "Battery Firmware Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.BATTERY_AUTH_ERROR:
                case FCMotorStartFailureError.BATTERY_CELL_ERROR:
                case FCMotorStartFailureError.BATTERY_COMMUNICATION_ERROR:
                case FCMotorStartFailureError.MULT_BATTERIES_COMM_ERR:
                    errorMsg = "Battery Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.BATTERIES_VOLT_DIFF_LARGE:
                case FCMotorStartFailureError.VOLTAGE_OF_BATTERY_IS_TOO_HIGH:
                case FCMotorStartFailureError.BATTERIES_VOLT_DIFF_VERY_LARGE:
                case FCMotorStartFailureError.SERIOU_LOW_VOLTAGE:
                case FCMotorStartFailureError.LOW_VOLTAGE:
                    errorMsg = "Battery Voltage Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.SERIOU_LOW_POWER:
                case FCMotorStartFailureError.SMART_LOW_TO_LAND:
                    errorMsg = "Low Battery Charge: Cannot Takeoff"; break;
                case FCMotorStartFailureError.BATTERY_NOT_PRESENT:
                case FCMotorStartFailureError.BATTERY_INSTALL_ERROR:
                    errorMsg = "Battery Install Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.MISSING_BATTERIES:
                    errorMsg = "Missing Batteries: Cannot Takeoff"; break;
                case FCMotorStartFailureError.BATTERY_NOT_READY:
                    errorMsg = "Battery Initializing: Cannot Takeoff"; break;
                case FCMotorStartFailureError.TEMPERATURE_TOO_LOW:
                    errorMsg = "Temperature too Low: Cannot Takeoff"; break;
                case FCMotorStartFailureError.SIMULATOR_MODE:
                case FCMotorStartFailureError.SIMULATOR_STARTED:
                    break;
                case FCMotorStartFailureError.IN_TRANSPORT_MODE:
                    errorMsg = "Transport Mode: Cannot Takeoff"; break;
                case FCMotorStartFailureError.NOT_ACTIVATED:
                    errorMsg = "Aircraft Not Activated: Cannot Takeoff"; break;
                case FCMotorStartFailureError.IN_NO_FLY_ZONE:
                    errorMsg = "No Fly Zone: Cannot Takeoff"; break;
                case FCMotorStartFailureError.BIAS_ERROR:
                    errorMsg = "Sensor Bias too Large: Cannot Takeoff"; break;
                case FCMotorStartFailureError.ESC_ERROR:
                case FCMotorStartFailureError.ESC_BEEPING:
                    errorMsg = "ESC Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.ESC_CALIBRATING:
                    errorMsg = "ESC Calibrating: Cannot Takeoff"; break;
                case FCMotorStartFailureError.ESC_OVER_HEAT:
                    errorMsg = "ESC Overheated: Cannot Takeoff"; break;
                case FCMotorStartFailureError.ESC_VERSION_NOT_MATCH:
                    errorMsg = "ESC Firmware Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.SYSTEM_UPGRADE:
                    errorMsg = "System Updating: Cannot Takeoff"; break;
                case FCMotorStartFailureError.GYROSCOPE_LARGE_BIAS:
                    errorMsg = "Gyroscope Bias too Large: Cannot Takeoff"; break;
                case FCMotorStartFailureError.GYROSCOPE_NOT_WORKING:
                case FCMotorStartFailureError.GRYO_ACC_ABNORMAL:
                    errorMsg = "Gyroscope Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.ACCELERATOR_NOT_WORKING:
                    errorMsg = "Accelerometer Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.ACCELERATOR_LARGE_BIAS:
                    errorMsg = "Accelerometer Bias too Large: Cannot Takeoff"; break;
                case FCMotorStartFailureError.BAROMETER_NEGATIVE:
                case FCMotorStartFailureError.BAROMETER_NOT_WORKING:
                case FCMotorStartFailureError.BARO_ABNORMAL:
                    errorMsg = "Barometer Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.BAROMETER_LARGE_NOISE:
                    errorMsg = "Recalibrate Barometer: Cannot Takeoff"; break;
                case FCMotorStartFailureError.INVALID_SN:
                    errorMsg = "Invalid Serial Number: Cannot Takeoff"; break;
                case FCMotorStartFailureError.FLASH_OPERATING:
                    errorMsg = "Flash Operating: Cannot Takeoff"; break;
                case FCMotorStartFailureError.SDCARD_EXCEPTION:
                    errorMsg = "Data Logging Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.INVALID_FLOAT:
                    errorMsg = "Data Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.RC_CALIBRATION:
                case FCMotorStartFailureError.RC_NEED_CALI:
                case FCMotorStartFailureError.RC_CALIBRATION_UNFINISHED:
                    errorMsg = "Recalibrate RC: Cannot Takeoff"; break;
                case FCMotorStartFailureError.RC_CALIBRATION_EXCEPTION:
                case FCMotorStartFailureError.RC_MAPPING_ERROR:
                case FCMotorStartFailureError.RC_STICK_CENTER_ERROR:
                    errorMsg = "RC Error: Cannot Takeoff";  break;
                case FCMotorStartFailureError.AIRCRAFT_TYPE_MISMATCH:
                    errorMsg = "Aircraft Type Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.NOT_CONFIGURED_MODULES:
                    errorMsg = "Module Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.NAV_SYS_EXCEPTION:
                    errorMsg = "Navigation System Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.TOPOLOGY_ABNORMAL:
                    errorMsg = "Topology Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.GIMBAL_GYRO_ABNORMAL:
                case FCMotorStartFailureError.GIMBAL_ESC_PITCH_NON_DATA:
                case FCMotorStartFailureError.GIMBAL_ESC_ROLL_NON_DATA:
                case FCMotorStartFailureError.GIMBAL_ESC_YAW_NON_DATA:
                case FCMotorStartFailureError.GIMBAL_DISORDER:
                    errorMsg = "Gimbal Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.GIMBAL_FIRM_IS_UPDATING:
                    errorMsg = "Gimbal Updating: Cannot Takeoff"; break;
                case FCMotorStartFailureError.GIMBAL_PITCH_VIBRATE:
                case FCMotorStartFailureError.GIMBAL_ROLL_VIBRATE:
                case FCMotorStartFailureError.GIMBAL_YAW_VIBRATE:
                    errorMsg = "Gimbal Vibrations: Cannot Takeoff"; break;
                case FCMotorStartFailureError.TAKEOFF_ROLLOVER:
                    errorMsg = "Tilting After Takeoff"; break;
                case FCMotorStartFailureError.MOTOR_STUCK:
                case FCMotorStartFailureError.MOTOR_UNBALANCED:
                case FCMotorStartFailureError.MOTOR_START_ERROR:
                    errorMsg = "Motor Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.MISSING_PROPELLER:
                    errorMsg = "Missing Propeller: Cannot Takeoff"; break;
                case FCMotorStartFailureError.MOTOR_AUTO_TAKEOFF_FAIL:
                    errorMsg = "Auto Takeoff Failed"; break;
                case FCMotorStartFailureError.RTK_BAD_SIGNAL:
                    errorMsg = "Weak Signal: Cannot Takeoff"; break;
                case FCMotorStartFailureError.RTK_DEVIATION_ERROR:
                    errorMsg = "RTK Bias Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.BE_IMPACT:
                    errorMsg = "Collision: Cannot Takeoff"; break;
                case FCMotorStartFailureError.CRASH:
                    errorMsg = "Takeoff Error: Cannot Takeoff"; break;
                case FCMotorStartFailureError.COOLING_FAN_EXCEPTION:
                    errorMsg = "Processor Overheated: Cannot Takeoff"; break;
                default:
                    errorMsg = "Unknown Motor Error"; break;
            }

            if (!string.IsNullOrEmpty(errorMsg))
            {
                _alertService.AlertState("MotorStart", true, errorMsg, 2);
                EventLogService.Instance.Log(LogEventType.Error, errorMsg);
            }
        }


        
        // Mode handlers
        private void SimulatorChanged(bool simulatorStarted)
        {
            _alertService.AlertState("Simulator", simulatorStarted, "Simulator Mode", 3);
            EvaluateFlightStatus();
        }
    }
}
