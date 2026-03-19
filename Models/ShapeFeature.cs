using NetTopologySuite.Geometries;

namespace CoordinateConverter.Models
{
    public class ShapeFeature
    {
        public Geometry Geometry { get; set; }
        public string GeometryType { get; set; }
        public List<(double X, double Y)> Coordinates { get; set; } = new();
        public Dictionary<string, object> Attributes { get; set; } = new();
    }
}
