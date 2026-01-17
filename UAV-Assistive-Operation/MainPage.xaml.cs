using DJI.WindowsSDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Gaming.Input;
using Windows.System;
using Windows.UI.ViewManagement;
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

        public MainPage()
        {
            this.InitializeComponent();

            App.ControllerService.GamepadUpdated += GamepadInput;
            App.ControllerService.RawControllerUpdated += RawInput;

            App.ControllerService.GamepadConnected += GamepadConnected;
            App.ControllerService.GamepadDisconnected += GamepadDisconnected;
        }

        private void GamepadInput(Windows.Gaming.Input.GamepadReading gamepad)
        {
            
        }

        private void RawInput(bool[] buttons, GameControllerSwitchPosition[] switches, double[] axes)
        {

        }

        private void GamepadConnected(Gamepad gamepad)
        {

        }

        private void GamepadDisconnected()
        {

        }
    }
}
