using CoordinateConverter.Models;
using CoordinateConverter.Services;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.Esri;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

public class ShapeFileService : IShapeFileService
{
    private readonly IWebHostEnvironment _env;
    private readonly GeometryFactory _geometryFactory;

    public List<ShapeFeature> CentreLineFeatures { get; private set; } = new();
    public List<ShapeFeature> ELRFeatures { get; private set; } = new();

    private static readonly ICoordinateTransformation _wgs84ToBng;

    public ShapeFileService(
        IWebHostEnvironment env,
        GeometryFactory geometryFactory)
    {
        _env = env ?? throw new ArgumentNullException(nameof(env));
        _geometryFactory = geometryFactory ?? throw new ArgumentNullException(nameof(geometryFactory));

        LoadShapes("Centrelines", "NWR_TrackCentreLines.shp", CentreLineFeatures);
        LoadShapes("ELRs", "NWR_ELRs.shp", ELRFeatures);
    }

    private void LoadShapes(string folder, string fileName, List<ShapeFeature> targetList)
    {
        targetList.Clear();

        var shpPath = Path.Combine(_env.ContentRootPath, "Data", folder, fileName);

        if (!File.Exists(shpPath))
        {
            throw new FileNotFoundException($"Shapefile not found: {shpPath}");
        }

        var features = Shapefile.ReadAllFeatures(shpPath);

        foreach (var f in features)
        {
            var geometry = f.Geometry;
            if (geometry == null || geometry.IsEmpty) continue;

            var feature = new ShapeFeature
            {
                Geometry = geometry,
                GeometryType = geometry.GeometryType
            };

            foreach (var coord in geometry.Coordinates)
            {
                feature.Coordinates.Add((coord.X, coord.Y));
            }

            foreach (string attrName in f.Attributes.GetNames())
            {
                object? value = f.Attributes[attrName];
                if (value == DBNull.Value) value = null;
                feature.Attributes[attrName] = value;
            }

            targetList.Add(feature);
        }
    }

    static ShapeFileService() 
    {
        var cf = new CoordinateSystemFactory();

        // WGS84 (EPSG:4326) - geographic lon/lat
        var wgs84 = GeographicCoordinateSystem.WGS84;

        // OSGB36 / British National Grid (EPSG:27700)
        string bngWkt = @"PROJCS[""OSGB36 / British National Grid"",
        GEOGCS[""OSGB36"",
            DATUM[""Ordnance Survey of Great Britain 1936"",
                SPHEROID[""Airy 1830"",6377563.396,299.3249646,
                    AUTHORITY[""EPSG"",""7001""]],
                AUTHORITY[""EPSG"",""6277""]],
            PRIMEM[""Greenwich"",0,
                AUTHORITY[""EPSG"",""8901""]],
            UNIT[""degree"",0.0174532925199433,
                AUTHORITY[""EPSG"",""9122""]],
            AUTHORITY[""EPSG"",""4277""]],
        PROJECTION[""Transverse_Mercator""],
        PARAMETER[""latitude_of_origin"",49],
        PARAMETER[""central_meridian"",-2],
        PARAMETER[""scale_factor"",0.9996012717],
        PARAMETER[""false_easting"",400000],
        PARAMETER[""false_northing"",-100000],
        UNIT[""metre"",1,
            AUTHORITY[""EPSG"",""9001""]],
        AXIS[""Easting"",EAST],
        AXIS[""Northing"",NORTH],
        AUTHORITY[""EPSG"",""27700""]]";

        var bng = cf.CreateFromWkt(bngWkt) as ProjectedCoordinateSystem;

        var tf = new CoordinateTransformationFactory();
        _wgs84ToBng = tf.CreateFromCoordinateSystems(wgs84, bng);
    }
}