using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClimbTrack.Config
{
    public static class FirebaseConfig
    {
        // Valori predefiniti (usati come fallback)
        private static string _apiKey = "AIzaSyDm4BhJvBpW8F0INtzFpjMxqq1RBKrNrLg";
        private static string _authDomain = "climbtracknew.firebaseapp.com";
        private static string _databaseUrl = "https://climbtracknew-default-rtdb.europe-west1.firebasedatabase.app";
        private static string _projectId = "climbtracknew";
        private static string _storageBucket = "climbtracknew.firebasestorage.app";
        private static string _messagingSenderId = "703559950083";
        private static string _appId = "1:703559950083:web:7f70a9919a11de0819d336";
        private static string _measurementId = "G-NC3CTXJ2CZ";
        private static string _webClientId = "703559950083-kbfeqqi9evvmmgbn482ff65iaq5tchud.apps.googleusercontent.com";

        // Proprietà pubbliche per accedere alle configurazioni
        public static string ApiKey => _apiKey;
        public static string AuthDomain => _authDomain;
        public static string DatabaseUrl => _databaseUrl;
        public static string ProjectId => _projectId;
        public static string StorageBucket => _storageBucket;
        public static string MessagingSenderId => _messagingSenderId;
        public static string AppId => _appId;
        public static string MeasurementId => _measurementId;
        public static string WebClientId => _webClientId;

        // URL specifici per l'autenticazione
        public static string SignUpUrl => $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}";
        public static string SignInUrl => $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}";
        public static string RefreshTokenUrl => $"https://securetoken.googleapis.com/v1/token?key={_apiKey}";

        /// <summary>
        /// Inizializza la configurazione Firebase da un file JSON
        /// </summary>
        public static void InitializeFromJson(string configJson)
        {
            try
            {
                var config = JsonSerializer.Deserialize<FirebaseConfigModel>(configJson);

                if (config != null)
                {
                    _apiKey = config.ApiKey ?? _apiKey;
                    _authDomain = config.AuthDomain ?? _authDomain;
                    _databaseUrl = config.DatabaseUrl ?? _databaseUrl;
                    _projectId = config.ProjectId ?? _projectId;
                    _storageBucket = config.StorageBucket ?? _storageBucket;
                    _messagingSenderId = config.MessagingSenderId ?? _messagingSenderId;
                    _appId = config.AppId ?? _appId;
                    _measurementId = config.MeasurementId ?? _measurementId;
                    _webClientId = config.WebClientId ?? _webClientId;

                    Console.WriteLine("Firebase configurato da JSON");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante la configurazione Firebase da JSON: {ex.Message}");
                Console.WriteLine("Utilizzo della configurazione predefinita");
            }
        }

        /// <summary>
        /// Tenta di caricare la configurazione Firebase da un file
        /// </summary>
        public static async Task TryLoadConfigurationAsync()
        {
            try
            {
                // Prima prova a caricare da un file nella directory del progetto (utile per CI/CD)
                if (File.Exists("firebase-config.json"))
                {
                    string json = await File.ReadAllTextAsync("firebase-config.json");
                    InitializeFromJson(json);
                    return;
                }

                // Poi prova a caricare dalle risorse dell'app
                using var stream = await Microsoft.Maui.Storage.FileSystem.OpenAppPackageFileAsync("firebase-config.json");
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    string json = await reader.ReadToEndAsync();
                    InitializeFromJson(json);
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel caricamento della configurazione Firebase: {ex.Message}");
                Console.WriteLine("Utilizzo della configurazione predefinita");
            }
        }
    }

    // Classe per deserializzare la configurazione JSON
    internal class FirebaseConfigModel
    {
        public string ApiKey { get; set; }
        public string AuthDomain { get; set; }
        public string DatabaseUrl { get; set; }
        public string ProjectId { get; set; }
        public string StorageBucket { get; set; }
        public string MessagingSenderId { get; set; }
        public string AppId { get; set; }
        public string MeasurementId { get; set; }
        public string WebClientId { get; set; }
    }
}