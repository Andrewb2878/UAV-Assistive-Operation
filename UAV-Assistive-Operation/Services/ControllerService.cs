using System;
using Windows.Gaming.Input;
using Windows.UI.Xaml;

namespace UAV_Assistive_Operation.Services
{
    public class ControllerService
    {
        public bool IsControllerConnected { get; private set; }


        private Gamepad _gamepad;
        private DispatcherTimer _inputTimer;

        //Input events for services to subscribe to
        public event Action<GamepadReading> GamepadUpdated;

        //Connection state events for services to subscribe to
        public event Action<Gamepad> GamepadConnected;
        public event Action GamepadDisconnected;


        public void Initialize()
        {
            Gamepad.GamepadAdded += GamepadAdded;
            Gamepad.GamepadRemoved += GamepadRemoved;
        }


        public void Start()
        {
            if (_inputTimer != null)
                return;

            _inputTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(20)
            };
            _inputTimer.Tick += InputUpdate;
            _inputTimer.Start();
        }

        //Gamepad added/removed
        private void GamepadAdded(object sender, Gamepad gamepad)
        {
            _gamepad = gamepad;
            IsControllerConnected = true;
            EventLogService.Instance.Log(Enums.LogEventType.Connection, "Controller connected");
            GamepadConnected?.Invoke(gamepad);
        }

        private void GamepadRemoved(object sender, Gamepad gamepad)
        {
            if (_gamepad != gamepad)
                return;
            
            _gamepad = null;
            IsControllerConnected = false;
            EventLogService.Instance.Log(Enums.LogEventType.Warning, "Controller disconnected");
            GamepadDisconnected?.Invoke();
        }

        private void InputUpdate(object sender, object gamepad)
        {
            if (_gamepad != null)
            {
                GamepadUpdated?.Invoke(_gamepad.GetCurrentReading());
            }
        } 
    }
}
