using static GpkgHelper;

namespace CoordinateConverter.Services
{
    public interface IGpkgHelper
    {
        public (GpkgFeature closestFeature, double closestDistance) FindClosest(double lat, double lon);
    }
}
