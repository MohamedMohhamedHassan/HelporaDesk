using System.IO;

namespace ServiceCore.Services
{
    public class MockEmailService : IEmailService
    {
        private readonly string _logPath = "email_log.txt";

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var emailContent = $"--- EMAIL SENT AT {DateTime.Now} ---\n" +
                               $"To: {to}\n" +
                               $"Subject: {subject}\n" +
                               $"Body:\n{body}\n" +
                               $"------------------------------------\n\n";

            await File.AppendAllTextAsync(_logPath, emailContent);
        }
    }
}
