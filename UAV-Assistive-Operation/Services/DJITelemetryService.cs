using DJI.WindowsSDK;
using DJI.WindowsSDK.Components;
using UAV_Assistive_Operation.Models;
using System;
using System.Diagnostics;
using Windows.UI.Core;

namespace UAV_Assistive_Operation.Services
{
    public class DJITelemetryService
    {
        private readonly CoreDispatcher _dispatcher;
        private BatteryHandler _batteryHandler;
        private bool _running;

        public BatteryModel Battery { get; } = new BatteryModel();

        public DJITelemetryService(CoreDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public void AircraftConnected()
        {
            if (_running)
                return;

            _running = true;

            SubscribeToBattery();
        }

        public void AircraftDisconnected()
        {
            if (!_running)
                return;

            _running = false;

            Battery.Percentage = null;
            UnsubscribeFromBattery();
        }

        private void SubscribeToBattery()
        {
            _batteryHandler = DJISDKManager.Instance.ComponentManager.GetBatteryHandler(0, 0);
            if (_batteryHandler != null)
            {
                _batteryHandler.ChargeRemainingInPercentChanged += BatteryPercentChanged;
                Debug.WriteLine("Subscribed to battery percentage updates");
            }
            else
            {
                Debug.WriteLine("Battery handler not available");
            }
        }

        private void UnsubscribeFromBattery()
        {
            if (_batteryHandler != null)
            {
                _batteryHandler.ChargeRemainingInPercentChanged -= BatteryPercentChanged;
                _batteryHandler = null;
                Debug.WriteLine("Unsubscribed to battery percentge updates");
            }
        }

        private async void BatteryPercentChanged(object sender, IntMsg? value)
        {
            if (!_running || value == null)
                return;

            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Battery.Percentage = value.Value.value;
            });
        }
    }
}
