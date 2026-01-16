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

        //Input events for services to subscribe to
        public event Action<GamepadReading> GamepadUpdated;
        public event Action<bool[], GameControllerSwitchPosition[], double[]> RawControllerUpdated;

        //Connection state events for services to subscribe to
        public event Action<Gamepad> GamepadConnected;
        public event Action GamepadDisconnected;
        public event Action<RawGameController> RawControllerConnected;
        public event Action RawControllerDisconnected;

        /// <summary>
        /// Subscribes to controller add/remove events
        /// </summary>
        public void Initialize()
        {
            Gamepad.GamepadAdded += GamepadAdded;
            Gamepad.GamepadRemoved += GamepadRemoved;

            RawGameController.RawGameControllerAdded += RawControllerAdded;
            RawGameController.RawGameControllerRemoved += RawControllerRemoved;
        }

        /// <summary>
        /// Begins polling input at 50Hz
        /// </summary>
        public void Start()
        {
            if (_inputTimer != null)
                return;

            _inputTimer = new DispatcherTimer();
            _inputTimer.Interval = TimeSpan.FromMilliseconds(20);
            _inputTimer.Tick += Instance_InputUpdate;
            _inputTimer.Start();
        }

        //Gamepad added/removed
        private void GamepadAdded(object sender, Gamepad gamepad)
        {
            _gamepad = gamepad;
            GamepadConnected?.Invoke(gamepad);
        }

        private void GamepadRemoved(object sender, Gamepad gamepad)
        {
            if (_gamepad != gamepad)
                return;
            
            _gamepad = null;
            GamepadDisconnected?.Invoke();
        }

        //Raw controller added/removed
        private void RawControllerAdded(object sender, RawGameController controller)
        {
            _rawGameController = controller;
            RawControllerConnected?.Invoke(controller);
        }

        private void RawControllerRemoved(object sender, RawGameController controller)
        {
            if (_rawGameController != controller)
                return;

            _rawGameController = null;
            RawControllerDisconnected?.Invoke();
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
