using System;
using System.Collections.Generic;
using UAV_Assistive_Operation.Configuration;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Helpers;
using UAV_Assistive_Operation.Models;
using Windows.Gaming.Input;

namespace UAV_Assistive_Operation.Services
{
    public class ControllerRemappingService
    {
        private readonly Dictionary<ApplicationControls, InputBindingModel> _binding = 
            new Dictionary<ApplicationControls, InputBindingModel>();


        //Bool value if all controls are assigned
        public bool IsFullyRemapped => _binding.Count == Enum.GetValues(typeof(ApplicationControls)).Length;


        //Uses _binding to generate real-time control values
        public Dictionary<ApplicationControls, double> ProcessInput(bool[] buttons, GameControllerSwitchPosition[] switches, double[] axes)
        {
            var output = new Dictionary<ApplicationControls, double>();

            foreach (var pair in _binding)
            {
                var control = pair.Key;
                var binding = pair.Value;

                double value = 0.0;

                switch (binding.Type)
                {
                    case InputTypes.Button:
                        value = buttons[binding.Index] ? 1.0 : 0.0; break;
                    case InputTypes.Switch:
                        value = switches[binding.Index] != GameControllerSwitchPosition.Center ? 1.0 : 0.0; break;
                    case InputTypes.Axis:
                        double raw = axes[binding.Index];
                        if (binding.Polarity == AxisPolarity.Bipolar)
                        {
                            double directed = raw * binding.Direction;
                            value = Math.Max(0, directed);
                        }
                        else
                        {
                            value = Math.Max(0, raw);
                        }
                        break;
                }

                value = Math.Clamp(value, 0.0, 1.0);
                output[control] = value;
            }
            return output;
        }


        //Configurating controller inputs
        public bool TryAssignBinding(ApplicationControls control, InputBindingModel binding, out string error)
        {
            error = null;

            //Ensures against multiple controls assigned to the same input
            foreach (var existingEntry in _binding)
            {
                if (existingEntry.Key == control) 
                    continue;
                
                var existing = existingEntry.Value;
                if (existing.Type == binding.Type && existing.Index == binding.Index)
                {
                    error = $"Input already used by {existingEntry.Key.GetDisplayName()}";
                    return false;
                }
            }

            var rule = ControlRemappingRules.Rules[control];
                        
            if (binding.Type == InputTypes.Button && !rule.AllowButton)
                error = "Button input not allowed";
            if (binding.Type == InputTypes.Switch && !rule.AllowSwitch)
                error = "Switch input not allowed";
            if (binding.Type == InputTypes.Axis && !rule.AllowBipolarAxis)
                error = "Axis input not allowed";
            if (binding.Polarity == AxisPolarity.Unipolar && !rule.AllowUnipolarAxis)
                error = "Trigger style axis input not allowed";
            if (binding.Polarity == AxisPolarity.Bipolar && !rule.AllowBipolarAxis)
                error = "Stick style axis input not allowed";

            if (error != null)
                return false;

            _binding[control] = binding;
            if (rule.AutoCreateOpposite && (binding.Type == InputTypes.Axis) &&
                (binding.Polarity == AxisPolarity.Bipolar))
            {
                AssignOpposite(control, binding);
            }

            return true;
        }

        //Automatically assigns opposite controls for allowed axis inputs
        private void AssignOpposite(ApplicationControls control, InputBindingModel binding)
        {
            ApplicationControls? opposite;
            switch (control)
            {
                case ApplicationControls.ThrottleUp:
                    opposite = ApplicationControls.ThrottleDown; break;
                case ApplicationControls.YawLeft:
                    opposite = ApplicationControls.YawRight; break;
                case ApplicationControls.PitchForward:
                    opposite = ApplicationControls.PitchBackward; break;
                case ApplicationControls.RollLeft:
                    opposite = ApplicationControls.RollRight; break;
                default:
                    opposite = null; break;
            }

            if (!opposite.HasValue)
                return;

            _binding[opposite.Value] = new InputBindingModel
            {
                Type = InputTypes.Axis,
                Index = binding.Index,
                Direction = -binding.Direction,
                Polarity = binding.Polarity,
            };
        }
    }
}
