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
        private DateTime _lastAssignmentTime = DateTime.MinValue;
        private GamepadReading? _lastReading;

        public event Action<InputBindingModel> InputDetected;

        private static readonly GamepadButtons[] ButtonsToTrack =
        {
            GamepadButtons.A, GamepadButtons.B, GamepadButtons.X, GamepadButtons.Y,
            GamepadButtons.LeftShoulder, GamepadButtons.RightShoulder,
            GamepadButtons.DPadLeft, GamepadButtons.DPadUp, GamepadButtons.DPadRight, GamepadButtons.DPadDown,
            GamepadButtons.View, GamepadButtons.Menu
        };

        //Used to manage when to remap controls 
        public void Start()
        {
            _listeningForRemap = true;
            _lastReading = null;
            _lastAssignmentTime = DateTime.MinValue;

            App.ControllerService.GamepadUpdated += GamepadUpdated;
        }

        public void Stop() 
        {
            //_mappingService.DisplayBindings();            - Used to check the stored control bindings
            _listeningForRemap = false;
            App.ControllerService.GamepadUpdated -= GamepadUpdated;
        }


        public ControllerRemapInputService(ControllerMappingService mappingService)
        {
            _mappingService = mappingService;
        }


        //Controller methods
        private void GamepadUpdated(GamepadReading reading)
        {
            if (!_listeningForRemap)
                return;

            //Cooldown timer
            if ((DateTime.Now - _lastAssignmentTime).TotalMilliseconds < AssignmentCooldownMs)
            {
                _lastReading = reading;
                return;
            }

            if (_lastReading.HasValue)
            {
                var last = _lastReading.Value;

                //Buttons
                for (int index = 0; index < ButtonsToTrack.Length; index++)
                {
                    DetectButton(ButtonsToTrack[index], reading, last, index);
                }

                //Triggers
                DetectTrigger(reading.LeftTrigger, last.LeftTrigger, 0);
                DetectTrigger(reading.RightTrigger, last.RightTrigger, 1);

                //Joysticks
                DetectAxis(reading.LeftThumbstickX, last.LeftThumbstickX, 2);
                DetectAxis(reading.LeftThumbstickY, last.LeftThumbstickY, 3);
                DetectAxis(reading.RightThumbstickX, last.RightThumbstickX, 4);
                DetectAxis(reading.RightThumbstickY, last.RightThumbstickY, 5);
            }
            _lastReading = reading;
        }

        private void DetectButton(GamepadButtons button, GamepadReading current, GamepadReading last, int index)
        {
            if (current.Buttons.HasFlag(button) && !last.Buttons.HasFlag(button))
            {
                DetectedInput(new InputBindingModel
                {
                    Type = InputTypes.Button,
                    Index = index
                });
            }
        }

        private void DetectTrigger(double current, double last, int index)
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

        private void DetectAxis(double current, double last, int index)
        {
            if (Math.Abs(current - last) > AxisDeadZone)
            {
                DetectedInput(new InputBindingModel
                {
                    Type = InputTypes.Axis,
                    Index = index,
                    Polarity = AxisPolarity.Bipolar,
                    Direction = current >= 0 ? 1 : -1
                });
            }
        }

        private void DetectedInput(InputBindingModel binding)
        {
            _lastAssignmentTime = DateTime.Now;
            InputDetected?.Invoke(binding);
        }
    }
}
