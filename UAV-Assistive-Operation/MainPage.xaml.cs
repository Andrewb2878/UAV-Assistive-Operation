using System;
using System.Threading.Tasks;
using UAV_Assistive_Operation.Enums;
using UAV_Assistive_Operation.Models;
using UAV_Assistive_Operation.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UAV_Assistive_Operation
{
    /// <summary>
    /// Main application page
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //Services
        private readonly UIPopupService _popupService;
        private readonly MapService _mapService;
        private readonly ControllerMappingService _mappingService;
        private readonly ControllerInputRegistrationService _inputRegistrationService;
        private readonly ControllerInputProcessingService _processingService;

        //State
        private bool _mapServiceAvailable = false;
        private bool _startedConfiguration = false;
        private bool _completingConfiguration = false;
        private bool _completedFirstAircraftConnection = false;
        public MainViewModel ViewModel { get; }


        //Vales used to control popup visibilities
        private bool IsControllerConnected => App.ControllerService.IsControllerConnected;
        private bool IsControllerRemapped => _mappingService.IsFullyRemapped;
        private bool IsAircraftConnected => App.DJIConnectionService.IsAircraftConnected;
        private bool IsMenuOpen => ViewModel.Menu.MenuActive;
        private bool IsSimWarningOpen => ViewModel.SimulatorWarning.MenuActive;
        

        /// <summary>
        /// Initializes services, view models, popup registrations and event subscriptions
        /// </summary>
        public MainPage()
        {
            InitializeComponent();


            //Service initialization
            _mappingService = new ControllerMappingService();
            _inputRegistrationService = new ControllerInputRegistrationService(_mappingService);

            //View model initialization
            ViewModel = new MainViewModel(_mappingService);
            DataContext = ViewModel;
            ViewModel.Menu.PropertyChanged += Menu_PropertyChanged;

            _processingService = new ControllerInputProcessingService(_mappingService, App.DJIFlightControllerService,
                ViewModel.FlightCommand, ViewModel.Menu, ViewModel.SimulatorWarning);
            _mapService = new MapService(MapView);
            _popupService = new UIPopupService();
            _popupService.RegisterPopups(ControllerRequiredPopup, ControllerRemappingPopup, AircraftRequiredPopup, MenuPopup,
                SimulatorWarningPopup);


            //Setup subscriptions
            RegisterEvents();

            _ = InitializeMapAsync();
        }

        
        /// <summary>
        /// Subscribes to application events
        /// </summary>
        private void RegisterEvents()
        {
            Loaded += MainPage_Loaded;

            MapView.NavigationCompleted += MapView_NavigationCompleted;
            MapView.NavigationFailed += MapView_NavigationFailed;

            App.ControllerService.GamepadConnected += _ => EvaluatePopupState();
            App.DJIConnectionService.AircraftConnected += AircraftConnected;

            ViewModel.Menu.CommandRequested += MenuCommandRequested;
            ViewModel.SimulatorWarning.CommandRequested += SimulatorCommandRequested;

            App.DJISimulatorService.SimulatorStateChanged += isRunning =>
            {
                var simulatorRow = ViewModel.Menu.SimulatorRow;
                if (simulatorRow != null)
                    simulatorRow.IsToggled = isRunning;
            };

            App.DJIFlightDataService.UAVLocationUpdated += async (lat, lon) =>
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

        private void Menu_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.Menu.MenuActive))
            {
                if (ViewModel.Menu.MenuActive)
                    ViewModel.Menu.SelectedIndex = 0;

                EvaluatePopupState();
            }
        }

        private void RemapItemsControl_PreviewKeyDown(object sender, KeyRoutedEventArgs args)
        {
            if (args.Key == Windows.System.VirtualKey.GamepadLeftTrigger ||
                args.Key == Windows.System.VirtualKey.GamepadRightTrigger ||
                args.Key == Windows.System.VirtualKey.GamepadLeftShoulder ||
                args.Key == Windows.System.VirtualKey.GamepadRightShoulder)
            {
                args.Handled = true;
            }
        }


        /// <summary>
        /// Displays popups depending on controller connection and configuration, aircraft
        /// connection, menu state and simulator warnings
        /// </summary>      
        private void EvaluatePopupState()
        {
            //Setting input mode
            if (ViewModel.SimulatorWarning.MenuActive)
                _processingService.SetMode(InputMode.SimWarning);
            else if (ViewModel.Menu.MenuActive)            
                _processingService.SetMode(InputMode.Menu);            
            else
                _processingService.SetMode(InputMode.Flight);

            //Setting popup visibility
            if (!IsControllerConnected)
            {
                _popupService.ShowPopup(UIPopups.ControllerRequired);
            }
            else if (!IsControllerRemapped)
            {
                _popupService.ShowPopup(UIPopups.ControllerRemapping);
                ShowRemapping();
            }
            else if (!IsAircraftConnected && !_completedFirstAircraftConnection)
            {
                _popupService.ShowPopup(UIPopups.AircraftRequired);
            }
            else if (IsMenuOpen)
            {
                _popupService.ShowPopup(UIPopups.Menu);
            }
            else if (IsSimWarningOpen)
            {
                _popupService.ShowPopup(UIPopups.SimulatorWarning);
            }
            else
            {
                _popupService.ShowPopup(UIPopups.None);
            }
        }

        /// <summary>
        /// Starts controller remapping
        /// 
        /// Subscribes to input detection event and starts the remapInputService
        /// </summary>
        private void ShowRemapping()
        {
            if (!_startedConfiguration)
            {
                _inputRegistrationService.InputDetected += InputDetected;
                _startedConfiguration = true;
            }
            _inputRegistrationService.Start();
        }

        /// <summary>
        /// Handles controller input during remapping and triggers configuration completion
        /// </summary>
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

            RemapItemsControl.ScrollIntoView(currentRow);           
        }

        /// <summary>
        /// <para> Completes controller configuration </para>
        /// 
        /// <para> Unsubscribes from input detection, starts live controller processing and
        /// logs completion state </para>
        /// </summary>
        private async void StartCompletionSequenceAsync()
        {
            _completingConfiguration = true;

            try
            {
                _inputRegistrationService.Stop();
                _inputRegistrationService.InputDetected -= InputDetected;
                _startedConfiguration = false;

                _processingService.Start();

                await Task.Delay(5000);
                EventLogService.Instance.Log(LogEventType.System, "Controller configured");
                _completingConfiguration = false;

                EvaluatePopupState();
            }
            catch (Exception error)
            {
                EventLogService.Instance.Log(LogEventType.Error, $"Controller configuration failed: {error.Message}");
                _completingConfiguration = false;
            }
        }


        /// <summary>
        /// Handles menu command selections, preventing unsafe
        /// actions if aircraft is flying
        /// </summary>
        private void MenuCommandRequested(MenuCommand command, int index)
        {
            switch (command)
            {
                case MenuCommand.ReconfigureController:
                    if (IsActionBlocked(index)) return;
                    _ = HandleReconfigAsync(); break;

                case MenuCommand.ToggleSimulator:
                    if (IsActionBlocked(index)) return;
                    var simulatorRow = ViewModel.Menu.SimulatorRow;

                    if (!App.DJIConnectionService.IsAircraftConnected && !simulatorRow.IsToggled)
                    {
                        ViewModel.Menu.SetRowError(index, "Aircraft must be connected to start simulator mode");
                        return;
                    }
                    HandleToggleSimulator(simulatorRow); break;

                case MenuCommand.TelemetryUnits:
                    var unitRow = ViewModel.Menu.UnitRow;

                    unitRow.IsToggled = !unitRow.IsToggled;
                    App.DJITelemetryService.Altitude.UseMetric = !unitRow.IsToggled;
                    App.DJITelemetryService.Speed.UseMetric = !unitRow.IsToggled;
                    EventLogService.Instance.Log(LogEventType.Info, "Telemetry units changed");
                    break;

                case MenuCommand.ExitApplication:
                    if (IsActionBlocked(index)) return;
                    HandleExit(); break;
            }
        }

        /// <summary>
        /// Checks if the drone is flying and sets a row error if an unsafe action is attempted.
        /// </summary>
        private bool IsActionBlocked(int index)
        {
            if (App.DJIFlightDataService.IsFlying)
            {
                ViewModel.Menu.SetRowError(index, "Cannot perform action during flight.");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Stops controller processing and restarts the controller configuration sequence
        /// </summary>
        private async Task HandleReconfigAsync()
        {
            _processingService.Stop();
            _mappingService.ClearBindings();

            ViewModel.ControllerConfiguration.Reset();
            ViewModel.Menu.MenuActive = false;

            EvaluatePopupState();
            EventLogService.Instance.Log(LogEventType.System, "Controller reconfiguration started...");

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                UpdateScrollPosition();
            });
        }

        /// <summary>
        /// Toggles DJI simulator mode, showing a warning prior to start up
        /// </summary>
        private async void HandleToggleSimulator(MenuRowViewModel simulatorRow)
        {
            if (!simulatorRow.IsToggled)
            {
                ViewModel.SimulatorWarning.MenuActive = true;
                await App.DJISimulatorService.InitializeSimulatorAsync();
            }
            else
            {
                _ = App.DJISimulatorService.StopSimulatorAsync();
            }
            ViewModel.Menu.MenuActive = false;
            EvaluatePopupState();
        }

        /// <summary>
        /// Confirms simulator start after warning 
        /// </summary>
        private void SimulatorCommandRequested()
        {
            ViewModel.SimulatorWarning.MenuActive = false;
            _ = App.DJISimulatorService.StartSimulatorAsync();

            EvaluatePopupState();
        }

        private void HandleExit()
        {
            Application.Current.Exit();
        }


        /// <summary>
        /// Starts the MapService and determines if the map fallback UI should be displayed,
        /// logs if unavailable 
        /// </summary>
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


        /// <summary>
        /// Triggered when the aircraft successfully connects
        /// </summary>
        private void AircraftConnected()
        {
            _completedFirstAircraftConnection = true;
            EvaluatePopupState();
        }
    }
}
