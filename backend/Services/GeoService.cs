using Logistics.Api.Interfaces.Services;

namespace Logistics.Api.Services;

public class GeoService : IGeoService
{
    public double CalculateDistance(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        const double r = 6371.0;

        var dLat = ToRadians((double)(lat2 - lat1));
        var dLng = ToRadians((double)(lng2 - lng1));

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Asin(Math.Min(1.0, Math.Sqrt(a)));

        return r * c;
    }

    private static double ToRadians(double val) => (Math.PI / 180) * val;
}
