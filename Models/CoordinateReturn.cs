namespace CoordinateConverter.Models
{
    public class CoordinateReturn
    {
        public double? DistanceAwayInMetres { get; set; }
        public Original Original { get; set; }
        public LocationReturn LocationReturn { get; set; }
        public TrackCentreline TrackCentrelineInfo { get; set; }
    }

    public class Original
    {
        public double? Easting { get; set; }
        public double? Northing { get; set; }
        public string CoordinateReferenceSystem { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? ELR { get; set; }
        public int? Mileage { get; set; }
        public int? Chainage { get; set; }
        public int? Yardage { get; set; }
    }
}
