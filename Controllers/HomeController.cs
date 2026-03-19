using CoordinateConverter.Models;
using CoordinateConverter.Services;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using System.Diagnostics;

namespace CoordinateConverter.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IGpkgHelper _gpkgHelper;
        private readonly IShapeFileService _shapeService;
        private readonly GeometryFactory _geometryFactory;
        private readonly ICoordinateTransformer _transformer;

        public HomeController(
            ILogger<HomeController> logger,
            IGpkgHelper gpkgHelper,
            IShapeFileService shapeService,
            GeometryFactory geometryFactory,
            ICoordinateTransformer transformer)
        {
            _logger = logger;
            _gpkgHelper = gpkgHelper;
            _shapeService = shapeService;
            _geometryFactory = geometryFactory;
            _transformer = transformer;
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

            ViewBag.ClosestDistanceMeters = closestDistanceMeters;
            ViewBag.ClosestELR = closestCentreline?.Attributes.GetValueOrDefault("ELR", "None");

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
