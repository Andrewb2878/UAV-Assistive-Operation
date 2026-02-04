using System;
using Windows.Gaming.Input;
using Windows.UI.Xaml;

namespace UAV_Assistive_Operation.Services
{
    public class ControllerService
    {
        public bool IsControllerConnected { get; private set; }
        public RawGameController RawController { get; private set; }


        private Gamepad _gamepad;
        private DispatcherTimer _inputTimer;

        //Input events for services to subscribe to
        public event Action<GamepadReading> GamepadUpdated;
        public event Action<bool[], GameControllerSwitchPosition[], double[]> RawControllerUpdated;

        //Connection state events for services to subscribe to
        public event Action<Gamepad> GamepadConnected;
        public event Action GamepadDisconnected;
        public event Action<RawGameController> RawControllerConnected;
        public event Action RawControllerDisconnected;


        public void Initialize()
        {
            Gamepad.GamepadAdded += GamepadAdded;
            Gamepad.GamepadRemoved += GamepadRemoved;

            RawGameController.RawGameControllerAdded += RawControllerAdded;
            RawGameController.RawGameControllerRemoved += RawControllerRemoved;
        }


        public void Start()
        {
            if (_inputTimer != null)
                return;

            _inputTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(20)
            };
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
            RawController = controller;
            IsControllerConnected = true;

            EventLogService.Instance.Log(Enums.LogEventType.Connection, "Controller connected");
            RawControllerConnected?.Invoke(controller);
        }

        private void RawControllerRemoved(object sender, RawGameController controller)
        {
            if (RawController != controller)
                return;

            RawController = null;
            IsControllerConnected = false;

            EventLogService.Instance.Log(Enums.LogEventType.Warning, "Controller disconnected");
            RawControllerDisconnected?.Invoke();
        }

        private void Instance_InputUpdate(object sender, object gamepad)
        {
            if (_gamepad != null)
            {
                GamepadUpdated?.Invoke(_gamepad.GetCurrentReading());
            }

            if (RawController != null)
            {
                var buttons = new bool[RawController.ButtonCount];
                var switches = new GameControllerSwitchPosition[RawController.SwitchCount];
                var axes = new double[RawController.AxisCount];

                RawController.GetCurrentReading(buttons, switches, axes);
                RawControllerUpdated?.Invoke(buttons, switches, axes);
            }
        } 
    }
}
