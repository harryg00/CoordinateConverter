namespace CoordinateConverter.Models
{
    public class TrackCentreline
    {
        public string AssetID { get; set; }
        public string ELR { get; set; }
        public string TrackType { get; set; }
        public string TrackID { get; set; }
        public string Owner { get; set; }
        public int Mileage { get; set; }
        public int Chainage { get; set; }
        public int Yardage { get; set; }
        public int TotalYardage { get; set; }
    }
}
