using System;
using System.Linq;
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
        //Services
        private readonly UIPopupService _popupService;
        private readonly MapService _mapService;
        private readonly ControllerRemappingService _remappingService;

        //State
        private bool _listeningForRemap = false;
        private DateTime _lastAssignmentTime = DateTime.MinValue;
        private const int AssignmentCooldownMs = 500;
        private GamepadReading? _lastReading;

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
            _listeningForRemap = true;
            _lastReading = null;

            App.ControllerService.GamepadUpdated += GamepadUpdated;
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
        private void GamepadUpdated(GamepadReading reading)
        {

            if (!_listeningForRemap)
                return;

            //Cooldown timer
            if ((DateTime.Now - _lastAssignmentTime).TotalMilliseconds < AssignmentCooldownMs)
            {
                _lastReading = reading;
                return;
            }

            var row = ViewModel.CurrentRow;
            if (row == null)
                return;

            if (_lastReading.HasValue)
            {
                var last = _lastReading.Value;

                //Buttons
                DetectButton(GamepadButtons.A, reading, last, row, 0);
                DetectButton(GamepadButtons.B, reading, last, row, 1);
                DetectButton(GamepadButtons.X, reading, last, row, 2);
                DetectButton(GamepadButtons.Y, reading, last, row, 3);
                DetectButton(GamepadButtons.LeftShoulder, reading, last, row, 4);
                DetectButton(GamepadButtons.RightShoulder, reading, last, row, 5);
                DetectButton(GamepadButtons.DPadLeft, reading, last, row, 6);
                DetectButton(GamepadButtons.DPadUp, reading, last, row, 7);
                DetectButton(GamepadButtons.DPadRight, reading, last, row, 8);
                DetectButton(GamepadButtons.DPadDown, reading, last, row, 9);
                DetectButton(GamepadButtons.View, reading, last, row, 10);
                DetectButton(GamepadButtons.Menu, reading, last, row, 11);
                DetectButton(GamepadButtons.Paddle1, reading, last, row, 12);
                DetectButton(GamepadButtons.Paddle2, reading, last, row, 13);
                DetectButton(GamepadButtons.Paddle3, reading, last, row, 14);
                DetectButton(GamepadButtons.Paddle4, reading, last, row, 15);

                //Triggers
                DetectTrigger(reading.LeftTrigger, last.LeftTrigger, row, 0);
                DetectTrigger(reading.RightTrigger, last.RightTrigger, row, 1);

                //Joysticks
                DetectAxis(reading.LeftThumbstickX, last.LeftThumbstickX, row, 2);
                DetectAxis(reading.LeftThumbstickY, last.LeftThumbstickY, row, 3);
                DetectAxis(reading.RightThumbstickX, last.RightThumbstickX, row, 4);
                DetectAxis(reading.RightThumbstickY, last.RightThumbstickY, row, 5);
            }
            _lastReading = reading;
        }

        private void DetectButton(GamepadButtons button, GamepadReading current, GamepadReading last,
            ControlRemapRowViewModel row, int index)
        {
            if (current.Buttons.HasFlag(button) && !last.Buttons.HasFlag(button))
            {
                AssignInput(row, new InputBindingModel
                {
                    Type = InputTypes.Button,
                    Index = index
                });
            }
        }

        private void DetectTrigger(double current, double last, ControlRemapRowViewModel row, int index)
        {
            if (current > 0.7 && last < 0.1)
            {
                AssignInput(row, new InputBindingModel
                {
                    Type = InputTypes.Axis,
                    Index = index,
                    Polarity = AxisPolarity.Unipolar,
                    Direction = 1
                });
            }
        }

        private void DetectAxis(double current, double last, ControlRemapRowViewModel row, int index)
        {
            double delta = Math.Abs(current - last);

            if (delta > 0.5)
            {
                AssignInput(row, new InputBindingModel
                {
                    Type = InputTypes.Axis,
                    Index = index,
                    Polarity = AxisPolarity.Bipolar,
                    Direction = current >= 0 ? 1 : -1
                });
            }
        }

        private void AssignInput(ControlRemapRowViewModel row, InputBindingModel binding) 
        {
            if (_remappingService.TryAssignBinding(row.Controls, binding, out string error, out ApplicationControls? autoAssigned))
            {
                row.AssignedInput = DescribeBinding(binding);
                row.Error = null;
                _lastAssignmentTime = DateTime.Now;

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

                        autoRow.AssignedInput = DescribeBinding(oppositeBindingDescription);
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

        private async void StartCompletionSequenceAsync()
        {
            ShowCompletionProgress();

            _listeningForRemap = false;
            App.ControllerService.GamepadUpdated -= GamepadUpdated;

            await Task.Delay(5000);

            EvaluatePopupState();
        }
    }
}
