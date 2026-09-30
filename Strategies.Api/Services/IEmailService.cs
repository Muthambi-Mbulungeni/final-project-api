using System.Threading.Tasks;

namespace Strategies.Api.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
        Task SendAccountConfirmationEmailAsync(string toEmail, string confirmationLink);
    }
}
