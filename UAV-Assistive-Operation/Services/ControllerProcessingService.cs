using System;
using System.Collections.Generic;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;

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
