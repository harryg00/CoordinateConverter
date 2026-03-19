using NetTopologySuite.Geometries;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace CoordinateConverter.Services
{
    public class CoordinateTransformer : ICoordinateTransformer
    {
        private readonly MathTransform _mathTransform;
        private readonly GeometryFactory _geometryFactory;

        public CoordinateTransformer(GeometryFactory geometryFactory)
        {
            _geometryFactory = geometryFactory;

            var wgs84 = GeographicCoordinateSystem.WGS84;

            const string BngWkt = @"
            PROJCS[""OSGB 1936 / British National Grid"",
              GEOGCS[""OSGB 1936"",
                DATUM[""OSGB_1936"",
                  SPHEROID[""Airy 1830"",6377563.396,299.3249646,AUTHORITY[""EPSG"",""7001""]],
                  TOWGS84[446.448,-125.157,542.06,0.15,0.247,0.842,-20.489],AUTHORITY[""EPSG"",""6277""]],
                PRIMEM[""Greenwich"",0,AUTHORITY[""EPSG"",""8901""]],
                UNIT[""degree"",0.0174532925199433,AUTHORITY[""EPSG"",""9122""]],
                AUTHORITY[""EPSG"",""4277""]],
              PROJECTION[""Transverse_Mercator""],
              PARAMETER[""latitude_of_origin"",49],
              PARAMETER[""central_meridian"",-2],
              PARAMETER[""scale_factor"",0.9996012717],
              PARAMETER[""false_easting"",400000],
              PARAMETER[""false_northing"",-100000],
              UNIT[""metre"",1,AUTHORITY[""EPSG"",""9001""]],
              AUTHORITY[""EPSG"",""27700""]]";

            var csFactory = new CoordinateSystemFactory();
            var bng = csFactory.CreateFromWkt(BngWkt) as ProjectedCoordinateSystem;

            var ctFactory = new CoordinateTransformationFactory();
            var transformation = ctFactory.CreateFromCoordinateSystems(wgs84, bng);

            _mathTransform = transformation.MathTransform;
        }

        public (double x, double y) TransformWgsToBng(double lon, double lat)
        {
            // MathTransform expects (x=lon, y=lat)
            return _mathTransform.Transform(lon, lat);
        }

        public Point ToBngPoint(Point wgsPoint, GeometryFactory? geometryFactory = null)
        {
            var gf = geometryFactory ?? _geometryFactory;
            var (easting, northing) = TransformWgsToBng(wgsPoint.X, wgsPoint.Y);
            var pt = gf.CreatePoint(new Coordinate(easting, northing));
            pt.SRID = 27700;
            return pt;
        }
    }
}
