using System;
using System.Collections.Generic;
using UAV_Assistive_Operation.Configuration;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Helpers;
using UAV_Assistive_Operation.Models;

namespace UAV_Assistive_Operation.Services
{
    /// <summary>
    /// Converts controller input bindings into application controls
    /// 
    /// Storing active control bindings, validating remapping rules, preventing duplicate input assignment
    /// and automatically generates opposite axis bindings
    /// </summary>
    public class ControllerMappingService
    {
        // Stores the mappings of application controls and their corresponding controller input
        private readonly Dictionary<ApplicationControls, InputBindingModel> _binding = 
            new Dictionary<ApplicationControls, InputBindingModel>();


        //Bool value used to tell if remapping is complete
        public bool IsFullyRemapped => _binding.Count == Enum.GetValues(typeof(ApplicationControls)).Length;

        //Events
        public event Action RemappingStateChanged;


        /// <summary>
        /// Processes controller inputs and converts into application values
        /// </summary>
        /// <param name="buttons">Array of controller button states</param>
        /// <param name="axes">Array of controller axis values</param>
        public Dictionary<ApplicationControls, double> ProcessInput(bool[] buttons, double[] axes)
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


        /// <summary>
        /// Attempts to assign controller inputs based on validation rules, preventing duplicate inputs,
        /// applying control specific remapping rules, optional automatic axis generation
        /// </summary>
        public bool TryAssignBinding(ApplicationControls control, InputBindingModel binding, out string error, out ApplicationControls? autoAssignedControl)
        {
            error = null;
            autoAssignedControl = null;

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

            //Validate remapping rules for current control
            var rule = ControlRemappingRules.Rules[control];
                        
            if (binding.Type == InputTypes.Button && !rule.AllowButton)
                error = "Button input not allowed";
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
                autoAssignedControl = AssignOpposite(control, binding);
            }

            RemappingStateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Creates a UI version of the binding
        /// </summary>
        public string DescribeBinding(InputBindingModel binding)
        {
            switch (binding.Type)
            {
                case InputTypes.Button:
                    return $"Button {binding.Index}";
                case InputTypes.Axis:
                    if (binding.Polarity == AxisPolarity.Unipolar)
                    {
                        return $"Trigger {binding.Index}";
                    }
                    else
                    {
                        return $"Axis {binding.Index} ({(binding.Direction > 0 ? "+" : "-")})";
                    }
                default:
                    return "Unknown";

            };
        }

        /// <summary>
        /// Automatically assigns opposite controls for bipolar axis inputs
        /// </summary>
        private ApplicationControls? AssignOpposite(ApplicationControls control, InputBindingModel binding)
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
                return null;

            _binding[opposite.Value] = new InputBindingModel
            {
                Type = InputTypes.Axis,
                Index = binding.Index,
                Direction = -binding.Direction,
                Polarity = binding.Polarity,
            };

            return opposite;
        }

        /// <summary>
        /// Logs all current bindings for debug use
        /// </summary>
        public void DisplayBindings()
        {
            EventLogService.Instance.Log(LogEventType.Info, "Control bindings:");
            foreach (var input in _binding)
            {
                var key = input.Key;
                var inputType = input.Value.Type;
                var inputIndex = input.Value.Index;
                var inputPolarity = input.Value.Polarity;
                var inputDirection = input.Value.Direction;

                if (inputType == InputTypes.Axis)
                {
                    EventLogService.Instance.Log(LogEventType.Info, $"Control: {key}.. T:{inputType}.." +
                    $" I:{inputIndex}.. P:{inputPolarity}.. D:{inputDirection}");
                }
                else
                {
                    EventLogService.Instance.Log(LogEventType.Info, $"Control: {key}.. T:{inputType}.." +
                    $" I:{inputIndex}");
                }
                
            }
        }

        /// <summary>
        /// Clears existing controller bindings
        /// </summary>
        public void ClearBindings()
        {
            _binding.Clear();
            RemappingStateChanged?.Invoke();
            EventLogService.Instance.Log(LogEventType.System, "Controller mappings cleared");
        }
    }
}
