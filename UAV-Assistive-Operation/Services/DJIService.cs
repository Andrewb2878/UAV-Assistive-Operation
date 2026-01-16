using DJI.WindowsSDK;
using System;
using System.Diagnostics;
using Windows.UI.Core;

namespace UAV_Assistive_Operation.Services
{
    public class DJIService
    {
        public void Initialize(CoreDispatcher dispatcher)
        {
            DJISDKManager.Instance.SDKRegistrationStateChanged += async (state, result) =>
            {
                if (result == SDKError.NO_ERROR)
                {
                    System.Diagnostics.Debug.WriteLine("Register app successfully.");

                    //Updating the UAV connection state on changes.
                    DJISDKManager.Instance.ComponentManager.GetProductHandler(0).ProductTypeChanged += async (sender, value) =>
                    {
                        await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            if (value != null && value?.value != ProductType.UNRECOGNIZED)
                                Debug.WriteLine("The aircraft is now connected.");
                            else
                                Debug.WriteLine("The aircraft is now disconnected.");
                        });
                    };
                }
                else
                {
                    Debug.WriteLine($"SDK registration failed: {result}");
                }
            };
            DJISDKManager.Instance.RegisterApp("7b980d8aa60b87f6b740fd94");
            
        }
    }
}
