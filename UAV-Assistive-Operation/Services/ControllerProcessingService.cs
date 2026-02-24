using System;
using System.Collections.Generic;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;

namespace UAV_Assistive_Operation.Services
{
    public class ControllerProcessingService
    {
        //Services
        private readonly ControllerMappingService _mappingService;
        private readonly DJIFlightControllerService _flightControllerService;
        private readonly FlightCommandViewModel _flightCommand;
        private readonly MenuViewModel _menuViewModel;
        private readonly SimulatorWarningViewModel _simulatorWarningViewModel;

        //States
        private InputMode _mode = InputMode.Flight;
        private readonly Dictionary<ApplicationControls, bool> _previousState = 
            new Dictionary<ApplicationControls, bool>();

        //Variables
        private const double PressThreshold = 0.8;
        private const double DeadZoneThreshold = 0.07;


        public ControllerProcessingService(ControllerMappingService mappingService, 
            DJIFlightControllerService flightControllerService, FlightCommandViewModel flightCommand, MenuViewModel menuViewModel,
            SimulatorWarningViewModel simulatorWarningViewModel)
        {
            _mappingService = mappingService;
            _flightControllerService = flightControllerService;
            _flightCommand = flightCommand;
            _menuViewModel = menuViewModel;
            _simulatorWarningViewModel = simulatorWarningViewModel;
        }

        public void Start()
        {
            App.ControllerService.ControllerUpdated += GamepadUpdated;
        }

        public void Stop()
        {
            App.ControllerService.ControllerUpdated -= GamepadUpdated;
        }

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


        //Selects which controls to enable based on InputMode
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


        //Processes control input with edge detection
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


        //Flight control processing
        private void ResetFlightCommandUI()
        {
            _flightCommand.TakeoffActive = false;
            _flightCommand.LandActive = false;
            _flightCommand.StopActive = false;

            _previousState[ApplicationControls.Takeoff] = false;
            _previousState[ApplicationControls.Land] = false;
            _previousState[ApplicationControls.Stop] = false;
        }


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

        private void ProcessVirtualJoystick(Dictionary<ApplicationControls, double> current)
        {
            float throttle = CombineAxis(current, ApplicationControls.ThrottleUp, ApplicationControls.ThrottleDown);
            float yaw = CombineAxis(current, ApplicationControls.YawRight, ApplicationControls.YawLeft);
            float pitch = CombineAxis(current, ApplicationControls.PitchForward, ApplicationControls.PitchBackward);
            float roll = CombineAxis(current, ApplicationControls.RollRight, ApplicationControls.RollLeft);

            _flightControllerService.VirtualStickCommand(throttle, yaw, pitch, roll);
        }

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

        private void HandleMenuNavigation(Dictionary<ApplicationControls, double> current)
        {
            HandleCommand(ApplicationControls.ThrottleUp, current, () => _menuViewModel.MoveUp());
            HandleCommand(ApplicationControls.ThrottleDown, current, () => _menuViewModel.MoveDown());
            HandleCommand(ApplicationControls.Select, current, () => _menuViewModel.Select());

            HandleMenuToggle(current);
        }
    }
}
