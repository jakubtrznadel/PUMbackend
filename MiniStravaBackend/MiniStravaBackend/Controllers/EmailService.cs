using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace MiniStravaBackend.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendResetPasswordEmailAsync(string to, string token)
        {
            var smtp = _config.GetSection("Smtp");

            var message = new MimeMessage();

            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = "Resetowanie hasła MiniStrava";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <h3>MiniStrava – Resetowanie hasła</h3>
                    <p>Otrzymaliśmy prośbę o zresetowanie hasła. Skopiuj poniższy token i wklej go w aplikacji mobilnej w ekranie resetowania hasła.</p>
                    <p><strong>Twój token:</strong></p>
                    <p style='font-family: monospace; background: #f4f4f4; padding: 10px; border-radius: 5px;'>{token}</p>
                    <p>Token jest ważny przez 1 godzinę.</p>"
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(smtp["Host"], 587, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtp["User"], smtp["Pass"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}