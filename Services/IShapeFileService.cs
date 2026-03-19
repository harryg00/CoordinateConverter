using CoordinateConverter.Models;

namespace CoordinateConverter.Services
{
    public interface IShapeFileService
    {
        List<ShapeFeature> CentreLineFeatures { get; }
        List<ShapeFeature> ELRFeatures { get; }
    }
}
