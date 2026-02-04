namespace UAV_Assistive_Operation.Models
{
    public class ControlRulesModel
    {
        public bool AllowButton { get; set; }
        public bool AllowSwitch { get; set; }

        public bool AllowBipolarAxis { get; set; }
        public bool AllowUnipolarAxis { get; set; }

        public bool AutoCreateOpposite { get; set; }
    }
}
