using System.ComponentModel.DataAnnotations;

namespace UAV_Assistive_Operation.Enums
{
    public enum MenuOptions
    {
        [Display(Name = "Reconfigure controller")]
        ReconfigController,
        [Display(Name = "Simulator mode")]
        simulatorMode
    }
}
