using System.Collections.Generic;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;

namespace UAV_Assistive_Operation.Configuration
{
    public static class ControlRemappingRules
    {
        public static readonly Dictionary<ApplicationControls, ControlRulesModel> Rules = 
            new Dictionary<ApplicationControls, ControlRulesModel>
        {
            [ApplicationControls.ThrottleUp] = new ControlRulesModel
            {
                AllowButton = true,
                AllowSwitch = false,

                AllowBipolarAxis = true,
                AllowUnipolarAxis = true,

                AutoCreateOpposite = true,
            },

            [ApplicationControls.ThrottleDown] = new ControlRulesModel
            {                
                AllowButton = true,
                AllowSwitch = false,

                AllowBipolarAxis = true,
                AllowUnipolarAxis = true,

                AutoCreateOpposite = false,
            },

            [ApplicationControls.YawLeft] = new ControlRulesModel
            {                
                AllowButton = true,
                AllowSwitch = false,

                AllowBipolarAxis = true,
                AllowUnipolarAxis = false,

                AutoCreateOpposite = true,
            },

            [ApplicationControls.YawRight] = new ControlRulesModel
            {                
                AllowButton = true,
                AllowSwitch = false,

                AllowBipolarAxis = true,
                AllowUnipolarAxis = false,

                AutoCreateOpposite = false,
            },

            [ApplicationControls.PitchForward] = new ControlRulesModel
            {                
                AllowButton = true,
                AllowSwitch = false,

                AllowBipolarAxis = true,
                AllowUnipolarAxis = true,

                AutoCreateOpposite = true,
            },

            [ApplicationControls.PitchBackward] = new ControlRulesModel
            {                
                AllowButton = true,
                AllowSwitch = false,

                AllowBipolarAxis = true,
                AllowUnipolarAxis = true,

                AutoCreateOpposite = false,
            },

            [ApplicationControls.RollLeft] = new ControlRulesModel
            {                
                AllowButton = true,
                AllowSwitch = false,

                AllowBipolarAxis = true,
                AllowUnipolarAxis = false,

                AutoCreateOpposite = true,
            },

            [ApplicationControls.RollRight] = new ControlRulesModel
            {                
                AllowButton = true,
                AllowSwitch = false,

                AllowBipolarAxis = true,
                AllowUnipolarAxis = false,

                AutoCreateOpposite = false,
            },

            [ApplicationControls.Takeoff] = new ControlRulesModel
            {                
                AllowButton = true,
                AllowSwitch = true,

                AllowBipolarAxis = false,
                AllowUnipolarAxis = false,

                AutoCreateOpposite = false,
            },

            [ApplicationControls.Land] = new ControlRulesModel
            {                
                AllowButton = true,
                AllowSwitch = true,

                AllowBipolarAxis = false,
                AllowUnipolarAxis = false,

                AutoCreateOpposite = false,
            },

            [ApplicationControls.Stop] = new ControlRulesModel
            {                
                AllowButton = true,
                AllowSwitch = true,

                AllowBipolarAxis = false,
                AllowUnipolarAxis = false,

                AutoCreateOpposite = false,
            },

            [ApplicationControls.Menu] = new ControlRulesModel
            {                
                AllowButton = true,
                AllowSwitch = false,

                AllowBipolarAxis = false,
                AllowUnipolarAxis = false,

                AutoCreateOpposite = false,
            },

            [ApplicationControls.Select] = new ControlRulesModel
            {                
                AllowButton = true,
                AllowSwitch = false,

                AllowBipolarAxis = false,
                AllowUnipolarAxis = false,

                AutoCreateOpposite = false,
            }
        };
    }
}
