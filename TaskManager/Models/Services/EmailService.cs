using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
namespace TaskManager.Models.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string plainTextMessage, string? htmlContent = null)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_configuration["SmtpSettings:SenderName"], _configuration["SmtpSettings:SenderEmail"]));
            emailMessage.To.Add(new MailboxAddress("", toEmail));
            emailMessage.Subject = subject;
            var bodyBuilder = new BodyBuilder {
                TextBody = plainTextMessage, // Plain text fallback
                HtmlBody = htmlContent        // Optional HTML content
            };

            emailMessage.Body = bodyBuilder.ToMessageBody();
            try {
                using (var client = new SmtpClient()) {
                await client.ConnectAsync(_configuration["SmtpSettings:Server"], int.Parse(_configuration["SmtpSettings:Port"]), MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_configuration["SmtpSettings:Username"], _configuration["SmtpSettings:Password"]);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
                }
                return true;
            } catch (Exception) {
                return false;
            }
            
        }
    }
}