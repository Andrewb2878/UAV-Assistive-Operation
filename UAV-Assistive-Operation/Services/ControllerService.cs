using System;
using Windows.Gaming.Input;
using Windows.UI.Xaml;

namespace UAV_Assistive_Operation.Services
{
    public class ControllerService
    {
        private Gamepad _gamepad;
        private RawGameController _rawGameController;
        private DispatcherTimer _inputTimer;

        //Events for other services to subscribe to
        public event Action<GamepadReading> GamepadUpdated;
        public event Action<bool[], GameControllerSwitchPosition[], double[]> RawControllerUpdated;

        //Listening for controllers and input polling
        public void Start()
        {
            Gamepad.GamepadAdded += (sender, gamepad) => _gamepad = gamepad;
            Gamepad.GamepadRemoved += (sender, gamepad) =>
            {
                if (_gamepad == gamepad)
                {
                    _gamepad = null;
                }
            };

            RawGameController.RawGameControllerAdded += (sender, rawController) => _rawGameController = rawController;
            RawGameController.RawGameControllerRemoved += (sender, rawController) =>
            {
                if (_rawGameController == rawController)
                {
                    _rawGameController = null;
                }
            };

            _inputTimer = new DispatcherTimer();
            _inputTimer.Interval = TimeSpan.FromMilliseconds(20);
            _inputTimer.Tick += Instance_InputUpdate;
            _inputTimer.Start();
        }

        private void Instance_InputUpdate(object sender, object gamepad)
        {
            if (_gamepad != null)
            {
                GamepadUpdated?.Invoke(_gamepad.GetCurrentReading());
            }

            if (_rawGameController != null)
            {
                var buttons = new bool[_rawGameController.ButtonCount];
                var switches = new GameControllerSwitchPosition[_rawGameController.SwitchCount];
                var axes = new double[_rawGameController.AxisCount];

                _rawGameController.GetCurrentReading(buttons, switches, axes);
                RawControllerUpdated?.Invoke(buttons, switches, axes);
            }
        }
    }
}
