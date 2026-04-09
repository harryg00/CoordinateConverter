let map;

document.addEventListener('DOMContentLoaded', () => {
    var lat = 52.3555;
    var lng = -1.1743;

    map = L.map('closestMap').setView([lat, lng], 6);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);
});

async function findClosest() {
    const latitude = document.getElementById("latitude").value;
    const longitude = document.getElementById("longitude").value;

    const result = await fetch(`/Home/ClosestCentreline?latitude=${latitude}&longitude=${longitude}`)
        .then(response => response.json());

    const originalLat = result.original.latitude;
    const originalLng = result.original.longitude;

    const returnLat = result.locationReturn.latitudeLongitude.latitude;
    const returnLng = result.locationReturn.latitudeLongitude.longitude;

    const redStyle = {
        radius: 8,
        color: "black",
        fillColor: "red",
        fillOpacity: 0.8
    };

    const blueStyle = {
        radius: 8,
        color: "black",
        fillColor: "blue",
        fillOpacity: 0.8
    };

    L.circleMarker([originalLat, originalLng], redStyle)
        .addTo(map)
        .bindPopup("Original Location");

    L.circleMarker([returnLat, returnLng], blueStyle)
        .addTo(map)
        .bindPopup("Returned Location");

    L.polyline([
        [originalLat, originalLng],
        [returnLat, returnLng]
    ], {
        color: "green",
        weight: 3
    }).addTo(map);

    const midLat = (originalLat + returnLat) / 2;
    const midLng = (originalLng + returnLng) / 2;

    L.marker([midLat, midLng])
        .addTo(map)
        .bindPopup(`${result.distanceAwayInMetres} metres`);

    map.fitBounds([
        [originalLat, originalLng],
        [returnLat, returnLng]
    ]);

    const originalBlock = document.getElementById("originalBlock");
    const returnBlock = document.getElementById("returnBlock");
    originalBlock.innerHTML = `        
        <h1>Original:</h1>
        <hr />
        <ul>
            <li>Easting: ${result.original.easting}</li>
            <li>Northing: ${result.original.northing}</li>
            <li>Latitude: ${result.original.latitude}</li>
            <li>Longitude: ${result.original.longitude}</li>
            <li>ELR: ${result.original.elr}</li>
            <li>Mileage: ${result.original.mileage}</li>
            <li>Chainage: ${result.original.chainage}</li>
            <li>Yardage: ${result.original.yardage}</li>
        </ul>
        `;
    returnBlock.innerHTML = `        
        <h1>Closest Track:</h1>
        <hr />
        <ul>
            <li>ELR: ${result.trackCentrelineInfo.elr}</li>
            <li>Miles and Chains: ${result.trackCentrelineInfo.mileage}m ${result.trackCentrelineInfo.chainage}ch</li>
            <li>Yards along ELR: ${result.trackCentrelineInfo.totalYardage}</li>
            <li>Latitude: ${result.locationReturn.latitudeLongitude.latitude}</li>
            <li>Longitude: ${result.locationReturn.latitudeLongitude.longitude}</li>
            <li>Track Type: ${result.trackCentrelineInfo.trackType}</li>
            <li>Easting / Northing (${result.locationReturn.eastingNorthing.coordinateReferenceSystem}): [${parseInt(result.locationReturn.eastingNorthing.easting)}, ${parseInt(result.locationReturn.eastingNorthing.easting) }]</li>
            <li>Distance from track: ${parseInt(result.distanceAwayInMetres)}m</li>
        </ul>
        `;
}
