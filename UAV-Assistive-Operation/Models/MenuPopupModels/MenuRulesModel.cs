namespace UAV_Assistive_Operation.Models
{
    public class MenuRulesModel
    {
        public bool IsToggleButton { get; set; }

        //Used for normal buttons
        public string ButtonText { get; set; }

        //Used for toggle buttons
        public string EnabledText { get; set; }
        public string DisabledText { get; set; }
    }
}
