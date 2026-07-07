using System.Threading.Tasks;
using Logistics.Api.Data;
using Logistics.Api.Interfaces.Repositories;
using Logistics.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Logistics.Api.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _db;

    public PaymentRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Payment?> GetByOrderIdAsync(string orderId)
    {
        return _db.Payments
            .Include(p => p.Shipment)
            .FirstOrDefaultAsync(p => p.RazorpayOrderId == orderId);
    }

    public async Task UpdateAsync(Payment payment)
    {
        _db.Payments.Update(payment);
        await _db.SaveChangesAsync();
    }
}
