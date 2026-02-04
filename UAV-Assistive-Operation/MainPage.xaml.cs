using System.Threading.Tasks;
using UAV_Assistive_Operation.Enums;
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
        private readonly UIPopupService _popupService;
        private readonly MapService _mapService;
        private readonly ControllerRemappingService _remappingService;

        private bool IsControllerConnected => App.ControllerService.IsControllerConnected;
        private bool IsControllerRemapped => _remappingService.IsFullyRemapped;
        private bool IsAircraftConnected => App.DJIConnectionService.IsAircraftConnected;
        private bool _mapServiceAvailable = false;


        public MainPage()
        {
            InitializeComponent();
            DataContext = new MainViewModel();         
            Loaded += MainPage_Loaded;


            //Initializing popups
            _popupService = new UIPopupService();
            _popupService.RegisterPopups(ControllerRequiredPopup, ControllerRemappingPopup, AircraftRequiredPopup);


            //Loading leaflet map
            _mapService = new MapService(MapView);
            _ = InitializeMapAsync();
            MapView.NavigationCompleted += MapView_NavigationCompleted;
            MapView.NavigationFailed += MapView_NavigationFailed;

            //Loading UAV map updates
            App.DJIFlightDataService.UavLocationUpdated += async (lat, lon) =>
            {
                await _mapService.UpdateUavLocation(lat, lon);
            };
            App.DJIFlightDataService.UAVHeadingUpdated += async heading =>
            {
                await _mapService.UpdateUavHeading(heading);
            };

            //Initializing remapping service
            _remappingService = new ControllerRemappingService();

            //Aircraft subscriptions
            App.DJIConnectionService.AircraftConnected += AircraftConnected;


            //Controller subscriptions
            App.ControllerService.RawControllerConnected += RawControllerConnected;            
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs args)
        {
            EvaluatePopupState();
        }


        //UI methods
        private void EvaluatePopupState()
        {
            if (!IsControllerConnected)
            {
                _popupService.ShowPopup(UIPopups.ControllerRequired);
            }
            else if (!IsControllerRemapped)
            {
                _popupService.ShowPopup(UIPopups.ControllerRemapping);
                ShowRemapping(ApplicationControls.ThrottleUp);
            }
            else if (!IsAircraftConnected)
            {
                _popupService.ShowPopup(UIPopups.AircraftRequired);
            }
            else
            {
                _popupService.ShowPopup(UIPopups.None);
            }
        }

        private async void ShowRemapping(ApplicationControls controls)
        {
            await App.RunOnUIThread(() =>
            {
                ThrottleUpHighlight.Visibility = Visibility.Visible;
            });
        }


        //Map methods
        private async Task InitializeMapAsync()
        {
            var result = await _mapService.InitializeMapAsync();
            _mapServiceAvailable = result == MapInitResult.success;

            if (result != MapInitResult.success)
            {
                MapView.Visibility = Visibility.Collapsed;
                MapFallback.Visibility = Visibility.Visible;
                EventLogService.Instance.Log(LogEventType.Warning, "MapService: Currently unavailable");
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


        //Aircraft methods
       private void AircraftConnected()
        {
            EvaluatePopupState();
        }


        //Controller methods
        private void RawControllerConnected(RawGameController controller)
        {
            EvaluatePopupState();
        }
    }
}
