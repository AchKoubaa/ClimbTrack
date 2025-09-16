using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Config
{
    public static class Config_template
    {
        // Credenziali Firebase
        public static string ApiKey = "YOUR_API_KEY_HERE";
        public static string AuthDomain = "YOUR_AUTH_DOMAIN_HERE";
        public static string DatabaseUrl = "https://climbtracknew-default.europe-west1.firebasedatabase.app";
        public static string ProjectId = "YOUR_PROJECT_ID_HERE";
        public static string StorageBucket = "YOUR_STORAGE_BUCKET_HERE";
        public static string MessagingSenderId = "YOUR_MESSAGING_SENDER_ID_HERE";
        public static string AppId = "YOUR_APP_ID_HERE";
        public static string MeasurementId = "YOUR_MEASUREMENT_ID_HERE";
        // URL specifici per l'autenticazione
        public static string SignUpUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signUp";
        public static string SignInUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword";
        public static string RefreshTokenUrl = "https://securetoken.googleapis.com/v1/token";
    }
}
