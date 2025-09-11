using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly bool _enableSsl;

        public SmtpEmailService(
            string smtpHost,
            int smtpPort,
            string smtpUsername,
            string smtpPassword,
            string fromEmail,
            string fromName,
            bool enableSsl = true)
        {
            _smtpHost = smtpHost;
            _smtpPort = smtpPort;
            _smtpUsername = smtpUsername;
            _smtpPassword = smtpPassword;
            _fromEmail = fromEmail;
            _fromName = fromName;
            _enableSsl = enableSsl;
        }

        public async Task SendVerificationCodeAsync(string email, string code)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = "Il tuo codice di verifica",
                Body = CreateEmailBody(code),
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            using var smtpClient = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = _enableSsl
            };

            await smtpClient.SendMailAsync(mailMessage);
        }

        private string CreateEmailBody(string code)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2>{_fromName} - Verifica il tuo indirizzo email</h2>
                    <p>Grazie per esserti registrato. Per completare la verifica, inserisci il seguente codice nell'app:</p>
                    <div style='background-color: #f4f4f4; padding: 15px; text-align: center; font-size: 24px; letter-spacing: 5px; font-weight: bold;'>
                        {code}
                    </div>
                    <p>Questo codice scadrà tra 15 minuti.</p>
                    <p>Se non hai richiesto questo codice, puoi ignorare questa email.</p>
                </div>";
        }
    }
}
