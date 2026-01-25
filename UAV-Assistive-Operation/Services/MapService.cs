using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml.Controls;

namespace UAV_Assistive_Operation.Services
{
    public class MapService
    {
        private readonly WebView _mapView;

        public MapService(WebView mapView)
        {
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
    }
}
