using System;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;
using Windows.Gaming.Input;

namespace UAV_Assistive_Operation.Services
{
    public class ControllerRemapInputService
    {
        private readonly ControllerMappingService _mappingService;

        private const int AssignmentCooldownMs = 500;
        private const double TriggerThreshold = 0.7;
        private const double AxisDeadZone = 0.5;

        private bool _listeningForRemap = false;
        private ControllerStateModel _lastState;
        private DateTime _lastAssignmentTime = DateTime.MinValue;

        public event Action<InputBindingModel> InputDetected;


        //Used to manage when to remap controls 
        public void Start()
        {
            _listeningForRemap = true;
            _lastState = null;
            _lastAssignmentTime = DateTime.MinValue;

            App.ControllerService.ControllerUpdated += GamepadUpdated;
        }

        public void Stop() 
        {
            //_mappingService.DisplayBindings();            - Used to check the stored control bindings
            _listeningForRemap = false;
            App.ControllerService.ControllerUpdated -= GamepadUpdated;
        }


        public ControllerRemapInputService(ControllerMappingService mappingService)
        {
            _mappingService = mappingService;
        }


        //Controller methods
        private void GamepadUpdated(ControllerStateModel state)
        {
            if (!_listeningForRemap)
                return;

            if  (_lastState == null)
            {
                _lastState = state;
                return;
            }

            //Cooldown timer
            if ((DateTime.Now - _lastAssignmentTime).TotalMilliseconds < AssignmentCooldownMs)
            {
                _lastState = state;
                return;
            }

            for (int index = 0; index < state.Buttons.Length; index++)
            {
                if (state.Buttons[index] && !_lastState.Buttons[index])
                    DetectedInput(new InputBindingModel
                    {
                        Type = InputTypes.Button,
                        Index = index,
                    });
            }

            for (int index = 0; index < state.Axes.Length; index++)
            {
                double current = state.Axes[index];
                double last = _lastState.Axes[index];

                if (index < 2)
                {
                    if (current > TriggerThreshold && last < 0.1)
                    {
                        DetectedInput(new InputBindingModel
                        {
                            Type = InputTypes.Axis,
                            Index = index,
                            Polarity = AxisPolarity.Unipolar,
                            Direction = 1
                        });
                    }
                }
                else
                {
                    if (Math.Abs(current - last) > AxisDeadZone)
                    {
                        DetectedInput(new InputBindingModel
                        {
                            Type = InputTypes.Axis,
                            Index = index,
                            Polarity = AxisPolarity.Bipolar,
                            Direction = current >= 0 ? 1 : -1,
                        });
                    }
                }
            }
            _lastState = state;
        }

        private void DetectedInput(InputBindingModel binding)
        {
            _lastAssignmentTime = DateTime.Now;
            InputDetected?.Invoke(binding);
        }
    }
}
