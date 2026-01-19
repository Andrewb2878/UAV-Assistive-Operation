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

        //Airacraft connection/disconnection events for services to subscribe to
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
        }

        private async void ProductTypeChanged(object sender, ProductTypeMsg? value)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (value != null && value?.value != ProductType.UNRECOGNIZED)
                {
                    Debug.WriteLine("The aircraft is now connected.");
                    AircraftConnected?.Invoke();
                }
                else
                {
                    Debug.WriteLine("The aircraft is now disconnected.");
                    AircraftDisconnected?.Invoke();
                }
            });
        }
    }
}
