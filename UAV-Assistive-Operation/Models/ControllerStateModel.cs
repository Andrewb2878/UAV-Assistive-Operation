using Windows.Gaming.Input;

namespace UAV_Assistive_Operation.Models
{
    public class ControllerStateModel
    {
        public bool[] Buttons { get; set; }
        public double[] Axes { get; set; }
        public GamepadReading RawReading { get; set; }
    }
}
