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

    map.eachLayer(function (layer) {
        if (layer instanceof L.CircleMarker) {
            map.removeLayer(layer);
        }
    });

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
        ],
        {
            padding: [10, 10]
        });

    const originalBlock = document.getElementById("originalBlock");
    const returnBlock = document.getElementById("returnBlock");
    originalBlock.innerHTML = `        
        <h1>Original:</h1>
        <hr />
        <ul>
            <li style="${result.original.easting != null ? 'font-weight: 600' : 'font-weight: 400'}">Easting: ${result.original.easting}</li>
            <li style="${result.original.northing != null ? 'font-weight: 600' : 'font-weight: 400'}">Northing: ${result.original.northing}</li>
            <li style="${result.original.latitude != null ? 'font-weight: 600' : 'font-weight: 400'}">Latitude: ${result.original.latitude}</li>
            <li style="${result.original.longitude != null ? 'font-weight: 600' : 'font-weight: 400'}">Longitude: ${result.original.longitude}</li>
            <li style="${result.original.elr != null ? 'font-weight: 600' : 'font-weight: 400'}">ELR: ${result.original.elr}</li>
            <li style="${result.original.mileage != null ? 'font-weight: 600' : 'font-weight: 400'}">Mileage: ${result.original.mileage}</li>
            <li style="${result.original.chainage != null ? 'font-weight: 600' : 'font-weight: 400'}">Chainage: ${result.original.chainage}</li>
            <li style="${result.original.yardage != null ? 'font-weight: 600' : 'font-weight: 400'}">Yardage: ${result.original.yardage}</li>
        </ul>
        `;
    returnBlock.innerHTML = `        
        <h1>Closest Track:</h1>
        <hr />

        <table border="1" style="border-collapse: collapse; width: 100%;">
            <thead>
                <tr>
                    <th style="text-align:left;">Field</th>
                    <th style="text-align:left;">Value</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td>ELR</td>
                    <td>${result.trackCentrelineInfo.elr}</td>
                </tr>
                <tr>
                    <td>Miles and Chains</td>
                    <td>${result.trackCentrelineInfo.mileage}m ${result.trackCentrelineInfo.chainage}ch</td>
                </tr>
                <tr>
                    <td>Yards along ELR</td>
                    <td>${result.trackCentrelineInfo.totalYardage}</td>
                </tr>
                <tr>
                    <td>Latitude</td>
                    <td>${result.locationReturn.latitudeLongitude.latitude}</td>
                </tr>
                <tr>
                    <td>Longitude</td>
                    <td>${result.locationReturn.latitudeLongitude.longitude}</td>
                </tr>
                <tr>
                    <td>Track Type</td>
                    <td>${result.trackCentrelineInfo.trackType}</td>
                </tr>
                <tr>
                    <td>
                        Easting / Northing 
                        <span class="tooltip">?
                            <span class="tooltiptext">
                                ${result.locationReturn.eastingNorthing.coordinateReferenceSystem.replaceAll("_", " ")}
                            </span>
                        </span>
                    </td>
                    <td>
                        [${parseInt(result.locationReturn.eastingNorthing.easting)}, 
                        ${parseInt(result.locationReturn.eastingNorthing.northing)}]
                    </td>
                </tr>
                <tr>
                    <td>Distance from track</td>
                    <td>${parseInt(result.distanceAwayInMetres)}m</td>
                </tr>
            </tbody>
        </table>
    `;
}

const inputOptions = document.getElementById("inputOptions")
inputOptions.addEventListener('change', (e) => {
    document.getElementById("latitudeLongitudeInput").style.display = 'none';
    document.getElementById("eastingNorthingInput").style.display = 'none';
    document.getElementById("trackComparisonInput").style.display = 'none';
    document.getElementById(e.target.value).style.display = 'flex';
});


function useMyLocation() {
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(
            (position) => {
                const latitude = position.coords.latitude;
                const longitude = position.coords.longitude;

                inputOptions.selectedIndex = 0;
                document.getElementById("latitude").value = latitude;
                document.getElementById("longitude").value = longitude;
            },
            (error) => {
                alert("Error getting location");
                console.error("Error getting location:", error.message);
            }
        );
    } else {
        alert("Geolocation is not supported by this browser.");
    }
}