using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;

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
    }
}
