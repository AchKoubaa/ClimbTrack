using SendGrid;
using SendGrid.Helpers.Mail;

namespace ClimbTrack.Services
{
   
    public class SendGridEmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public SendGridEmailService(string apiKey, string fromEmail, string fromName)
        {
            _apiKey = apiKey;
            _fromEmail = fromEmail;
            _fromName = fromName;
        }

        public async Task SendVerificationCodeAsync(string email, string code)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var subject = "Il tuo codice di verifica ClimbTrack";
            var to = new EmailAddress(email);
            var plainTextContent = $"Il tuo codice di verifica è: {code}";
            var htmlContent = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2>ClimbTrack - Verifica il tuo indirizzo email</h2>
                    <p>Grazie per esserti registrato a ClimbTrack. Per completare la verifica, inserisci il seguente codice nell'app:</p>
                    <div style='background-color: #f4f4f4; padding: 15px; text-align: center; font-size: 24px; letter-spacing: 5px; font-weight: bold;'>
                        {code}
                    </div>
                    <p>Questo codice scadrà tra 15 minuti.</p>
                    <p>Se non hai richiesto questo codice, puoi ignorare questa email.</p>
                </div>";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            await client.SendEmailAsync(msg);
        }
    }
}
