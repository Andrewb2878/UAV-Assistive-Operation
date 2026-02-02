using DJI.WindowsSDK;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UAV_Assistive_Operation.Enums;

namespace UAV_Assistive_Operation.Services
{
    public class DJIConnectionService
    {
        private readonly string _sdkKey;

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
        public void Initialize()
        {
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
                
                Debug.WriteLine("SDK Registered successfully.");
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

        private async Task<bool> ValidateAircraftConnectionAsync()
        {
            try
            {
                var handler = DJISDKManager.Instance.ComponentManager.GetFlightControllerHandler(0,0);

                var result = await handler.GetAircraftNameAsync();
                return result.value.Value.value != null && result.error == SDKError.NO_ERROR;
            }
            catch
            {
                return false;
            }
        }

        private async void EvaluateConnectionState()
        {
            //EventLogService.Instance.Log(LogEventType.Debug, $"Product present value: {_productPresent}");
            //EventLogService.Instance.Log(LogEventType.Debug, $"flight controller value: {_flightControllerConnected}");
            bool shouldBeConnected = _productPresent && _flightControllerConnected;

            if (shouldBeConnected == _isAircraftConnected)
                return;

            _isAircraftConnected = shouldBeConnected;
            await App.RunOnUIThread(() =>
            {
                if (_isAircraftConnected)
                {
                    EventLogService.Instance.Log(LogEventType.Connection, "Aircraft connected");
                    AircraftConnected?.Invoke();
                }
                else
                {
                    EventLogService.Instance.Log(LogEventType.Warning, "Aircraft disconnected");
                    AircraftDisconnected?.Invoke();
                }
            });
        }

        public bool CheckAircraftConnected()
        {
            return _isAircraftConnected;
        }
    }
}
