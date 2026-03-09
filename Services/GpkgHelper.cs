using CoordinateConverter.Services;
using Microsoft.Data.Sqlite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.IO;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

public class GpkgHelper : IGpkgHelper
{
    private readonly WKBReader _wkbReader = new();
    private readonly ICoordinateTransformation _wgs84ToBNG;
    private readonly IWebHostEnvironment _env;
    private readonly List<GpkgFeature> _features = new();
    private readonly STRtree<GpkgFeature> _spatialIndex = new();
    private bool _loaded = false;

    public GpkgHelper(IWebHostEnvironment env)
    {
        _env = env;

        var csFactory = new CoordinateSystemFactory();

        var osgb36Datum = csFactory.CreateHorizontalDatum(
            "OSGB36", DatumType.HD_Geocentric, Ellipsoid.GRS80, null
        );

        var geographicCS = csFactory.CreateGeographicCoordinateSystem(
            "OSGB36",
            AngularUnit.Degrees,
            osgb36Datum,
            PrimeMeridian.Greenwich,
            new AxisInfo("Lon", AxisOrientationEnum.East),
            new AxisInfo("Lat", AxisOrientationEnum.North)
        );

        var bng = csFactory.CreateProjectedCoordinateSystem(
            "OSGB36 / British National Grid",
            geographicCS,
            csFactory.CreateProjection(
                "Transverse_Mercator",
                "Transverse_Mercator",
                new List<ProjectionParameter>
                {
                    new ProjectionParameter("latitude_of_origin", 49.0),
                    new ProjectionParameter("central_meridian", -2.0),
                    new ProjectionParameter("scale_factor", 0.9996012717),
                    new ProjectionParameter("false_easting", 400000.0),
                    new ProjectionParameter("false_northing", -100000.0)
                }
            ),
            LinearUnit.Metre,
            new AxisInfo("East", AxisOrientationEnum.East),
            new AxisInfo("North", AxisOrientationEnum.North)
        );

        var ctFactory = new CoordinateTransformationFactory();
        _wgs84ToBNG = ctFactory.CreateFromCoordinateSystems(
            GeographicCoordinateSystem.WGS84,
            bng
        );
    }

    private void LoadGeometryLines()
    {
        if (_loaded) return;

        string gpkgPath = Path.Combine(_env.ContentRootPath, "NWR_GTCL20260209.gpkg");

        using var conn = new SqliteConnection($"Data Source={gpkgPath}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT table_name FROM gpkg_contents WHERE data_type='features'";

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            string tableName = reader.GetString(0);

            var geomCmd = conn.CreateCommand();
            geomCmd.CommandText = $"SELECT * FROM [{tableName}]";

            using var geomReader = geomCmd.ExecuteReader();

            while (geomReader.Read())
            {
                try
                {
                    var blob = (byte[])geomReader["geom"];
                    Geometry geom = ParseGeoPackageGeometry(blob);

                    var attributes = new Dictionary<string, object>();

                    for (int i = 0; i < geomReader.FieldCount; i++)
                    {
                        string name = geomReader.GetName(i);
                        if (name != "geom")
                            attributes[name] = geomReader.GetValue(i);
                    }

                    var feature = new GpkgFeature
                    {
                        Geometry = geom,
                        Attributes = attributes
                    };

                    _features.Add(feature);

                    _spatialIndex.Insert(geom.EnvelopeInternal, feature);
                }
                catch
                {
                    continue;
                }
            }
        }

        _spatialIndex.Build();
        _loaded = true;
    }

    public (GpkgFeature closestFeature, double closestDistance) FindClosest(double lat, double lon)
    {
        if (!_loaded)
            LoadGeometryLines();

        double[] xy = _wgs84ToBNG.MathTransform.Transform(new[] { lon, lat });

        var searchPoint = new Point(xy[0], xy[1]) { SRID = 27700 };

        double searchRadius = 1000;
        List<GpkgFeature> candidates;

        do
        {
            var env = new Envelope(
                searchPoint.X - searchRadius,
                searchPoint.X + searchRadius,
                searchPoint.Y - searchRadius,
                searchPoint.Y + searchRadius
            );

            candidates = _spatialIndex.Query(env).ToList();

            searchRadius *= 2;

        } while (candidates.Count == 0 && searchRadius < 500000);

        GpkgFeature closestFeature = null;
        double closestDistance = double.MaxValue;

        foreach (var feature in candidates)
        {
            foreach (var line in FlattenLines(feature.Geometry))
            {
                double distance = line.Distance(searchPoint);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestFeature = new GpkgFeature
                    {
                        Geometry = line,
                        Attributes = feature.Attributes
                    };
                }
            }
        }

        return (closestFeature, closestDistance);
    }

    private IEnumerable<LineString> FlattenLines(Geometry geometry)
    {
        if (geometry is LineString line)
            yield return line;

        else if (geometry is MultiLineString mline)
        {
            foreach (var geom in mline.Geometries)
                if (geom is LineString l)
                    yield return l;
        }
    }

    private Geometry ParseGeoPackageGeometry(byte[] blob)
    {
        if (blob.Length < 8)
            throw new Exception("Invalid geometry");

        byte flags = blob[3];
        int envelopeType = (flags >> 1) & 0x07;

        int envelopeSize = envelopeType switch
        {
            0 => 0,
            1 => 32,
            2 => 48,
            3 => 48,
            4 => 64,
            _ => throw new Exception("Unknown envelope type")
        };

        int wkbStart = 8 + envelopeSize;

        if (wkbStart >= blob.Length)
            throw new Exception("Invalid geometry length");

        byte[] wkb = new byte[blob.Length - wkbStart];
        Array.Copy(blob, wkbStart, wkb, 0, wkb.Length);

        return _wkbReader.Read(wkb);
    }

    public class GpkgFeature
    {
        public Geometry Geometry { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new();
    }
}