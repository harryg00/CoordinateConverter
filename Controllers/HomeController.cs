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
            _trackCodes = json ?? new Dictionary<string, string>();
        }

        public IActionResult Index()
        {
            var wgsPoint = _geometryFactory.CreatePoint(
                new Coordinate(-1.144000, 52.947444)); // lon, lat

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

            Console.WriteLine($"Closest distance: {closestDistanceMeters:F2} meters");
            Console.WriteLine($"ELR: {closestCentreline?.Attributes.GetValueOrDefault("ELR", "N/A")}");
            // Chain is 22 yards, miles.yards (first 4 decimal places), 1000 yards is a mile, 1 metre is 1.09361 yards

            var closestPoint = DistanceOp.NearestPoints(closestCentreline?.Geometry, pointBng);
            var snappedPoint = new Point(closestPoint[0].X, closestPoint[0].Y);

            var indexedLine = new NetTopologySuite.LinearReferencing.LengthIndexedLine(closestCentreline?.Geometry);
            double distanceAlongLine = indexedLine.Project(snappedPoint.Coordinate);

            double start = (double) closestCentreline.Attributes["START"];

            double miles = (int) Math.Truncate(start);
            double yards = (int)(start - Math.Truncate(start)) * 1000;
            yards += distanceAlongLine * 1.09361; // Metres to yards conversion
            double chains = yards / 22; // 1 Chain is 22 yards
            if (chains % 80 >= 1) // If 80 chains, 1 mile
            {
                double difference = (int) chains % 80;
                double additionalMiles = (int) (chains - difference) / 80;
                miles += additionalMiles; // Add on the miles
                chains = difference; // So you are left with the remaining chains
            }

            Console.WriteLine($"Distance along line: {miles} miles, {chains} chains ({chains * 22} yards). Track type: {_trackCodes[closestCentreline.Attributes["TRACK_ID"].ToString()]}");

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
