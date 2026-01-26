using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace UAV_Assistive_Operation.Services
{
    public class MapService
    {
        private readonly CoreDispatcher _dispatcher;
        private readonly WebView _mapView;

        public MapService(CoreDispatcher dispatcher, WebView mapView)
        {
            _dispatcher = dispatcher;
            _mapView = mapView;
        }

        public async Task<Enums.MapInitResult> InitializeMapAsync()
        {
            double latitude = 51.50141;
            double longitiude = -0.14208;
            bool locationSuccess = true;

            try
            {
                var geoLocator = new Geolocator { DesiredAccuracy = PositionAccuracy.High };
                var position = await geoLocator.GetGeopositionAsync();
                latitude = position.Coordinate.Point.Position.Latitude;
                longitiude = position.Coordinate.Point.Position.Longitude;
            }
            catch 
            {
                locationSuccess = false;
                Debug.WriteLine("MapService: Location failed");
            }

            string url = $"ms-appx-web:///Assets/Map/LeafletMap.html?lat={latitude}&lon={longitiude}";
            _mapView.Source = new Uri(url);

            return locationSuccess ? Enums.MapInitResult.success : Enums.MapInitResult.failure;
        }

        public async Task UpdateUavLocationAsync(double lat, double lon)
        {
            string script = $"updateUAVPosition({lat.ToString(CultureInfo.InvariantCulture)}, {lon.ToString(CultureInfo.InvariantCulture)});";

            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                _ = _mapView.InvokeScriptAsync("eval", new[] { script });
            });
        }

        public async Task RefreshMap()
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                _ = _mapView.InvokeScriptAsync("eval", new[] { "refreshMapSize();" });
            });
        }
    }
}
