using System.ComponentModel.DataAnnotations;

namespace UAV_Assistive_Operation.Enums
{
    public enum ApplicationControls
    {
        [Display(Name = "Throttle up")]
        ThrottleUp,
        [Display(Name = "Throttle down")]
        ThrottleDown,
        [Display(Name = "Yaw left")]
        YawLeft,
        [Display(Name = "Yaw right")]
        YawRight,
        [Display(Name = "Pitch forward")]
        PitchForward,
        [Display(Name = "Pitch backwards")]
        PitchBackward,
        [Display(Name = "Roll left")]
        RollLeft,
        [Display(Name = "Roll right")]
        RollRight,
        [Display(Name = "Takeoff")]
        Takeoff,
        [Display(Name = "Land")]
        Land,
        [Display(Name = "Stop")]
        Stop,
        [Display(Name = "Menu")]
        Menu,
        [Display(Name = "Select")]
        Select
    }
}
