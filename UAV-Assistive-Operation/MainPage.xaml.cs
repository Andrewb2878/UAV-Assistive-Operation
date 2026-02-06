using System;
using System.Threading.Tasks;
using UAV_Assistive_Operation.Configuration;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;
using UAV_Assistive_Operation.Services;
using Windows.Gaming.Input;
using Windows.UI.Core;
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
        private readonly ControllerRemappingService _remappingService;

        //State
        private bool _listeningForRemap = false;
        private DateTime _lastAssignmentTime = DateTime.MinValue;
        private const int AssignmentCooldownMs = 500;

        private bool[] _lastButtons;
        private double[] _lastAxes;
        private GameControllerSwitchPosition[] _lastSwitches;

        private bool _mapServiceAvailable = false;

        public MainViewModel ViewModel { get; } = new MainViewModel();
        private bool IsControllerConnected => App.ControllerService.IsControllerConnected;
        private bool IsControllerRemapped => _remappingService.IsFullyRemapped;
        private bool IsAircraftConnected => App.DJIConnectionService.IsAircraftConnected;
        


        public MainPage()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            Loaded += MainPage_Loaded;

            //Services
            _popupService = new UIPopupService();
            _popupService.RegisterPopups(ControllerRequiredPopup, ControllerRemappingPopup, AircraftRequiredPopup);

            _remappingService = new ControllerRemappingService();
            _mapService = new MapService(MapView);

            //Subscriptions
            MapView.NavigationCompleted += MapView_NavigationCompleted;
            MapView.NavigationFailed += MapView_NavigationFailed;

            App.ControllerService.RawControllerConnected += RawControllerConnected;
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
            _listeningForRemap = true;
            App.ControllerService.RawControllerUpdated += RawControllerUpdated;
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

        private void RawControllerUpdated(bool[] buttons, GameControllerSwitchPosition[] switches, double[] axes)
        {
            if (!_listeningForRemap)
                return;

            //Cooldown timer
            if ((DateTime.Now - _lastAssignmentTime).TotalMilliseconds < AssignmentCooldownMs)
            {
                _lastButtons = (bool[])buttons.Clone();
                _lastSwitches = (GameControllerSwitchPosition[])switches.Clone();
                _lastAxes = (double[])axes.Clone();
                return;
            }

            var row = ViewModel.CurrentRow;
            if (row == null)
                return;

            if (_lastButtons != null)
            {
                for (int index = 0; index < buttons.Length; index++)
                {
                    if (buttons[index] && !_lastButtons[index])
                    {
                        AssignInput(row, new InputBindingModel
                        {
                            Type = InputTypes.Button,
                            Index = index
                        });
                        return;
                    }
                }
            }

            if (_lastSwitches != null)
            {
                for (int index = 0; index< switches.Length; index++)
                {
                    if (switches[index] != GameControllerSwitchPosition.Center &&
                        switches[index] != _lastSwitches[index])
                    {
                        AssignInput(row, new InputBindingModel
                        {
                            Type = InputTypes.Switch,
                            Index = index
                        });
                        return;
                    }
                }
            }

            if (_lastAxes != null)
            {
                for (int index = 0; index < axes.Length; index++)
                {
                    double delta = axes[index] - _lastAxes[index];
                    if (Math.Abs(delta) > 0.3)
                    {
                        AssignInput(row, CreateAxisBinding(index, axes[index], _lastAxes[index], row.Controls));
                        return;
                    }
                }
            }
            _lastButtons = (bool[])buttons.Clone();
            _lastSwitches = (GameControllerSwitchPosition[])switches.Clone();
            _lastAxes = (double[])axes.Clone();
        }

        private InputBindingModel CreateAxisBinding(int index, double currentValue, double restingValue, ApplicationControls control)
        {
            var rule = ControlRemappingRules.Rules[control];

            //Checking control rules to differentiate between joystick and trigger
            if (rule.AllowBipolarAxis && !rule.AllowUnipolarAxis)
                return BuildBinding(index, currentValue, AxisPolarity.Bipolar);

            if (rule.AllowUnipolarAxis && !rule.AllowBipolarAxis)
                return BuildBinding(index, currentValue, AxisPolarity.Unipolar);

            //Additional checks
            if (currentValue < -0.2 || restingValue < -0.2)
                return BuildBinding(index, currentValue, AxisPolarity.Bipolar);

            bool hasDrift = Math.Abs(restingValue) > 0.0001 && Math.Abs(currentValue) < 0.2;
            if (hasDrift)
                return BuildBinding(index, currentValue, AxisPolarity.Bipolar);

            if (rule.AutoCreateOpposite)
                return BuildBinding(index, currentValue, AxisPolarity.Bipolar);

            return BuildBinding(index, currentValue, AxisPolarity.Unipolar);
        }

        private InputBindingModel BuildBinding(int index, double value, AxisPolarity polarity)
        {
            return new InputBindingModel
            {
                Type = InputTypes.Axis,
                Index = index,
                Polarity = polarity,
                Direction = value >= 0 ? 1 : -1,
            };
        }

        private void AssignInput(ControlRemapRowViewModel row, InputBindingModel binding) 
        {
            if (_remappingService.TryAssignBinding(row.Controls, binding, out string error))
            {
                row.AssignedInput = DescribeBinding(binding);
                row.Error = null;

                _lastAssignmentTime = DateTime.Now;

                if (ViewModel.AdvanceToNext())
                {
                    UpdateScrollPosition();
                }
                else
                {
                    FinishRemapping();
                }
            }
            else
            {
                row.Error = error;
            }
        }

        private string DescribeBinding(InputBindingModel binding)
        {
            switch (binding.Type) 
            {
                case InputTypes.Button:
                    return $"Button {binding.Index}";
                case InputTypes.Switch:
                    return $"Switch {binding.Index}";
                case InputTypes.Axis:
                    if (binding.Polarity == AxisPolarity.Unipolar)
                    {
                        return $"Trigger {binding.Index}";
                    }
                    else
                    {
                        return $"Axis {binding.Index} ({(binding.Direction > 0 ? "+" : "-")})";
                    }
                default:
                    return "Unknown";

            };
        }

        private void FinishRemapping()
        {
            _listeningForRemap = false;
            App.ControllerService.RawControllerUpdated -= RawControllerUpdated;

            EvaluatePopupState();
        }
    }
}
