using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Strategies.Api.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var subject = "Nexus Strategies - Password Reset Request";
            var body = $@"
                <p>Hello,</p>
                <p>We received a request to reset your password for your Nexus Strategies account.</p>
                <p><a href=""{resetLink}"" style=""display:inline-block;padding:10px 20px;background-color:#2563eb;color:#ffffff;text-decoration:none;border-radius:6px;"">Reset Password</a></p>
                <p>If you did not request a password reset, please ignore this email.</p>
                <p>Best regards,<br/>The Nexus Strategies Team</p>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendAccountConfirmationEmailAsync(string toEmail, string confirmationLink)
        {
            var subject = "Nexus Strategies - Confirm Your Account";
            var body = $@"
                <p>Welcome to Nexus Strategies!</p>
                <p>Please confirm your account by clicking the link below:</p>
                <p><a href=""{confirmationLink}"" style=""display:inline-block;padding:10px 20px;background-color:#16a34a;color:#ffffff;text-decoration:none;border-radius:6px;"">Confirm Account</a></p>
                <p>Best regards,<br/>The Nexus Strategies Team</p>";

            await SendEmailAsync(toEmail, subject, body);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var host = _configuration["Smtp:Host"];
            var portStr = _configuration["Smtp:Port"];
            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];
            var fromEmail = _configuration["Smtp:FromEmail"] ?? "noreply@nexusstrategies.com";
            var fromName = _configuration["Smtp:FromName"] ?? "Nexus Strategies";

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogInformation("SMTP not configured (Smtp:Host, Smtp:Username, or Smtp:Password missing). Email to {Email} was simulated. Subject: {Subject}", toEmail, subject);
                return;
            }

            int port = int.TryParse(portStr, out var p) ? p : 587;

            try
            {
                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(username, password)
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Successfully sent email to {Email}. Subject: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                // In production, do not break public caller if email transport encounters transient issues
            }
        }
    }
}
