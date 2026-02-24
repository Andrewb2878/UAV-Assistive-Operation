using System;
using System.Threading.Tasks;
using UAV_Assistive_Operation.Enums;
using Windows.Devices.Geolocation;
using Windows.Networking.Connectivity;
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


        public MapService(WebView mapView)
        {
            _mapView = mapView;
            _mapView.NavigationCompleted += MapViewNavigationCompleted;
        }


        private bool HasInternet()
        {
            var profile = NetworkInformation.GetInternetConnectionProfile();
            return profile != null && profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
        }

        public async Task<MapInitResult> InitializeMapAsync()
        {
            if (!HasInternet())
            {
                EventLogService.Instance.Log(LogEventType.Warning, "MapService: No internet connection");
                return MapInitResult.failure;
            }

            double latitude = 51.50141;
            double longitude = -0.14208;
            bool locationSuccess = true;

            try
            {
                var geoLocator = new Geolocator { DesiredAccuracy = PositionAccuracy.High };
                var position = await geoLocator.GetGeopositionAsync();
                latitude = position.Coordinate.Point.Position.Latitude;
                longitude = position.Coordinate.Point.Position.Longitude;
            }
            catch 
            {
                locationSuccess = false;
                EventLogService.Instance.Log(LogEventType.Error, "MapService: Location failed");
            }

            string url = $"ms-appx-web:///Assets/Map/LeafletMap.html?lat={latitude}&lon={longitude}";
            _mapView.Source = new Uri(url);

            return locationSuccess ? MapInitResult.success : MapInitResult.failure;
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

            await App.RunOnUIThread(async () =>
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
                catch
                {
                    EventLogService.Instance.Log(LogEventType.Error, "MapService: Failed to update UAV location");
                }
            });
        }

        public async Task UpdateUavHeading(double heading)
        {
            await App.RunOnUIThread(async () =>
            {
                if (!_isMapReady || _mapView.Source == null)
                    return;

                try
                {
                    string headingStr = heading.ToString(System.Globalization.CultureInfo.InvariantCulture);

                    await _mapView.InvokeScriptAsync("updateUavHeading", new[] { headingStr });
                }
                catch
                {
                    EventLogService.Instance.Log(LogEventType.Error, "MapService: Failed to update UAV heading");
                }
            });
        }
    }
}
