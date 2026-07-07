using System.Threading.Tasks;
using Logistics.Api.Models;

namespace Logistics.Api.Interfaces.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByOrderIdAsync(string orderId);
    Task UpdateAsync(Payment payment);
}
