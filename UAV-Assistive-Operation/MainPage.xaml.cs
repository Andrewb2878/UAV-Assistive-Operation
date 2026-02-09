using System;
using System.Linq;
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

        //State
        private bool _mapServiceAvailable = false;

        public MainViewModel ViewModel { get; } = new MainViewModel();
        private bool IsControllerConnected => App.ControllerService.IsControllerConnected;
        private bool IsControllerRemapped => _mappingService.IsFullyRemapped;
        private bool IsAircraftConnected => App.DJIConnectionService.IsAircraftConnected;
        


        public MainPage()
        {
            InitializeComponent();
            DataContext = ViewModel;
            Loaded += MainPage_Loaded;

            //Services
            _remapInputService = new ControllerRemapInputService();

            _popupService = new UIPopupService();
            _popupService.RegisterPopups(ControllerRequiredPopup, ControllerRemappingPopup, AircraftRequiredPopup);

            _mappingService = new ControllerMappingService();
            _mapService = new MapService(MapView);

            //Subscriptions
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

            _ = InitializeMapAsync();
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
            _remapInputService.InputDetected += AssignInput;
            _remapInputService.Start();
        }

        private void UpdateScrollPosition()
        {
            var currentRow = ViewModel.CurrentRow;
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


        //Controller methods
        private void AssignInput(InputBindingModel binding) 
        {
            var row = ViewModel.CurrentRow;
            if (row == null)
                return;

            if (_mappingService.TryAssignBinding(row.Controls, binding, out string error, out ApplicationControls? autoAssigned))
            {
                row.AssignedInput = _mappingService.DescribeBinding(binding);
                row.Error = null;

                if (autoAssigned.HasValue)
                {
                    var autoRow = ViewModel.RemapRows.FirstOrDefault(r => r.Controls == autoAssigned.Value);
                    if (autoRow != null)
                    {
                        var oppositeBindingDescription = new InputBindingModel
                        {
                            Type = binding.Type,
                            Index = binding.Index,
                            Polarity = binding.Polarity,
                            Direction = -binding.Direction,
                        };

                        autoRow.AssignedInput = _mappingService.DescribeBinding(oppositeBindingDescription);
                        autoRow.Error = null;
                    }
                }

                if (ViewModel.AdvanceToNext())
                {
                    UpdateScrollPosition();
                }
                else
                {
                    StartCompletionSequenceAsync();
                }
            }
            else
            {
                row.Error = error;
            }
        }

        private async void StartCompletionSequenceAsync()
        {
            try
            {
                ShowCompletionProgress();
                _remapInputService.Stop();

                await Task.Delay(5000);

                EvaluatePopupState();
            }
            catch (Exception ex)
            {
                EventLogService.Instance.Log(LogEventType.Error, $"Controller configuration failed: {ex.Message}");
            }
        }
    }
}
