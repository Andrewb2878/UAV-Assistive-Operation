using UAV_Assistive_Operation.Enums;

namespace UAV_Assistive_Operation.Models
{
    public class InputBindingModel
    {
        public InputTypes Type { get; set; }
        public int Index { get; set; }

        public AxisPolarity? Polarity { get; set; }
        public int Direction { get; set; }        
    }
}
