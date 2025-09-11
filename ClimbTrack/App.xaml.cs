using ClimbTrack.Services;
using ClimbTrack.ViewModels;
using ClimbTrack.Views;
using System.Diagnostics;

namespace ClimbTrack
{
    public partial class App : Application
    {
        private readonly IAuthService _authService;
        private readonly IDatabaseService _databaseService;
        private readonly INavigationService _navigationService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IFirebaseService _firebaseService;

        public App(AppShell appShell,
            IAuthService authService, 
            IDatabaseService databaseService,
            INavigationService navigationService, 
            IGoogleAuthService googleAuthService, 
            IFirebaseService firebaseService)
        {
            InitializeComponent();
            _authService = authService;
            _databaseService = databaseService;
            _navigationService = navigationService;
            _googleAuthService = googleAuthService;

            // Verifica se l'utente è autenticato
            var user = _authService.GetCurrentUser();

            if (user == null)
            {
                // Nessun utente autenticato, mostra la pagina di login
                // Crea un'istanza di LoginViewModel
                var loginViewModel = new LoginViewModel(
                    _authService,
                    _navigationService,
                    _googleAuthService,
                    _databaseService, 
                    _firebaseService);

                MainPage = new NavigationPage(new LoginPage(loginViewModel));
            }
            else
            {
                // Utente già autenticato, mostra la shell principale
                MainPage = appShell;
            }

            _firebaseService = firebaseService;
        }


        protected override async void OnStart()
        {
            base.OnStart();

            // Inizializza l'autenticazione e il database
            await InitializeAppAsync();
        }

        private async Task InitializeAppAsync()
        {
            try
            {
                // Verifica se c'è un utente già autenticato
                var user = _authService.GetCurrentUser();

                if (user == null)
                {
                    // Nessun utente autenticato, reindirizza alla pagina di login
                    Debug.WriteLine("Nessun utente autenticato. Reindirizzamento alla pagina di login...");

                    // Crea un'istanza di LoginViewModel
                    var loginViewModel = new LoginViewModel(
                        _authService,
                        _navigationService,
                        _googleAuthService,
                        _databaseService,
                        _firebaseService);

                    // Crea una nuova istanza di LoginPage
                    var loginPage = new LoginPage(loginViewModel);

                    // Imposta la pagina di login come pagina principale
                    MainPage = new NavigationPage(loginPage);
                }
                else
                {
                    // Utente già autenticato, continua con l'inizializzazione normale
                    Debug.WriteLine($"Utente già autenticato: {user.Info?.Email ?? user.Uid}");

                    // Inizializza il database
                    await InitializeDatabaseAsync();
                }

                // In modalità debug, verifica se il database necessita di seeding
#if DEBUG
                await CheckAndSeedDatabaseAsync();
#else
                // In produzione, inizializza il database senza mostrare messaggi
                await _databaseService.InitializeDatabaseAsync();
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore durante l'inizializzazione dell'app: {ex.Message}");
                await MainPage.DisplayAlert("Errore", "Si è verificato un errore durante l'inizializzazione dell'app. Alcune funzionalità potrebbero non essere disponibili.", "OK");
            }
        }
        private async Task InitializeDatabaseAsync()
        {
            try
            {
                // In modalità debug, verifica se il database necessita di seeding
#if DEBUG
                await CheckAndSeedDatabaseAsync();
#else
        // In produzione, inizializza il database senza mostrare messaggi
        await _databaseService.InitializeDatabaseAsync();
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore durante l'inizializzazione del database: {ex.Message}");
                await MainPage.DisplayAlert("Errore", "Si è verificato un errore durante l'inizializzazione dell'app. Alcune funzionalità potrebbero non essere disponibili.", "OK");
            }
        }

        private async Task CheckAndSeedDatabaseAsync()
        {
            try
            {
                bool needsSeeding = await _databaseService.CheckIfDatabaseNeedsSeedingAsync();

                if (needsSeeding)
                {
                    // Mostra un indicatore di caricamento solo se è necessario il seeding
                    await MainPage.DisplayAlert("Inizializzazione", "Preparazione del database in corso...", "OK");

                    // Avvia il seeding
                    await _databaseService.SeedDatabaseIfNeeded();

                    await MainPage.DisplayAlert("Completato", "Database inizializzato con successo!", "OK");
                }
                else
                {
                    Debug.WriteLine("Il database è già popolato, seeding non necessario.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking/seeding database: {ex.Message}");
                await MainPage.DisplayAlert("Errore", $"Impossibile inizializzare il database: {ex.Message}", "OK");
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            LogException(exception, "Unhandled AppDomain Exception");
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException(e.Exception, "Unobserved Task Exception");
            e.SetObserved(); // Impedisce che l'app si chiuda
        }

        private void LogException(Exception exception, string source)
        {
            if (exception == null) return;

            Debug.WriteLine($"ERRORE CRITICO ({source}): {exception.Message}");
            Debug.WriteLine($"Stack Trace: {exception.StackTrace}");

            // Qui puoi aggiungere la logica per inviare l'errore a un servizio di logging
            // o mostrare un messaggio all'utente
        }
    }
}