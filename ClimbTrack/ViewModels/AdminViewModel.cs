
using ClimbTrack.Services;
using ClimbTrack.ViewModels;
using System.Windows.Input;
namespace ClimbTrack.ViewModels
{
    public class AdminViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private string _statusMessage;
        private bool _isSuccess;
        private string _lastUpdateTime;

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            set => SetProperty(ref _isSuccess, value);
        }

        public string LastUpdateTime
        {
            get => _lastUpdateTime;
            set => SetProperty(ref _lastUpdateTime, value);
        }

        public ICommand InitializeDatabaseCommand { get; }
        public ICommand BackupDatabaseCommand { get; }
        public ICommand RestoreDatabaseCommand { get; }
        public ICommand ClearTestDataCommand { get; }

        public AdminViewModel(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
            Title = "Amministrazione";
            LastUpdateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            InitializeDatabaseCommand = new Command(async () => await InitializeDatabase());
            BackupDatabaseCommand = new Command(async () => await BackupDatabase());
            RestoreDatabaseCommand = new Command(async () => await RestoreDatabase());
            ClearTestDataCommand = new Command(async () => await ClearTestData());
        }

        private async Task InitializeDatabase()
        {
            await ExecuteWithBusy(async () =>
            {
                try
                {
                    StatusMessage = "Inizializzazione del database in corso...";

                    await _databaseService.InitializeDatabaseAsync();

                    IsSuccess = true;
                    StatusMessage = "Database inizializzato con successo!";
                    LastUpdateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                }
                catch (Exception ex)
                {
                    IsSuccess = false;
                    StatusMessage = $"Errore: {ex.Message}";
                    Console.WriteLine($"Error initializing database: {ex}");
                }
            });
        }

        private async Task BackupDatabase()
        {
            await ExecuteWithBusy(async () =>
            {
                try
                {
                    StatusMessage = "Backup del database in corso...";

                    // Implementare la logica di backup
                    await Task.Delay(2000); // Simulazione

                    IsSuccess = true;
                    StatusMessage = "Backup completato con successo!";
                }
                catch (Exception ex)
                {
                    IsSuccess = false;
                    StatusMessage = $"Errore durante il backup: {ex.Message}";
                    Console.WriteLine($"Error backing up database: {ex}");
                }
            });
        }

        private async Task RestoreDatabase()
        {
            await ExecuteWithBusy(async () =>
            {
                try
                {
                    StatusMessage = "Ripristino del database in corso...";

                    // Implementare la logica di ripristino
                    await Task.Delay(2000); // Simulazione

                    IsSuccess = true;
                    StatusMessage = "Ripristino completato con successo!";
                    LastUpdateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                }
                catch (Exception ex)
                {
                    IsSuccess = false;
                    StatusMessage = $"Errore durante il ripristino: {ex.Message}";
                    Console.WriteLine($"Error restoring database: {ex}");
                }
            });
        }

        private async Task ClearTestData()
        {
            await ExecuteWithBusy(async () =>
            {
                try
                {
                    StatusMessage = "Cancellazione dei dati di test in corso...";

                    // Implementare la logica di cancellazione
                    await Task.Delay(2000); // Simulazione

                    IsSuccess = true;
                    StatusMessage = "Dati di test cancellati con successo!";
                }
                catch (Exception ex)
                {
                    IsSuccess = false;
                    StatusMessage = $"Errore durante la cancellazione: {ex.Message}";
                    Console.WriteLine($"Error clearing test data: {ex}");
                }
            });
        }
    }
}