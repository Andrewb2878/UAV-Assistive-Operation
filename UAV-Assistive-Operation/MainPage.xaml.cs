using System;
using System.Threading.Tasks;
using UAV_Assistive_Operation.Models;
using UAV_Assistive_Operation.Services;
using Windows.Gaming.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UAV_Assistive_Operation
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool _controllerPopupShown = false;
        private bool _controllerConn = false;
        private readonly MapService _mapService;
        private bool _mapServiceAvailable = false;


        public MainPage()
        {
            this.InitializeComponent();
            DataContext = new MainViewModel();
            this.Loaded += MainPage_Loaded;


            //Loading leaflet map
            _mapService = new MapService(Dispatcher, MapView);
            _ = InitializeMapAsync();
            MapView.NavigationCompleted += MapView_NavigationCompleted;
            MapView.NavigationFailed += MapView_NavigationFailed;


            App.DJIFlightDataService.UavLocationUpdated += async (lat, lon) =>
            {
                await _mapService.UpdateUavLocation(lat, lon);
            };
            App.DJIFlightDataService.UAVHeadingUpdated += async heading =>
            {
                await _mapService.UpdateUavHeading(heading);
            };


            //Alert banner
            //App.AlertService.AlertState("TEST_ALERT", true, "Test alert", 5);


            //Controller subscriptions
            App.ControllerService.GamepadConnected += GamepadConnected;
            App.ControllerService.GamepadDisconnected += GamepadDisconnected;
            App.ControllerService.GamepadUpdated += GamepadInput;
            App.ControllerService.RawControllerUpdated += RawInput;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            CheckControllerOnStartup();
        }

        //Map methods
        private async Task InitializeMapAsync()
        {
            var result = await _mapService.InitializeMapAsync();
            _mapServiceAvailable = result == Enums.MapInitResult.success;

            if (result != Enums.MapInitResult.success)
            {
                MapView.Visibility = Visibility.Collapsed;
                MapFallback.Visibility = Visibility.Visible;
                EventLogService.Instance.Log(Enums.LogEventType.Warning, "MapService: Currently unavailable");
            }
        }

        private void MapView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            if (args.IsSuccess && _mapServiceAvailable)
            {
                MapFallback.Visibility = Visibility.Collapsed;
                MapView.Visibility = Visibility.Visible;
            }
        }

        private void MapView_NavigationFailed(object sender, WebViewNavigationFailedEventArgs args) 
        {
            MapView.Visibility = Visibility.Collapsed;
            MapFallback.Visibility = Visibility.Visible;
        }


        //Controller methods
        private async void CheckControllerOnStartup()
        {
            if (!_controllerConn)
            {
                _controllerPopupShown = true;
                await ControllerRequiredPopup.ShowAsync();
            }
        }

        private async void GamepadConnected(Gamepad gamepad)
        {
            _controllerConn = true;

            if (_controllerPopupShown)
            {
                _controllerPopupShown = false;

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (ControllerRequiredPopup.IsLoaded)
                        ControllerRequiredPopup.Hide();
                });
            }
        }

        private void GamepadDisconnected()
        {
            _controllerConn = false;

        }

        private void GamepadInput(Windows.Gaming.Input.GamepadReading gamepad)
        {

        }

        private void RawInput(bool[] buttons, GameControllerSwitchPosition[] switches, double[] axes)
        {

        }
    }
}
