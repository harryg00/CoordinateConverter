using CoordinateConverter.Models;
using CoordinateConverter.Services;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Distance;
using System.Diagnostics;
using System.Text.Json;

namespace CoordinateConverter.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IShapeFileService _shapeService;
        private readonly GeometryFactory _geometryFactory;
        private readonly ICoordinateTransformer _transformer;
        private readonly IWebHostEnvironment _env;
        private readonly Dictionary<string, string> _trackCodes;

        public HomeController(
            ILogger<HomeController> logger,
            IShapeFileService shapeService,
            GeometryFactory geometryFactory,
            ICoordinateTransformer transformer,
            IWebHostEnvironment env)
        {
            _logger = logger;
            _shapeService = shapeService;
            _geometryFactory = geometryFactory;
            _transformer = transformer;
            _env = env;
            var trackCodesPath = Path.Combine(_env.ContentRootPath, "Data", "TrackCodes.json");
            var json = JsonSerializer.Deserialize<Dictionary<string, string>>(System.IO.File.ReadAllText(trackCodesPath));
            _trackCodes = json ?? [];
        }

        public IActionResult Index()
        {
            Console.WriteLine(JsonSerializer.Serialize(ClosestCentreline(52.947444, -1.144000)));
            return View();
        }

        [HttpGet]
        public CoordinateReturn ClosestCentreline(double latitude, double longitude)
        {
            var wgsPoint = _geometryFactory.CreatePoint(
                new Coordinate(longitude, latitude)); // lon, lat

            var pointBng = _transformer.ToBngPoint(wgsPoint, _geometryFactory);

            ShapeFeature? closestCentreline = null;
            double closestDistanceMeters = double.MaxValue;

            foreach (var feature in _shapeService.CentreLineFeatures)
            {
                if (feature.Geometry == null) continue;

                double distance = feature.Geometry.Distance(pointBng);
                if (distance < closestDistanceMeters)
                {
                    closestDistanceMeters = distance;
                    closestCentreline = feature;
                }
            }

            string ELR = closestCentreline?.Attributes.GetValueOrDefault("ELR", "N/A")?.ToString();
            // Chain is 22 yards, miles.yards (first 4 decimal places), 1760 yards is a mile, 1 metre is 1.09361 yards
            var closestPoint = DistanceOp.NearestPoints(closestCentreline?.Geometry, pointBng);
            var snappedPoint = new Point(closestPoint[0].X, closestPoint[0].Y);

            var indexedLine = new NetTopologySuite.LinearReferencing.LengthIndexedLine(closestCentreline?.Geometry);
            double distanceAlongLine = indexedLine.Project(snappedPoint.Coordinate);

            double start = (double)closestCentreline.Attributes["START"];

            double miles = (int)Math.Truncate(start);
            double yards = (int)(start - Math.Truncate(start)) * 1000, yardsLeft = 0.0;
            yards += distanceAlongLine * 1.09361; // Metres to yards conversion
            double chains = (int) yards / 22; // 1 Chain is 22 yards
            double difference = chains / 80;
            if (difference >= 1) // If 80 chains, 1 mile
            {
                int additionalMiles = (int) Math.Floor(difference);
                miles += additionalMiles; // Add on the miles
                chains = chains - (additionalMiles * 80); // So you are left with the remaining chains
            }
            double extraYards = yards - (chains * 22);

            char[] trackCodes = closestCentreline.Attributes["TRACK_ID"].ToString().ToCharArray(); // First 2 digits of track code matter so take those and compare to list of track codes
            string trackCode = trackCodes[0].ToString() + trackCodes[1].ToString() + "00";

            TrackCentreline centreline = new TrackCentreline
            {
                AssetID = closestCentreline.Attributes["ASSET_ID"]?.ToString(),
                ELR = ELR,
                TrackType = _trackCodes[trackCode],
                TrackID = closestCentreline.Attributes["TRACK_ID"]?.ToString(),
                Owner = closestCentreline.Attributes["OWNER"]?.ToString(),
                Mileage = (int)miles,
                Chainage = (int)chains,
                Yardage = (int)extraYards,
                TotalYardage = (int)Math.Floor(yards)
            };

            Original original = new Original
            {
                Latitude = latitude,
                Longitude = longitude,
            };

            ELRMileChain elrMileChain = new ELRMileChain
            {
                ELR = ELR,
                Mileage = (int) miles,
                Chainage = (int) chains,
                Yardage = (int) Math.Floor(extraYards)
            };
            (string crsName, string wkt) = closestCentreline.CRS.Value;
            EastingNorthing eastingNorthing = new EastingNorthing
            {
                Easting = snappedPoint.X,
                Northing = snappedPoint.Y,
                CoordinateReferenceSystem = wkt
            };

            (double endLatitude, double endLongitude) = _transformer.ConvertToLatLong(eastingNorthing);
            LatitudeLongitude latitudeLongitude = new LatitudeLongitude
            {
                Latitude = endLatitude,
                Longitude = endLongitude
            };
            eastingNorthing.CoordinateReferenceSystem = crsName;

            LocationReturn locationReturn = new LocationReturn
            {
                EastingNorthing = eastingNorthing,
                LatitudeLongitude = latitudeLongitude,
                ELRMileChain = elrMileChain
            };

            CoordinateReturn closestReturn = new CoordinateReturn
            {
                DistanceAwayInMetres = closestDistanceMeters,
                Original = original,
                LocationReturn = locationReturn,
                TrackCentrelineInfo = centreline
            };

            return closestReturn;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
