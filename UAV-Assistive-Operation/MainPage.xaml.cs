using DJI.WindowsSDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Gaming.Input;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UAV_Assistive_Operation
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Gamepad _gamepad;
        private RawGameController _rawGameController;
        private readonly DispatcherTimer _inputUpdateTimer;


        public MainPage()
        {
            this.InitializeComponent();

            //DJI setup
            DJISDKManager.Instance.SDKRegistrationStateChanged += Instance_SDKRegistrationEvent;
            DJISDKManager.Instance.RegisterApp("7b980d8aa60b87f6b740fd94");


            //Gamepad setup
            Gamepad.GamepadAdded += Instance_GamepadAdded;
            Gamepad.GamepadRemoved += Instance_GamepadRemoved;
            //Handles non-standard peripherals or additional inputs
            RawGameController.RawGameControllerAdded += Instance_RawControllerAdded;
            RawGameController.RawGameControllerRemoved += Instance_RawControllerRemoved;

            if (Gamepad.Gamepads.Count > 0)
            {
                _gamepad = Gamepad.Gamepads[0];
            }

            _inputUpdateTimer = new DispatcherTimer();
            _inputUpdateTimer.Interval = TimeSpan.FromMilliseconds(20);
            _inputUpdateTimer.Tick += Instance_InputUpdate;
            _inputUpdateTimer.Start();
        }

        private async void Instance_SDKRegistrationEvent(SDKRegistrationState state, SDKError resultCode)
        {
            if (resultCode == SDKError.NO_ERROR)
            {
                System.Diagnostics.Debug.WriteLine("Register app successfully.");

                //The product connection state will be updated when it changes here.
                DJISDKManager.Instance.ComponentManager.GetProductHandler(0).ProductTypeChanged += async delegate (object sender, ProductTypeMsg? value)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        if (value != null && value?.value != ProductType.UNRECOGNIZED)
                        {
                            System.Diagnostics.Debug.WriteLine("The Aircraft is connected now.");
                            //Load/display pages according to the aircarft connection state here.
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("The Aircraft is disconnected now.");
                            //Hide pages according to the aircraft connection state here, or show connection tips to the users.

                        }
                    });
                };

                //If you want to get the latest product connection state manually, you can use the following code.
                var productType = (await DJISDKManager.Instance.ComponentManager.GetProductHandler(0).GetProductTypeAsync()).value;
                if (productType != null && productType?.value != ProductType.UNRECOGNIZED)
                {
                    System.Diagnostics.Debug.WriteLine("The Aircarft is connected now.");
                    //Load/display your pages according to the aircraft connection state here.
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Resgister SDK failed, the error is: ");
                System.Diagnostics.Debug.WriteLine(resultCode.ToString());
            }
        }


        private void Instance_GamepadAdded(object sender, Gamepad gamepad)
        {
            _gamepad = gamepad;
            System.Diagnostics.Debug.WriteLine("Gamepad now connected.");
        }

        private void Instance_GamepadRemoved(object sender, Gamepad gamepad)
        {
            if (_gamepad == gamepad)
            {
                _gamepad = null;
                System.Diagnostics.Debug.WriteLine("Gamepad now disconnected.");
            }
        }

        private void Instance_RawControllerAdded(object sender, RawGameController controller)
        {
            _rawGameController = controller;
            System.Diagnostics.Debug.WriteLine("Raw controller now connected.");
        }

        private void Instance_RawControllerRemoved(object sender, RawGameController controller)
        {
            if (_rawGameController == controller)
            {
                _rawGameController = null;
                System.Diagnostics.Debug.WriteLine("Raw controller now disconnected.");
            }
        }

        //Reading input from gamepad/controller
        private void Instance_InputUpdate(object sender, object gamepad)
        {
            //Standard gamepad input
            if (_gamepad == null)
            {
                return;
            }
                
            var reading = _gamepad.GetCurrentReading();

            //Joysticks
            double leftX = reading.LeftThumbstickX;
            double leftY = reading.LeftThumbstickY;
            double rightX = reading.RightThumbstickX;
            double rightY = reading.RightThumbstickY;

            //Triggers
            double leftTrigger = reading.LeftTrigger;
            double rightTrigger = reading.RightTrigger;

            //Buttons
            if (reading.Buttons.HasFlag(GamepadButtons.A))
            {
                
            }
            if (reading.Buttons.HasFlag(GamepadButtons.B))
            {

            }

            //Raw controller input
            if (_rawGameController == null)
            {
                return;
            }

            var buttons = new bool[_rawGameController.ButtonCount];
            var switches = new GameControllerSwitchPosition[_rawGameController.SwitchCount];
            var axis = new double[_rawGameController.AxisCount];

            _rawGameController.GetCurrentReading(buttons, switches, axis);

            if (buttons.Length > 0 && buttons[0])
            {

            }
        }
    }
}
