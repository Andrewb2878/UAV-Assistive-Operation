using System;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;

namespace UAV_Assistive_Operation.Services
{
    /// <summary>
    /// Listens for controller input while in remapping mode and detects which
    /// button or axis the user triggers for control assignment
    /// </summary>
    public class ControllerRemapInputService
    {
        private readonly ControllerMappingService _mappingService;

        //Prevents multiple assignments from a single input
        private const int AssignmentCooldownMs = 500;
        //Trigger must be pressed for at least 70% to be mapped
        private const double TriggerThreshold = 0.7;
        //Prevents stick drift or accidental touch from assigning axis inputs
        private const double AxisDeadZone = 0.5;

        private bool _listeningForRemap = false;
        private ControllerStateModel _lastState;
        private DateTime _lastAssignmentTime = DateTime.MinValue;

        public event Action<InputBindingModel> InputDetected;


        /// <summary>
        /// Starts listening for controller inputs to perform remapping
        /// </summary>
        public void Start()
        {
            _listeningForRemap = true;
            _lastState = null;
            _lastAssignmentTime = DateTime.MinValue;

            App.ControllerService.ControllerUpdated += GamepadUpdated;
        }

        /// <summary>
        /// Stops listening for controller inputs and unsubscribes from controller updates
        /// </summary>
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


        /// <summary>
        /// Called when the controller state updates, used to detect inputs
        /// </summary>
        private void GamepadUpdated(ControllerStateModel state)
        {
            if (!_listeningForRemap)
                return;

            if  (_lastState == null)
            {
                _lastState = state;
                return;
            }

            //Cooldown between control assignments
            if ((DateTime.Now - _lastAssignmentTime).TotalMilliseconds < AssignmentCooldownMs)
            {
                _lastState = state;
                return;
            }

            //Detect new button inputs
            for (int index = 0; index < state.Buttons.Length; index++)
            {
                if (state.Buttons[index] && !_lastState.Buttons[index])
                    DetectedInput(new InputBindingModel
                    {
                        Type = InputTypes.Button,
                        Index = index,
                    });
            }

            //Detect new axis inputs
            for (int index = 0; index < state.Axes.Length; index++)
            {
                double current = state.Axes[index];
                double last = _lastState.Axes[index];

                //First two axes are treated as unipolar axis inputs
                if (index < 2)
                {
                    if (current > TriggerThreshold)
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
                //Remaining axes are treated as bipolar inputs
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

        /// <summary>
        /// Handles detected input, updating the cooldown timer and triggering
        /// new InputDetected event
        /// </summary>
        private void DetectedInput(InputBindingModel binding)
        {
            _lastAssignmentTime = DateTime.Now;
            InputDetected?.Invoke(binding);
        }
    }
}
