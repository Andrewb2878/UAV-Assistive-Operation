using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;
using Windows.UI.Xaml.Controls;

namespace UAV_Assistive_Operation.Services
{
    public class ControllerProcessingService
    {
        private readonly ControllerMappingService _mappingService;
        private readonly DJIFlightControllerService _flightControllerService;
        private readonly FlightCommandViewModel _flightCommand;

        private readonly Dictionary<ApplicationControls, bool> _previousState = 
            new Dictionary<ApplicationControls, bool>();

        private const double PressThreshold = 0.8;


        public ControllerProcessingService(ControllerMappingService mappingService, 
            DJIFlightControllerService flightControllerService, FlightCommandViewModel flightCommand)
        {
            _mappingService = mappingService;
            _flightControllerService = flightControllerService;
            _flightCommand = flightCommand;
        }

        public void Start()
        {
            App.ControllerService.ControllerUpdated += GamepadUpdated;
        }

        public void Stop()
        {
            App.ControllerService.ControllerUpdated -= GamepadUpdated;
        }

        private void GamepadUpdated(ControllerStateModel state)
        {
            Process(state.Buttons, state.Axes);
        }


        public void Process(bool[] buttons, double[] axes)
        {
            var current = _mappingService.ProcessInput(buttons, axes);

            HandleCommand(ApplicationControls.Takeoff, current,
                value => _flightCommand.TakeoffActive = value,
                () => _flightControllerService.TakeoffAsync());

            HandleCommand(ApplicationControls.Land, current,
                value => _flightCommand.LandActive = value,
                () => _flightControllerService.LandAsync());

            HandleCommand(ApplicationControls.Stop, current,
                value => _flightCommand.StopActive = value,
                () => _flightControllerService.StopAsync());

            HandleMenu(current);
            ProcessVirtualJoystick(current);
        }

        private void HandleCommand(ApplicationControls control, Dictionary<ApplicationControls, double> current,
            Action<bool> setActive, Func<Task> executeCommand)
        {
            current.TryGetValue(control, out var value);
            bool isPressed = value > PressThreshold;
            setActive(isPressed);

            _previousState.TryGetValue(control, out var wasPressed);
            if (isPressed && !wasPressed)
                _ = executeCommand();

            _previousState[control] = isPressed;
        }

        private void HandleMenu(Dictionary<ApplicationControls, double> current)
        {
            current.TryGetValue(ApplicationControls.Menu, out var value);
            bool pressed = value > PressThreshold;

            _previousState.TryGetValue(ApplicationControls.Menu, out var wasPressed);
            if (pressed && !wasPressed)
                _flightCommand.MenuActive = !_flightCommand.MenuActive;

            _previousState[ApplicationControls.Menu] = pressed;
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
            if (Math.Abs(pos) < 0.07)
                pos = 0;
            if (Math.Abs(neg) < 0.07)
                neg = 0;


            if (Math.Abs(pos) > Math.Abs(neg))
                return (float)pos;
            if (Math.Abs(neg) > Math.Abs(pos))
                return -(float)neg;

            return 0f;
        }
    }
}
