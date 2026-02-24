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
        private bool _startedConfiguration = false;
        private bool _completingConfiguration = false;
        private bool _completedFirstAircraftConnection = false;
        private bool _showSimulatorWarning = false;
        public MainViewModel ViewModel { get; }


        //Vales used to control popup visibilities
        private bool IsControllerConnected => App.ControllerService.IsControllerConnected;
        private bool IsControllerRemapped => _mappingService.IsFullyRemapped;
        private bool IsAircraftConnected => App.DJIConnectionService.IsAircraftConnected;
        private bool IsMenuOpen => ViewModel.Menu.MenuActive;
        private bool IsSimWarningOpen => ViewModel.SimulatorWarning.MenuActive;
        


        public MainPage()
        {
            InitializeComponent();


            //Service initialization
            _mappingService = new ControllerMappingService();
            _remapInputService = new ControllerRemapInputService(_mappingService);

            //View model initialization
            ViewModel = new MainViewModel(_mappingService);
            DataContext = ViewModel;
            ViewModel.Menu.PropertyChanged += Menu_PropertyChanged;

            _processingService = new ControllerProcessingService(_mappingService, App.DJIFlightControllerService,
                ViewModel.FlightCommand, ViewModel.Menu, ViewModel.SimulatorWarning);
            _mapService = new MapService(MapView);
            _popupService = new UIPopupService();
            _popupService.RegisterPopups(ControllerRequiredPopup, ControllerRemappingPopup, AircraftRequiredPopup, MenuPopup,
                SimulatorWarningPopup);


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

            ViewModel.Menu.CommandRequested += MenuCommandRequested;
            ViewModel.SimulatorWarning.CommandRequested += SimulatorCommandRequested;

            App.DJISimulatorService.SimulatorStateChanged += isRunning =>
            {
                ViewModel.Menu.IsToggleButtonEnabled = isRunning;
            };

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

        private void Menu_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.Menu.MenuActive))
            {
                if (ViewModel.Menu.MenuActive)
                    ViewModel.Menu.SelectedIndex = 0;

                EvaluatePopupState();
            }
        }


        //UI popup methods        
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

        //Controller configuration popup
        private void ShowRemapping()
        {
            if (!_startedConfiguration)
            {
                _remapInputService.InputDetected += InputDetected;
                _startedConfiguration = true;
            }
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

            if (RemapItemsControl.ContainerFromItem(currentRow) is FrameworkElement container)
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
                _remapInputService.InputDetected -= InputDetected;
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


        //Menu popup
        private void MenuCommandRequested(MenuCommand command, int index)
        {
            if (App.DJIFlightDataService.IsFlying)
            {
                ViewModel.Menu.SetRowError(index, "Cannot perform action during flight.");
                return;
            }

            switch (command)
            {
                case MenuCommand.ReconfigureController:
                    _ = HandleReconfigAsync(); break;
                case MenuCommand.ToggleSimulator:
                    if (!App.DJIConnectionService.IsAircraftConnected && !ViewModel.Menu.IsToggleButtonEnabled)
                    {
                        ViewModel.Menu.SetRowError(index, "Aircraft must be connected to start simulator mode");
                        return;
                    }
                    HandleToggleSimulator();
                    break;
                case MenuCommand.ExitApplication:
                    HandleExit(); break;
            }
        }

        private async Task HandleReconfigAsync()
        {
            _processingService.Stop();
            _mappingService.ClearBindings();

            ViewModel.ControllerConfiguration.Reset();
            ViewModel.Menu.MenuActive = false;

            EvaluatePopupState();
            EventLogService.Instance.Log(LogEventType.System, "Controller reconfiguration started...");

            await Task.Delay(50);
            RemapScrollViewer.ChangeView(null, 0, null);
        }

        private async void HandleToggleSimulator()
        {
            if (!ViewModel.Menu.IsToggleButtonEnabled)
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

        private void HandleExit()
        {
            Application.Current.Exit();
        }


        private void SimulatorCommandRequested()
        {
            ViewModel.SimulatorWarning.MenuActive = false;
            _ = App.DJISimulatorService.StartSimulatorAsync();

            EvaluatePopupState();
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
            _completedFirstAircraftConnection = true;
            EvaluatePopupState();
        }
    }
}
