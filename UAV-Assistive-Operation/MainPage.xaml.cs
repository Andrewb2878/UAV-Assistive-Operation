using UAV_Assistive_Operation.Services;
using Windows.Gaming.Input;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UAV_Assistive_Operation
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly MapService _mapService;

        public MainPage()
        {
            this.InitializeComponent();

            DataContext = App.DJITelemetryService;
            
            //Loading leaflet map
            _mapService = new MapService(MapView);
            _ = _mapService.InitializeMapAsync();

            //Controller subscriptions
            App.ControllerService.GamepadConnected += GamepadConnected;
            App.ControllerService.GamepadDisconnected += GamepadDisconnected;
            App.ControllerService.GamepadUpdated += GamepadInput;
            App.ControllerService.RawControllerUpdated += RawInput;

        }

        private void GamepadConnected(Gamepad gamepad)
        {

        }

        private void GamepadDisconnected()
        {

        }

        private void GamepadInput(Windows.Gaming.Input.GamepadReading gamepad)
        {

        }

        private void RawInput(bool[] buttons, GameControllerSwitchPosition[] switches, double[] axes)
        {

        }
    }
}
