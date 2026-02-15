using System;
using System.Threading.Tasks;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;
using UAV_Assistive_Operation.Services;
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
        //Services
        private readonly UIPopupService _popupService;
        private readonly MapService _mapService;
        private readonly ControllerMappingService _mappingService;
        private readonly ControllerRemapInputService _remapInputService;
        private readonly ControllerProcessingService _processingService;

        //State
        private bool _mapServiceAvailable = false;
        private bool _completingConfiguration = false;
        public MainViewModel ViewModel { get; }


        private bool IsControllerConnected => App.ControllerService.IsControllerConnected;
        private bool IsControllerRemapped => _mappingService.IsFullyRemapped;
        private bool IsAircraftConnected => App.DJIConnectionService.IsAircraftConnected;
        


        public MainPage()
        {
            InitializeComponent();


            //Service initialization
            _mappingService = new ControllerMappingService();
            _remapInputService = new ControllerRemapInputService(_mappingService);

            //View model initialization
            ViewModel = new MainViewModel(_mappingService);
            DataContext = ViewModel;

            _processingService = new ControllerProcessingService(_mappingService, App.DJIFlightControllerService,
                ViewModel.FlightCommand);
            _mapService = new MapService(MapView);
            _popupService = new UIPopupService();
            _popupService.RegisterPopups(ControllerRequiredPopup, ControllerRemappingPopup, AircraftRequiredPopup);


            //Setup subscriptions
            RegisterEvents();

            _ = InitializeMapAsync();
        }

        
        private void RegisterEvents()
        {
            Loaded += MainPage_Loaded;

            MapView.NavigationCompleted += MapView_NavigationCompleted;
            MapView.NavigationFailed += MapView_NavigationFailed;

            App.ControllerService.GamepadConnected += _ => EvaluatePopupState();
            App.DJIConnectionService.AircraftConnected += AircraftConnected;

            App.DJIFlightDataService.UavLocationUpdated += async (lat, lon) =>
            {
                await _mapService.UpdateUavLocation(lat, lon);
            };

            App.DJIFlightDataService.UAVHeadingUpdated += async heading =>
            {
                await _mapService.UpdateUavHeading(heading);
            };
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
                ShowRemapping();
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

        private void ShowRemapping()
        {
            _remapInputService.InputDetected += InputDetected;
            _remapInputService.Start();
        }

        private void InputDetected(InputBindingModel binding)
        {
            if (_completingConfiguration)
                return;

            bool isComplete = ViewModel.ControllerConfiguration.HandleInput(binding);

            if (isComplete)
            {
                StartCompletionSequenceAsync();
            }
            else
            {
                UpdateScrollPosition();
            }
        }

        private void UpdateScrollPosition()
        {
            var currentRow = ViewModel.ControllerConfiguration.CurrentRow;
            if (currentRow == null)
                return;

            var container = RemapItemsControl.ContainerFromItem(currentRow) as FrameworkElement;
            if (container != null)
            {
                var transform = container.TransformToVisual(RemapItemsControl);
                var position = transform.TransformPoint(new Windows.Foundation.Point(0, 0));

                RemapScrollViewer.ChangeView(null, position.Y - 50, null);
            }
        }

        private async void StartCompletionSequenceAsync()
        {
            _completingConfiguration = true;

            try
            {
                _remapInputService.Stop();
                ShowCompletionProgress();
                _processingService.Start();

                await Task.Delay(5000);
                EventLogService.Instance.Log(LogEventType.System, $"Controller configured");

                EvaluatePopupState();
            }
            catch (Exception error)
            {
                EventLogService.Instance.Log(LogEventType.Error, $"Controller configuration failed: {error.Message}");
                _completingConfiguration = false;
            }
        }

        private void ShowCompletionProgress()
        {
            RemapProgressText.Visibility = Visibility.Visible;
            RemapProgressBar.Visibility = Visibility.Visible;
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
    }
}
