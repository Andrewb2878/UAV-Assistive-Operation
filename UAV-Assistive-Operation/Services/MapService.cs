using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace UAV_Assistive_Operation.Services
{
    public class MapService
    {
        private readonly WebView _mapView;
        private bool _isMapReady = false;
        private DateTime _lastLocationUpdateTime = DateTime.MinValue;
        private const double UpdateIntervalMs = 200;

        public MapService(CoreDispatcher dispatcher, WebView mapView)
        {
            _mapView = mapView;
            _mapView.NavigationCompleted += MapViewNavigationCompleted;
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

        private void MapViewNavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            _isMapReady = true;
        }

        public async Task UpdateUavLocation(double lat, double lon)
        {
            if ((DateTime.Now - _lastLocationUpdateTime).TotalMilliseconds < UpdateIntervalMs)
                return;

            _lastLocationUpdateTime = DateTime.Now;

            await _mapView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {

                if (!_isMapReady || _mapView.Source == null)
                return;

                try
                {
                    string latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string lonStr = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);

                    // Filtering out 0 values when aircraft doesn't have a GPS lock
                    if (Math.Abs(lat) < 0.0001 && Math.Abs(lon) < 0.0001)
                        return;

                    await _mapView.InvokeScriptAsync("updateUavMarker", new[] { latStr, lonStr });

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"MapService: Failed to update UAV location: {ex.Message}");
                }
            });
        }

        public async Task UpdateUavHeading(double heading)
        {
            await _mapView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (!_isMapReady || _mapView.Source == null)
                    return;

                try
                {
                    string headingStr = heading.ToString(System.Globalization.CultureInfo.InvariantCulture);

                    await _mapView.InvokeScriptAsync("updateUavHeading", new[] { headingStr });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"MapService: Failed to update UAV heading: {ex.Message}");
                }
            });
        }
    }
}
