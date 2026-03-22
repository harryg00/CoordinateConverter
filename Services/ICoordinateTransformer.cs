using CoordinateConverter.Models;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;

namespace CoordinateConverter.Services
{
    public interface ICoordinateTransformer
    {
        (double x, double y) TransformWgsToBng(double lon, double lat);
        Point ToBngPoint(Point wgsPoint, GeometryFactory geometryFactory);
        (double Latitude, double Longitude) ConvertToLatLong(EastingNorthing eastingNorthing);
    }
}
