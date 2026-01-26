let uavMarker = null;

//Get query parameters from URL
function getQueryParam(name) {
    const urlParams = new URLSearchParams(window.location.search);
    return parseFloat(urlParams.get(name));
}

//UAV marker
function updateUAVPosition(lat, lon) {
    if (!uavMarker) {
        uavMarker = L.marker([lat, lon], {
            icon: L.icon({
                iconUrl: 'UAVIcon.png',
                iconSize: [32, 32],
                iconAnchor: [16, 16]
            })
        }).addTo(map);
    } else {
        uavMarker.setLatLng([ lat, lon ]);
    }

    map.panTo([lat, lon], { animate: false });
}

function refreshMapSize() {
    if (map) {
        map.invalidateSize(true);
    }
}

//Get device coordinates
const initialLatitude = getQueryParam('lat');
const initialLongitude = getQueryParam('lon');

//Initialize map
const map = L.map('map', {
    zoomControl: false
    }).setView([initialLatitude, initialLongitude],18);


//Disabling map interactions
map.dragging.disable();
map.touchZoom.disable();
map.scrollWheelZoom.disable();
map.doubleClickZoom.disable();
map.keyboard.disable();


L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; OpenStreetMap contributors'
}).addTo(map);

map.whenReady(function () {
    window.external.notify("MapReady")
});

window.mapReady = true;