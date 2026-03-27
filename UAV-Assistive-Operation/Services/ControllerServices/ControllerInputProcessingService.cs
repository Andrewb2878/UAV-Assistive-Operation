using System;
using System.Collections.Generic;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;

namespace UAV_Assistive_Operation.Services
{
    /// <summary>
    /// Processes mapped controller inputs and assigns correct inputs depending on the
    /// current application mode
    /// 
    /// Converts controller inputs into application controls, handles flight commands, 
    /// process virtual joystick inputs and changes behaviour based on the InputMode
    /// </summary>
    public class ControllerInputProcessingService
    {
        //Services
        private readonly ControllerMappingService _mappingService;
        private readonly DJIFlightControllerService _flightControllerService;
        private readonly FlightCommandViewModel _flightCommand;
        private readonly MenuViewModel _menuViewModel;
        private readonly SimulatorWarningViewModel _simulatorWarningViewModel;

        //Current application input mode
        private InputMode _mode = InputMode.Flight;
        //Stores previous button states for edge detection
        private readonly Dictionary<ApplicationControls, bool> _previousState = 
            new Dictionary<ApplicationControls, bool>();

        //Threshold values
        private const double PressThreshold = 0.8;
        private const double DeadZoneThreshold = 0.07;


        /// <summary>
        /// Subscribes to all required services and view models
        /// </summary>
        public ControllerInputProcessingService(ControllerMappingService mappingService, 
            DJIFlightControllerService flightControllerService, FlightCommandViewModel flightCommand, MenuViewModel menuViewModel,
            SimulatorWarningViewModel simulatorWarningViewModel)
        {
            _mappingService = mappingService;
            _flightControllerService = flightControllerService;
            _flightCommand = flightCommand;
            _menuViewModel = menuViewModel;
            _simulatorWarningViewModel = simulatorWarningViewModel;
        }

        /// <summary>
        /// Subscribes to controller update events
        /// </summary>
        public void Start()
        {
            App.ControllerService.ControllerUpdated += GamepadUpdated;
        }

        /// <summary>
        /// Unsubscribes to controller update events
        /// </summary>
        public void Stop()
        {
            App.ControllerService.ControllerUpdated -= GamepadUpdated;
        }

        /// <summary>
        /// Changes the active input mode
        /// </summary>
        public void SetMode(InputMode mode)
        {
            if (_mode == mode)
                return;

            if (_mode == InputMode.Flight)
                ResetFlightCommandUI();

            _mode = mode;
        }

        private void GamepadUpdated(ControllerStateModel state)
        {
            Process(state.Buttons, state.Axes);
        }


        /// <summary>
        /// Processes input based on the current InputMode
        /// </summary>
        public void Process(bool[] buttons, double[] axes)
        {
            var current = _mappingService.ProcessInput(buttons, axes);

            switch (_mode)
            {
                case InputMode.Flight:
                    HandleFlight(current); break;
                case InputMode.Menu:
                    HandleMenuNavigation(current); break;
                case InputMode.SimWarning:
                    HandleCommand(ApplicationControls.Select, current, () => _simulatorWarningViewModel.Select()); break;
            }
        }


        /// <summary>
        /// Executes an action using edge detection
        /// </summary>
        private void HandleCommand(ApplicationControls control, Dictionary<ApplicationControls, double> current,
            Action action, Action<bool> setActive = null)
        {
            current.TryGetValue(control, out var value);
            bool isPressed = value > PressThreshold;

            setActive?.Invoke(isPressed);

            _previousState.TryGetValue(control, out var wasPressed);
            if (isPressed && !wasPressed)
                action();

            _previousState[control] = isPressed;
        }


        /// <summary>
        /// Resets flight command state when leaving flight mode
        /// </summary>
        private void ResetFlightCommandUI()
        {
            _flightCommand.TakeoffActive = false;
            _flightCommand.LandActive = false;
            _flightCommand.StopActive = false;

            _previousState[ApplicationControls.Takeoff] = false;
            _previousState[ApplicationControls.Land] = false;
            _previousState[ApplicationControls.Stop] = false;
        }


        /// <summary>
        /// Handles controller inputs while in flight mode
        /// </summary>
        private void HandleFlight(Dictionary<ApplicationControls, double> current)
        {
            HandleCommand(ApplicationControls.Takeoff, current, () => { _ = _flightControllerService.TakeoffAsync(); },
                value => _flightCommand.TakeoffActive = value);

            HandleCommand(ApplicationControls.Land, current, () => { _ = _flightControllerService.LandAsync(); },
                value => _flightCommand.LandActive = value);

            HandleCommand(ApplicationControls.Stop, current, () => { _ = _flightControllerService.StopAsync(); },
                value => _flightCommand.StopActive = value);

            HandleMenuToggle(current);
            ProcessVirtualJoystick(current);
        }

        /// <summary>
        /// Sends virtual stick commands to the aircraft
        /// </summary>
        private void ProcessVirtualJoystick(Dictionary<ApplicationControls, double> current)
        {
            float throttle = CombineAxis(current, ApplicationControls.ThrottleUp, ApplicationControls.ThrottleDown);
            float yaw = CombineAxis(current, ApplicationControls.YawRight, ApplicationControls.YawLeft);
            float pitch = CombineAxis(current, ApplicationControls.PitchForward, ApplicationControls.PitchBackward);
            float roll = CombineAxis(current, ApplicationControls.RollRight, ApplicationControls.RollLeft);

            _flightControllerService.VirtualStickCommandAsync(throttle, yaw, pitch, roll);
        }

        /// <summary>
        /// Combines two opposing axis inputs into a single output value
        /// </summary>
        private float CombineAxis(Dictionary<ApplicationControls, double> current, ApplicationControls positive,
                                    ApplicationControls negative)
        {
            current.TryGetValue(positive, out var pos);
            current.TryGetValue(negative, out var neg);

            //Dead zones
            if (Math.Abs(pos) < DeadZoneThreshold)
                pos = 0;
            if (Math.Abs(neg) < DeadZoneThreshold)
                neg = 0;


            if (Math.Abs(pos) > Math.Abs(neg))
                return (float)pos;
            if (Math.Abs(neg) > Math.Abs(pos))
                return -(float)neg;

            return 0f;
        }


        //Menu control processing
        private void HandleMenuToggle(Dictionary<ApplicationControls, double> current)
        {
            HandleCommand(ApplicationControls.Menu, current, () => _menuViewModel.MenuActive = !_menuViewModel.MenuActive);
        }

        /// <summary>
        /// Handles navigation controls while in the menu
        /// </summary>
        private void HandleMenuNavigation(Dictionary<ApplicationControls, double> current)
        {
            HandleCommand(ApplicationControls.ThrottleUp, current, () => _menuViewModel.MoveUp());
            HandleCommand(ApplicationControls.ThrottleDown, current, () => _menuViewModel.MoveDown());
            HandleCommand(ApplicationControls.Select, current, () => _menuViewModel.Select());

            HandleMenuToggle(current);
        }
    }
}
