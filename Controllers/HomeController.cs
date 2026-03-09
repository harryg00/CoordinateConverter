using CoordinateConverter.Models;
using CoordinateConverter.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CoordinateConverter.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IGpkgHelper _gpkgHelper;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment env, IGpkgHelper gpkgHelper)
        {
            _logger = logger;
            _env = env;
            _gpkgHelper = gpkgHelper;
        }

        public IActionResult Index()
        {
            string test = Closest("52.263447", "-0.952382").Result;
            Console.WriteLine(test);
            return View();
        }

        [HttpGet]
        public Task<string> Closest([FromBody] string latitude, [FromBody] string longitude)
        {
            double lat = Double.Parse(latitude);
            double lon = Double.Parse(longitude);

            var (closestLine, distanceMeters) = _gpkgHelper.FindClosest(lat, lon);

            return Task.FromResult($"Closest line feature ID: {closestLine.Attributes["ELR"]}, Distance: {distanceMeters} meters");
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
