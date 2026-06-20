using System.Threading.Tasks;

namespace Logistics.Api.Interfaces.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}
