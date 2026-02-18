using System;
using System.Threading.Tasks;
using UAV_Assistive_Operation.Services;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UAV_Assistive_Operation
{
    /// <summary>
    /// Provides application-specific behaviour to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public static DJIConnectionService DJIConnectionService { get; private set; }
        public static DJITelemetryService DJITelemetryService { get; private set; }
        public static DJIFlightDataService DJIFlightDataService { get; private set; }
        public static DJIFlightControllerService DJIFlightControllerService { get; private set; }
        public static ControllerService ControllerService { get; private set; }
        public static AlertService AlertService { get; internal set; }
        public static EvaluationService EvaluationService { get; private set; }

        public static CoreDispatcher UIDispatcher { get; private set; }

        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            DJIConnectionService = new DJIConnectionService(Secrets.DjiKey);
            ControllerService = new ControllerService();

            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Maximized;
            
        }

        public static async Task RunOnUIThread(Action action)
        {
            if (UIDispatcher == null)
                return;

            if (UIDispatcher.HasThreadAccess)
            {
                action();
            }
            else
            {
                await UIDispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    new DispatchedHandler(action));
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            if (!(Window.Current.Content is Frame rootFrame))
            {
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                Window.Current.Content = rootFrame;
            }

            UIDispatcher = Window.Current.Dispatcher;

            ControllerService.Initialize();
            ControllerService.Start();

            DJIConnectionService.Initialize();
            DJITelemetryService = new DJITelemetryService();
            DJIFlightDataService = new DJIFlightDataService();
            DJIFlightControllerService = new DJIFlightControllerService();
            AlertService = new AlertService();
            EvaluationService = new EvaluationService(DJIConnectionService, DJITelemetryService,
                                                        DJIFlightDataService, ControllerService, AlertService);


            DJIConnectionService.AircraftConnected += DJITelemetryService.AircraftConnected;
            DJIConnectionService.AircraftConnected += DJIFlightDataService.AircraftConnected;
            DJIConnectionService.AircraftConnected += () => DJIFlightControllerService.AircraftConnected(DJIConnectionService,
                                                                                                        DJITelemetryService,
                                                                                                        DJIFlightDataService);

            DJIConnectionService.AircraftDisconnected += DJITelemetryService.AircraftDisconnected;
            DJIConnectionService.AircraftDisconnected += DJIFlightDataService.AircraftDisconnected;
            DJIConnectionService.AircraftDisconnected += DJIFlightControllerService.AircraftDisconnected;

            
            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
