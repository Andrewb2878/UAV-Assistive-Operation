using DJI.WindowsSDK;
using System;
using System.Diagnostics;
using Windows.UI.Core;

namespace UAV_Assistive_Operation.Services
{
    public class DJIConnectionService
    {
        private readonly string _sdkKey;
        private CoreDispatcher _dispatcher;

        //Used for aircraft connection/disconnection verification
        private bool _productPresent;
        private bool _flightControllerConnected;
        private bool _isAircraftConnected;

        //Aircraft connection/disconnection events for services to subscribe to
        public event Action AircraftConnected;
        public event Action AircraftDisconnected;

        public DJIConnectionService(string sdkKey)
        {
            _sdkKey = sdkKey ?? throw new ArgumentNullException(nameof(sdkKey));
        }

        /// <summary>
        /// Triggers one time SDK setup
        /// </summary>
        /// <param name="dispatcher"></param>
        public void Initialize(CoreDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            RegisterSdk();
        }

        //SDK registration
        private void RegisterSdk()
        {
            DJISDKManager.Instance.SDKRegistrationStateChanged += SdkRegistrationChanged;
            DJISDKManager.Instance.RegisterApp(_sdkKey);
        }

        //Registration results
        private void SdkRegistrationChanged(SDKRegistrationState state, SDKError result)
        {
            if (result == SDKError.NO_ERROR)
            {
                System.Diagnostics.Debug.WriteLine("SDK Registered successfully.");
                SubscribeToProductChanges();
            }
            else
            {
                Debug.WriteLine($"SDK registration failed: {result}");
            }
        }

        //Aircraft connection monitoring
        private void SubscribeToProductChanges()
        {
            DJISDKManager.Instance.ComponentManager.GetProductHandler(0).ProductTypeChanged += ProductTypeChanged;
            DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0, 0).ConnectionChanged += FlightControllerConnectionChanged;
        }

        //Aircraft connection managment
        private void ProductTypeChanged(object sender, ProductTypeMsg? value)
        {
            _productPresent = value != null && value.Value.value != ProductType.UNRECOGNIZED;

            EvaluateConnectionState();
        }

        private void FlightControllerConnectionChanged(object sender, BoolMsg? value)
        {
            if (value == null)
                return;

            _flightControllerConnected = value.Value.value;
            EvaluateConnectionState();
        }

        private async void EvaluateConnectionState()
        {
            bool shouldBeConnected = _productPresent && _flightControllerConnected;

            if (shouldBeConnected == _isAircraftConnected)
                return;

            _isAircraftConnected = shouldBeConnected;
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (_isAircraftConnected)
                {
                    Debug.WriteLine("Aircraft connected");
                    AircraftConnected?.Invoke();
                }
                else
                {
                    Debug.WriteLine("Aircraft disconnected");
                    AircraftDisconnected?.Invoke();
                }
            });
        }
    }
}
