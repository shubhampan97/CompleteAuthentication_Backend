using CompleteAuthentication.Models;
using System.Net.Mail;

namespace CompleteAuthentication.Services
{
    public class MailService
    {
        public static readonly string smtpClient = "localhost";
        public static readonly int smtpPort = 1025;
        public static readonly string smtpEmail = "test@gmail.com";
        public static readonly string smtpName = "testing";

        public static async void SendPasswordResetEmailAsync(ResetToken token)
        {
            SmtpClient client = new(smtpClient)
            {
                Port = smtpPort,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = true,
                EnableSsl = false
            };

            MailMessage message = new MailMessage()
            {
                From = new MailAddress(smtpEmail, smtpName),
                Subject = "Reset Password",
                Body = $"Click <a href=\"http://localhost:4200/reset/{token.Token}\">here</a> to reset your password.",
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(token.Email));
            await client.SendMailAsync(message);
            message.Dispose();
        }
    }
}
