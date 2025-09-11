using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public class MockEmailService : IEmailService
    {
        public Task SendVerificationCodeAsync(string email, string code)
        {
            Console.WriteLine($"[EMAIL SIMULATA] Invio codice {code} a {email}");

            // Mostra il codice in un popup di debug
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Codice di Verifica (SIMULATO)",
                    $"Il codice per {email} è: {code}",
                    "OK");
            });

            return Task.CompletedTask;
        }

    }
}
