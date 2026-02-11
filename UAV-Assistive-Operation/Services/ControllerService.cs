using System;
using UAV_Assistive_Operation.Models;
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
        public event Action<ControllerStateModel> ControllerUpdated;

        //Connection state events for services to subscribe to
        public event Action<Gamepad> GamepadConnected;
        public event Action GamepadDisconnected;

        //Defining the list of available buttons
        private static readonly GamepadButtons[] ButtonsToTrack =
        {
            GamepadButtons.A, GamepadButtons.B, GamepadButtons.X, GamepadButtons.Y,
            GamepadButtons.LeftShoulder, GamepadButtons.RightShoulder,
            GamepadButtons.DPadLeft, GamepadButtons.DPadUp, GamepadButtons.DPadRight, GamepadButtons.DPadDown,
            GamepadButtons.View, GamepadButtons.Menu
        };


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
                var reading = _gamepad.GetCurrentReading();

                //Mapping buttons
                bool[] buttonStates = new bool[ButtonsToTrack.Length];
                for (int index = 0; index < ButtonsToTrack.Length; index++)
                    buttonStates[index] = reading.Buttons.HasFlag(ButtonsToTrack[index]);

                //Mapping axes
                double[] axisStates = new double[]
                {
                    reading.LeftTrigger,
                    reading.RightTrigger,
                    reading.LeftThumbstickX,
                    reading.LeftThumbstickY,
                    reading.RightThumbstickX,
                    reading.RightThumbstickY,
                };

                ControllerUpdated?.Invoke(new ControllerStateModel
                {
                    Buttons = buttonStates,
                    Axes = axisStates,
                    RawReading = reading,
                });
            }
        } 
    }
}
