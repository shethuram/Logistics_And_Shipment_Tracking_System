using Logistics.Api.Services;
using Xunit;

namespace Logistics.Api.Tests.Services;

public class GeoServiceTests
{
    private readonly GeoService _geoService;

    public GeoServiceTests()
    {
        _geoService = new GeoService();
    }

    [Fact]
    public void CalculateDistance_SamePoints_ReturnsZero()
    {
        // Arrange
        decimal lat = 11.6643m;
        decimal lng = 78.1460m;

        // Act
        var distance = _geoService.CalculateDistance(lat, lng, lat, lng);

        // Assert
        Assert.Equal(0.0, distance, precision: 4);
    }

    [Fact]
    public void CalculateDistance_KnownPoints_ReturnsExpectedDistance()
    {
        // Arrange
        // Salem Junction (11.6643, 78.1460) to Five Roads Salem (11.6812, 78.1435)
        decimal lat1 = 11.664300m;
        decimal lng1 = 78.146000m;
        decimal lat2 = 11.681200m;
        decimal lng2 = 78.143500m;

        // Act
        var distance = _geoService.CalculateDistance(lat1, lng1, lat2, lng2);

        // Assert
        // The distance is approximately 1.89 km
        Assert.InRange(distance, 1.85, 1.95);
    }
}
