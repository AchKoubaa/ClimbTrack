namespace ClimbTrack.Config
{
    public static class FirebaseConfig
    {
        // Note: These Firebase configuration values are designed to be public
        // Security is enforced through Firebase Security Rules and authentication
        // Credenziali Firebase
        public static string ApiKey = "AIzaSyDm4BhJvBpW8F0INtzFpjMxqq1RBKrNrLg";
        public static string AuthDomain = "climbtracknew.firebaseapp.com";
        public static string DatabaseUrl = "https://climbtracknew-default-rtdb.europe-west1.firebasedatabase.app";
        public static string ProjectId = "climbtracknew";
        public static string StorageBucket = "climbtracknew.firebasestorage.app";
        public static string MessagingSenderId = "703559950083";
        public static string AppId = "1:703559950083:web:7f70a9919a11de0819d336";
        public static string MeasurementId = "G-NC3CTXJ2CZ";
        public static string WebClientId = "703559950083-kbfeqqi9evvmmgbn482ff65iaq5tchud.apps.googleusercontent.com";
        // URL specifici per l'autenticazione
        public static string SignUpUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signUp";
        public static string SignInUrl = "https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword";
        public static string RefreshTokenUrl = "https://securetoken.googleapis.com/v1/token";
    }
}
