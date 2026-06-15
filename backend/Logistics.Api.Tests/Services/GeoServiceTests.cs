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

        decimal lat = 11.6643m;
        decimal lng = 78.1460m;

        var distance = _geoService.CalculateDistance(lat, lng, lat, lng);

        Assert.Equal(0.0, distance, precision: 4);
    }

    [Fact]
    public void CalculateDistance_KnownPoints_ReturnsExpectedDistance()
    {


        decimal lat1 = 11.664300m;
        decimal lng1 = 78.146000m;
        decimal lat2 = 11.681200m;
        decimal lng2 = 78.143500m;

        var distance = _geoService.CalculateDistance(lat1, lng1, lat2, lng2);


        Assert.InRange(distance, 1.85, 1.95);
    }
}
