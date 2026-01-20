// Gets query parameters from URL
function getQueryParam(name) {
    const urlParams = new URLSearchParams(window.location.search);
    return parseFloat(urlParams.get(name));
}

// Getting coordinates
const initialLatitude = getQueryParam('lat');
const initialLongitude = getQueryParam('lon');

// Initialize map
const map = L.map('map').setView(
    [initialLatitude, initialLongitude],
    18
);

L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; OpenStreetMap contributors'
}).addTo(map);

// UAV marker
const uavMarker = L.marker([initialLatitude, initialLongitude])
    .addTo(map)
    .bindPopup('UAV Start Position')
    .openPopup();

// Exposed for C# calls
function updateUavMarker(lat, lon) {
    uavMarker.setLatLng([lat, lon]);
    map.panTo([lat, lon]);
}

// Make callable from WebView
window.updateUavMarker = updateUavMarker;
