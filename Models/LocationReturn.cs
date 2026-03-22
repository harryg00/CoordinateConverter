namespace CoordinateConverter.Models
{
    public class LocationReturn
    {
        public EastingNorthing EastingNorthing { get; set; }
        public LatitudeLongitude LatitudeLongitude { get; set; }
        public ELRMileChain ELRMileChain { get; set; }
    }

    public class EastingNorthing
    {
        public double Easting { get; set; }
        public double Northing { get; set; }
        public string CoordinateReferenceSystem { get; set; }
    }

    public class LatitudeLongitude
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class ELRMileChain
    {
        public string ELR { get; set; }
        public int Mileage { get; set; }
        public int Chainage { get; set; }
        public int Yardage { get; set; }
    }
}
