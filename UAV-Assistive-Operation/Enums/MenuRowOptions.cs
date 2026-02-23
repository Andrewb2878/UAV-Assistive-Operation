using System.ComponentModel.DataAnnotations;

namespace UAV_Assistive_Operation.Enums
{
    public enum MenuRowOptions
    {
        [Display(Name = "Reconfigure controller")]
        ReconfigController,
        [Display(Name = "Simulator mode")]
        simulatorMode
    }
}
