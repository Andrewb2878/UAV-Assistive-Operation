using System;
using System.Collections.Generic;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;
using Windows.Gaming.Input;

namespace UAV_Assistive_Operation.Services
{
    public class ControllerProcessingService
    {
        private readonly ControllerMappingService _mappingService;
        private readonly FlightCommandViewModel _flightCommand;

        private const double PressThreshold = 0.8;


        public ControllerProcessingService(ControllerMappingService mappingService, FlightCommandViewModel flightCommand)
        {
            _mappingService = mappingService;
            _flightCommand = flightCommand;
        }

        public void Start()
        {
            App.ControllerService.GamepadUpdated += GamepadUpdated;
        }

        public void Stop()
        {
            App.ControllerService.GamepadUpdated -= GamepadUpdated;
        }

        private void GamepadUpdated(GamepadReading reading)
        {
            bool[] buttons =
            {
                reading.Buttons.HasFlag(GamepadButtons.A),
                reading.Buttons.HasFlag(GamepadButtons.B),
                reading.Buttons.HasFlag(GamepadButtons.X),
                reading.Buttons.HasFlag(GamepadButtons.Y),
                reading.Buttons.HasFlag(GamepadButtons.LeftShoulder),
                reading.Buttons.HasFlag(GamepadButtons.RightShoulder),
                reading.Buttons.HasFlag(GamepadButtons.DPadLeft),
                reading.Buttons.HasFlag(GamepadButtons.DPadUp),
                reading.Buttons.HasFlag(GamepadButtons.DPadRight),
                reading.Buttons.HasFlag(GamepadButtons.DPadDown),
                reading.Buttons.HasFlag(GamepadButtons.View),
                reading.Buttons.HasFlag(GamepadButtons.Menu),
            };

            double[] axes =
            {
                reading.LeftTrigger,
                reading.RightTrigger,
                reading.LeftThumbstickX,
                reading.LeftThumbstickY,
                reading.RightThumbstickX,
                reading.RightThumbstickY,
            };

            Process(buttons, axes);
        }


        public void Process(bool[] buttons, double[] axes)
        {
            var current = _mappingService.ProcessInput(buttons, axes);

            HandleCommand(ApplicationControls.Takeoff, current, value => _flightCommand.TakeoffActive = value);
            HandleCommand(ApplicationControls.Land, current, value => _flightCommand.LandActive = value);
            HandleCommand(ApplicationControls.Stop, current, value => _flightCommand.StopActive = value);
        }

        private void HandleCommand(ApplicationControls control, Dictionary<ApplicationControls, double> current,
            Action<bool> setActive)
        {
            current.TryGetValue(control, out var value);
            setActive(value > PressThreshold);
        }
    }
}
