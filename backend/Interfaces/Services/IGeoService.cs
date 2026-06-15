namespace Logistics.Api.Interfaces.Services;

public interface IGeoService
{
    double CalculateDistance(decimal lat1, decimal lng1, decimal lat2, decimal lng2);
}
