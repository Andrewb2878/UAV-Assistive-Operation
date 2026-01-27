//Get query parameters from URL
function getQueryParam(name) {
    const urlParams = new URLSearchParams(window.location.search);
    return parseFloat(urlParams.get(name));
}

//Get device coordinates
const initialLatitude = getQueryParam('lat');
const initialLongitude = getQueryParam('lon');

//Initialize map
const map = L.map('map', {
    zoomControl: false
    }).setView([initialLatitude, initialLongitude], 18);


//Disabling map interactions
map.dragging.disable();
map.touchZoom.disable();
map.scrollWheelZoom.disable();
map.doubleClickZoom.disable();
map.keyboard.disable();


L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; OpenStreetMap contributors'
}).addTo(map);


// UAV marker
const uavMarker = L.marker([initialLatitude, initialLongitude], {
    icon: L.icon({
        iconUrl: 'UAVIcon.png',
        shadowUrl: 'UAVHeading.png',
        iconSize: [62, 62],
        shadowSize: [40, 40],
        iconAnchor: [31, 31],
        shadowAnchor: [20, 32]
    })
}).addTo(map)

function rotateMarker(marker, angle) {
    if (!marker || !marker._icon)
        return;

    const icon = marker._icon;
    const shadow = marker._shadow;

    icon.style.transformOrigin = "50% 50%";
    let currentIconTransform = icon.style.transform.replace(/rotate\(-?\d+.?\d*deg\)/g, "");
    icon.style.transform = `${currentIconTransform} rotate(${angle}deg)`;

    if (shadow) {
        shadow.style.transformOrigin = "50% 80%";
        let currentShadowTransform = shadow.style.transform.replace(/rotate\(-?\d+.?\d*deg\)/g, "");
        shadow.style.transform = `${currentShadowTransform} rotate(${angle}deg)`;
    }
}


function updateUavMarker(lat, lon) {
    uavMarker.setLatLng([lat, lon]);
}

function updateUavHeading(heading) {
    rotateMarker(uavMarker, heading);
}

//Make callable from WebView
window.updateUavMarker = updateUavMarker;
window.updateUavHeading = updateUavHeading;