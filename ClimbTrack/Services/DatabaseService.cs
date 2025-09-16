using ClimbTrack.Config;
using ClimbTrack.Models;
using Firebase.Database;
using Firebase.Database.Query;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace ClimbTrack.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly FirebaseClient _databaseClient;
        private readonly IFirebaseService _firebaseService;
        private readonly bool _useMockDatabase = false;
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly IConnectivity _connectivity;

        public DatabaseService(
            IErrorHandlingService errorHandlingService, 
            IFirebaseService firebase,IAuthService authService, 
            INavigationService navigationService,
            IConnectivity connectivity = null)
        {
            _errorHandlingService = errorHandlingService;
            _firebaseService = firebase;
            _authService = authService;
            _navigationService = navigationService;
            _connectivity = connectivity ?? Connectivity.Current;

            // Inizializza il client del database solo se non stiamo usando dati fittizi
            if (!_useMockDatabase)
            {
                try
                {
                    _databaseClient = new FirebaseClient(FirebaseConfig.DatabaseUrl);
                    Console.WriteLine("Firebase Database client initialized successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error initializing Firebase Database client: {ex.Message}");
                    // Non lanciare l'eccezione, useremo i dati fittizi
                    _useMockDatabase = true;
                }
            }

          
        }

        public async Task<string> AddItem<T>(string nodePath, T item)
        {
            if (_useMockDatabase)
            {
                Console.WriteLine($"Mock: Adding item to {nodePath}");
                return Guid.NewGuid().ToString(); // Simula un ID generato
            }

            try
            {
                // Get an authenticated client from the FirebaseService
                var client = await _authService.GetAuthenticatedClientAsync();
                if (client == null)
                {
                    // Instead of throwing, handle it properly
                    await _errorHandlingService.HandleAuthenticationExceptionAsync(
                        new UnauthorizedAccessException("User must be authenticated to update items"),
                        $"DatabaseService.AddItem<{typeof(T).Name}>({nodePath}, {item})");
                }

                var result = await client
                    .Child(nodePath)
                    .PostAsync(JsonConvert.SerializeObject(item));

                return result.Key;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding item: {ex.Message}");
                await _errorHandlingService.HandleExceptionAsync(ex, $"DatabaseService.AddItem<{typeof(T).Name}>({nodePath})", false);
                return null;
            }
        }

        public async Task<bool> UpdateItem<T>(string nodePath, string itemId, T item)
        {
            if (_useMockDatabase)
            {
                Console.WriteLine($"Mock: Updating item {itemId} in {nodePath}");
                return true;
            }

            try
            {
                // Get an authenticated client from the FirebaseService
                var client = await _authService.GetAuthenticatedClientAsync();
                if (client == null)
                {
                    // Instead of throwing, handle it properly
                    await _errorHandlingService.HandleAuthenticationExceptionAsync(
                        new UnauthorizedAccessException("User must be authenticated to update items"),
                        $"DatabaseService.UpdateItem<{typeof(T).Name}>({nodePath}, {itemId})"); ;
                }

                await client
                    .Child(nodePath)
                    .Child(itemId)
                    .PutAsync(JsonConvert.SerializeObject(item));
                return true;
            }
            catch (Exception ex)
            {
                // Handle other exceptions normally
                await _errorHandlingService.HandleExceptionAsync(
                    ex,
                    $"DatabaseService.UpdateItem<{typeof(T).Name}>({nodePath}, {itemId})",
                    false);
                return false;
            }
        }

        public async Task<T> GetItem<T>(string nodePath, string itemId)
        {
            if (_useMockDatabase)
            {
                Console.WriteLine($"Mock: Getting item {itemId} from {nodePath}");
                return GetMockItem<T>(nodePath, itemId);
            }

            try
            {
                // Get an authenticated client from the AuthService
                var client = await _authService.GetAuthenticatedClientAsync();

                // If we couldn't get an authenticated client, use mock data
                if (client == null)
                {
                    // Instead of throwing, handle it properly
                    await _errorHandlingService.HandleAuthenticationExceptionAsync(
                        new UnauthorizedAccessException("User must be authenticated to update items"),
                        $"DatabaseService.GetItem<{typeof(T).Name}>({nodePath}, {itemId})"); ;
                    Debug.WriteLine("Failed to create authenticated client. Using mock data.");
                    return GetMockItem<T>(nodePath, itemId);
                }

                // Query the database using the authenticated client
                var item = await client
                    .Child(nodePath)
                    .Child(itemId)
                    .OnceSingleAsync<T>();
                return item;
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleExceptionAsync(
           ex,
           $"DatabaseService.GetItem<{typeof(T).Name}>({nodePath}, {itemId})",
           false);
                Console.WriteLine($"Error getting item: {ex.Message}");
                return default;
            }
        }
        public async Task<ObservableCollection<T>> GetItems<T>(string nodePath)
        {
            if (_useMockDatabase)
            {
                Debug.WriteLine($"Using mock data for {nodePath}");
                return GetMockItems<T>(nodePath);
            }

            try
            {
              

                // Get an authenticated client from the FirebaseService
                var client = await _authService.GetAuthenticatedClientAsync();
                if (client == null)
                {
                    Debug.WriteLine("Failed to create authenticated client. Using mock data.");
                    return GetMockItems<T>(nodePath);
                }

                Debug.WriteLine($"Attempting to access path: {nodePath}");

                // Execute query with timeout using the authenticated client
                var items = await client
                    .Child(nodePath)
                    .OnceAsync<T>(TimeSpan.FromSeconds(30));

                Debug.WriteLine($"Query completed successfully. Items found: {items.Count}");

                var collection = new ObservableCollection<T>();
                foreach (var item in items)
                {
                    var itemWithKey = item.Object;
                    // If T has an Id property, try to set it
                    var idProperty = typeof(T).GetProperty("Id");
                    idProperty?.SetValue(itemWithKey, item.Key);

                    collection.Add(itemWithKey);
                }

                return collection;
            }
            catch (Firebase.Database.FirebaseException fbEx)
            {
                Debug.WriteLine($"Firebase specific error: {fbEx.Message}");
                Debug.WriteLine($"Response: {fbEx.ResponseData}");
                return GetMockItems<T>(nodePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"General error in GetItems: {ex.GetType().Name}: {ex.Message}");
                return GetMockItems<T>(nodePath);
            }
        }

        public async Task<ObservableCollection<string>> GetChildKeys(string nodePath)
        {
            

            try
            {
                // Get an authenticated client from the FirebaseService
                var client = await _authService.GetAuthenticatedClientAsync();
                if (client == null)
                {
                    Debug.WriteLine("Failed to create authenticated client. Using mock data.");
                    return null;
                }

                Debug.WriteLine($"Attempting to get keys at path: {nodePath}");

                // Get the raw data as a dictionary
                var response = await client
                    .Child(nodePath)
                    .OnceSingleAsync<Dictionary<string, object>>();

                var keys = new ObservableCollection<string>();
                if (response != null)
                {
                    foreach (var key in response.Keys)
                    {
                        keys.Add(key);
                    }
                }

                Debug.WriteLine($"Found {keys.Count} keys at {nodePath}");
                return keys;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting keys: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteItem(string nodePath, string itemId)
        {
            if (_useMockDatabase)
            {
                Console.WriteLine($"Mock: Deleting item {itemId} from {nodePath}");
                return true;
            }

            try
            {
                // Get an authenticated client from the FirebaseService
                var client = await _authService.GetAuthenticatedClientAsync();
                if (client == null)
                {
                    // Instead of throwing, handle it properly
                    await _errorHandlingService.HandleAuthenticationExceptionAsync(
                        new UnauthorizedAccessException("User must be authenticated to update items"),
                        $"DatabaseService.DeleteItem<{typeof(string).Name}>({nodePath}, {itemId})"); ;
                    
                }

                await client
                    .Child(nodePath)
                    .Child(itemId)
                    .DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting item: {ex.Message}");
                return false;
            }
        }
        public async Task InitializeDatabaseAsync()
        {
            if (_useMockDatabase)
            {
                Console.WriteLine("Using mock database, skipping initialization");
                return;
            }

            try
            {
                // Verifica se l'utente è autenticato
                bool isAuthenticated = await _authService.IsAuthenticated();

                if (!isAuthenticated)
                {
                    Debug.WriteLine("Utente non autenticato. Impossibile inizializzare il database.");
                    return;
                }

                // Verifica se il database necessita di seeding
                bool needsSeeding = await CheckIfDatabaseNeedsSeedingAsync();

                if (needsSeeding)
                {
                    // Popola il database con dati dai file JSON
                    await SeedDatabaseFromJsonAsync();
                }
                else
                {
                    Console.WriteLine("Il database è già popolato, seeding non necessario.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante l'inizializzazione del database: {ex.Message}");
                await _errorHandlingService.HandleExceptionAsync(ex, "DatabaseService.InitializeDatabaseAsync",true);
            }
        }

        public async Task SeedDatabaseIfNeeded()
        {
            if (_useMockDatabase)
            {
                Console.WriteLine("Using mock database, skipping seeding");
                return;
            }

            try
            {
                // Verifica se l'utente è autenticato
                bool isAuthenticated = await _authService.IsAuthenticated();

                if (!isAuthenticated)
                {
                    Debug.WriteLine("Utente non autenticato. Impossibile eseguire il seeding del database.");
                    return;
                }

                // Verifica se il database è già popolato
                bool databaseNeedsSeeding = await CheckIfDatabaseNeedsSeedingAsync();

                if (databaseNeedsSeeding)
                {
                    await SeedDatabaseFromJsonAsync();
                }
                else
                {
                    Console.WriteLine("Il database è già popolato, seeding non necessario.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding database: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> CheckIfDatabaseNeedsSeedingAsync()
        {
            if (_useMockDatabase)
            {
                return false;
            }

            try
            {
                // Get an authenticated client from the FirebaseService
                var client = await _authService.GetAuthenticatedClientAsync();
                if (client == null)
                {
                    Debug.WriteLine("Utente non autenticato. Impossibile verificare il database.");
                    return false; // Assumiamo che il database necessiti di seeding
                }

                // Controlla se esistono già percorsi nel database
                var routes = await client
                    .Child("routes")
                    .Child("Verticale")
                    .OrderBy("name")
                    .LimitToFirst(1)
                    .OnceAsync<object>();

                // Se non ci sono percorsi, il database necessita di seeding
                return !routes.Any();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore durante la verifica del database: {ex.Message}");
                // In caso di errore, assumiamo che il database necessiti di seeding
                return true;
            }
        }

        private async Task SeedDatabaseFromJsonAsync()
        {
            if (_useMockDatabase)
            {
                Console.WriteLine("Using mock database, skipping JSON seeding");
                return;
            }

           // FirebaseClient seedingClient = null;

            try
            {
                // Get an authenticated client using the AuthService
                var seedingClient = await _authService.GetAuthenticatedClientAsync();

                // If we couldn't get an authenticated client, prompt the user to login
                if (seedingClient == null)
                {
                   await _navigationService.DisplayAlertAsync(
                        "Autenticazione Richiesta",
                        "È necessario effettuare l'accesso per inizializzare il database. Vuoi accedere ora?",
                        "Ok");

                   
                        // Navigate to the login page
                        await _navigationService.NavigateToLoginPage();
                    
                    // Return early as we can't proceed without authentication
                    return;
                }

                Console.WriteLine("Inizializzazione del database con dati dai file JSON...");

                // Crea la struttura base del database
                await CreateDatabaseStructureAsync(seedingClient);

                // Verifica e popola le varie "tabelle" del database
                if (await IsJsonResourceAvailable("routes.json"))
                    await SeedRoutesFromJsonAsync(seedingClient);
                else
                    Console.WriteLine("File routes.json non trovato, seeding dei percorsi saltato.");

                if (await IsJsonResourceAvailable("users.json"))
                    await SeedUserProfilesFromJsonAsync(seedingClient);
                else
                    Console.WriteLine("File users.json non trovato, seeding dei profili utente saltato.");

                if (await IsJsonResourceAvailable("gyms.json"))
                    await SeedGymsFromJsonAsync(seedingClient);
                else
                    Console.WriteLine("File gyms.json non trovato, seeding delle palestre saltato.");

                if (await IsJsonResourceAvailable("trainingSessions.json"))
                    await SeedTrainingSessionsFromJsonAsync(seedingClient);
                else
                    Console.WriteLine("File trainingSessions.json non trovato, seeding delle sessioni di allenamento saltato.");

                if (await IsJsonResourceAvailable("userModels.json"))
                    await SeedUserModelsFromJsonAsync(seedingClient);
                else
                    Console.WriteLine("File userModels.json non trovato, seeding dei modelli utente saltato.");

                Console.WriteLine("Database popolato con successo!");
            }
            catch (Firebase.Database.FirebaseException ex)
            {
                Console.WriteLine($"Firebase Exception: {ex.Message}");
                Console.WriteLine($"Error Details: {ex.ResponseData}");
                Console.WriteLine($"Status Code: {ex.StatusCode}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il seeding del database: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task CreateDatabaseStructureAsync(FirebaseClient client)
        {
            Console.WriteLine("Creazione della struttura base del database...");

            // Definisci i nodi principali del database
            var databaseStructure = new Dictionary<string, object>
            {
                { "users", null },
                { "userProfiles", null },
                { "routes", null },
                { "trainingSessions", null },
                { "gyms", null },
                { "system", new { version = 1, lastUpdate = DateTime.UtcNow.ToString("o") } }
            };

            // Crea i nodi principali
            foreach (var node in databaseStructure.Keys)
            {
                try
                {
                    await client
                        .Child(node)
                        .PutAsync(databaseStructure[node]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Errore durante la creazione del nodo {node}: {ex.Message}");
                    // Continua con gli altri nodi anche se uno fallisce
                }
            }

            Console.WriteLine("Struttura base del database creata con successo");
        }

        private async Task<T> ReadJsonResource<T>(string resourceName)
        {
            try
            {
                Console.WriteLine($"Lettura del file JSON: {resourceName}");

                using var stream = await FileSystem.OpenAppPackageFileAsync(resourceName);
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante la lettura del file JSON {resourceName}: {ex.Message}");
                throw;
            }
        }

        private async Task<bool> IsJsonResourceAvailable(string resourceName)
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync(resourceName);
                return stream != null;
            }
            catch
            {
                return false;
            }
        }


        private async Task SeedRoutesFromJsonAsync(FirebaseClient client)
        {
            try
            {
                Console.WriteLine("Popolamento dei percorsi dal file JSON...");

                // Leggi i dati dal file JSON
                var routesData = await ReadJsonResource<RoutesData>("routes.json");

                if (routesData?.Panels == null || !routesData.Panels.Any())
                {
                    Console.WriteLine("Nessun pannello trovato nel file routes.json");
                    return;
                }

                // Per ogni pannello nel file JSON
                foreach (var panel in routesData.Panels)
                {
                    if (string.IsNullOrEmpty(panel.Name) || panel.Routes == null || !panel.Routes.Any())
                        continue;

                    Console.WriteLine($"Creazione percorsi per pannello: {panel.Name}");

                    // Crea un batch di percorsi per questo pannello
                    var routesBatch = new Dictionary<string, ClimbingRoute>();

                    foreach (var routeData in panel.Routes)
                    {
                        var routeId = Guid.NewGuid().ToString();
                        var route = new ClimbingRoute
                        {
                            Id = routeId,
                            Name = routeData.Name,
                            Color = routeData.Color,
                            ColorHex = routeData.ColorHex,
                            Difficulty = routeData.Difficulty,
                            PanelType = panel.Name,
                            CreatedDate = string.IsNullOrEmpty(routeData.CreatedDate) ? DateTime.UtcNow : DateTime.Parse(routeData.CreatedDate),
                            IsActive = routeData.IsActive
                        };

                        routesBatch.Add(routeId, route);
                    }

                    // Salva tutti i percorsi per questo pannello in un'unica operazione
                    if (routesBatch.Any())
                    {
                        await client
                            .Child("routes")
                            .Child(panel.Name)
                            .PutAsync(routesBatch);

                        Console.WriteLine($"Aggiunti {routesBatch.Count} percorsi al pannello {panel.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il seeding dei percorsi: {ex.Message}");
                throw;
            }
        }

        private async Task SeedUserProfilesFromJsonAsync(FirebaseClient client)
        {
            try
            {
                Console.WriteLine("Popolamento dei profili utente dal file JSON...");

                // Leggi i dati dal file JSON
                var usersData = await ReadJsonResource<UsersData>("users.json");

                if (usersData?.Users == null || !usersData.Users.Any())
                {
                    Console.WriteLine("Nessun utente trovato nel file users.json");
                    return;
                }

                // Salva i profili utente
                foreach (var userData in usersData.Users)
                {
                    var userProfile = new UserProfile
                    {
                        Id = userData.Id,
                        DisplayName = userData.DisplayName,
                        Email = userData.Email,
                        PhotoUrl = userData.PhotoUrl,
                        Bio = userData.Bio,
                        PreferredGym = userData.PreferredGym,
                        CreatedAt = DateTime.Parse(userData.CreatedAt),
                        LastLoginAt = DateTime.Parse(userData.LastLoginAt)
                    };

                    await client
                        .Child("userProfiles")
                        .Child(userProfile.Id)
                        .PutAsync(userProfile);
                }

                Console.WriteLine($"Aggiunti {usersData.Users.Count} profili utente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il seeding dei profili utente: {ex.Message}");
                throw;
            }
        }

        private async Task SeedGymsFromJsonAsync(FirebaseClient client)
        {
            try
            {
                Console.WriteLine("Popolamento delle palestre dal file JSON...");

                // Leggi i dati dal file JSON
                var gymsData = await ReadJsonResource<GymsData>("gyms.json");

                if (gymsData?.Gyms == null || !gymsData.Gyms.Any())
                {
                    Console.WriteLine("Nessuna palestra trovata nel file gyms.json");
                    return;
                }

                // Salva le palestre
                var gymsBatch = new Dictionary<string, object>();
                foreach (var gymData in gymsData.Gyms)
                {
                    var gym = new Gym
                    {
                        Id = gymData.Id,
                        Name = gymData.Name,
                        Address = gymData.Address,
                        Location = new GeoPoint
                        {
                            Latitude = gymData.Location.Latitude,
                            Longitude = gymData.Location.Longitude
                        },
                        HasVerticalWall = gymData.HasVerticalWall,
                        HasOverhangWall = gymData.HasOverhangWall,
                        Website = gymData.Website,
                        PhoneNumber = gymData.PhoneNumber,
                        ImageUrl = gymData.ImageUrl,
                        Amenities = gymData.Amenities,
                        OpeningHours = gymData.OpeningHours
                    };

                    gymsBatch.Add(gym.Id, gym);
                }

                await client.Child("gyms").PutAsync(gymsBatch);

                Console.WriteLine($"Aggiunte {gymsData.Gyms.Count} palestre");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il seeding delle palestre: {ex.Message}");
                throw;
            }
        }

        private async Task SeedUserModelsFromJsonAsync(FirebaseClient client)
        {
            try
            {
                Console.WriteLine("Popolamento dei modelli utente dal file JSON...");

                // Leggi i dati dal file JSON
                var userModelsData = await ReadJsonResource<UserModelsData>("userModels.json");

                if (userModelsData?.UserModels == null || !userModelsData.UserModels.Any())
                {
                    Console.WriteLine("Nessun modello utente trovato nel file userModels.json");
                    return;
                }

                // Salva i modelli utente
                var userModelsBatch = new Dictionary<string, object>();
                foreach (var userModelData in userModelsData.UserModels)
                {
                    var userModel = new UserModel
                    {
                        Id = userModelData.Id,
                        Name = userModelData.Name,
                        Email = userModelData.Email,
                        ClimbsCompleted = userModelData.ClimbsCompleted,
                        SkillLevel = userModelData.SkillLevel,
                        JoinDate = DateTime.Parse(userModelData.JoinDate)
                    };

                    userModelsBatch.Add(userModel.Id, userModel);
                }

                await client.Child("users").PutAsync(userModelsBatch);

                Console.WriteLine($"Aggiunti {userModelsData.UserModels.Count} modelli utente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il seeding dei modelli utente: {ex.Message}");
                throw;
            }
        }

        private async Task SeedTrainingSessionsFromJsonAsync(FirebaseClient client)
        {
            try
            {
                Console.WriteLine("Popolamento delle sessioni di allenamento dal file JSON...");

                // Leggi i dati dal file JSON
                var sessionsData = await ReadJsonResource<TrainingSessionsData>("trainingSessions.json");

                if (sessionsData?.UserSessions == null || !sessionsData.UserSessions.Any())
                {
                    Console.WriteLine("Nessuna sessione trovata nel file trainingSessions.json");
                    return;
                }

                // Per ogni utente, salva le sue sessioni
                foreach (var userSession in sessionsData.UserSessions)
                {
                    if (string.IsNullOrEmpty(userSession.UserId) || userSession.Sessions == null || !userSession.Sessions.Any())
                        continue;

                    Console.WriteLine($"Creazione sessioni per utente: {userSession.UserId}");

                    // Crea un batch di sessioni per questo utente
                    var sessionsBatch = new Dictionary<string, TrainingSession>();

                    foreach (var sessionData in userSession.Sessions)
                    {
                        var session = new TrainingSession
                        {
                            Id = sessionData.Id,
                            UserId = userSession.UserId,
                            PanelType = sessionData.PanelType,
                            Timestamp = DateTime.Parse(sessionData.Timestamp),
                            Duration = ParseTimeSpan(sessionData.Duration),
                            CompletedRoutes = sessionData.CompletedRoutes.Select(r => new CompletedRoute
                            {
                                RouteId = r.RouteId,
                                Completed = r.Completed,
                                Attempts = r.Attempts
                            }).ToList()
                        };

                        sessionsBatch.Add(session.Id, session);
                    }

                    // Salva tutte le sessioni per questo utente in un'unica operazione
                    if (sessionsBatch.Any())
                    {
                        await client
                            .Child("trainingSessions")
                            .Child(userSession.UserId)
                            .PutAsync(sessionsBatch);

                        Console.WriteLine($"Aggiunte {sessionsBatch.Count} sessioni per l'utente {userSession.UserId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore durante il seeding delle sessioni di allenamento: {ex.Message}");
                throw;
            }
        }

        // Helper per convertire una stringa ISO 8601 in TimeSpan
        private TimeSpan ParseTimeSpan(string duration)
        {
            // Formato ISO 8601 per durate: PT1H30M (1 ora e 30 minuti)
            if (duration.StartsWith("PT"))
            {
                duration = duration.Substring(2);

                int hours = 0;
                int minutes = 0;
                int seconds = 0;

                int hIndex = duration.IndexOf('H');
                if (hIndex > 0)
                {
                    hours = int.Parse(duration.Substring(0, hIndex));
                    duration = duration.Substring(hIndex + 1);
                }

                int mIndex = duration.IndexOf('M');
                if (mIndex > 0)
                {
                    minutes = int.Parse(duration.Substring(0, mIndex));
                    duration = duration.Substring(mIndex + 1);
                }

                int sIndex = duration.IndexOf('S');
                if (sIndex > 0)
                {
                    seconds = int.Parse(duration.Substring(0, sIndex));
                }

                return new TimeSpan(hours, minutes, seconds);
            }

            // Fallback: prova a convertire direttamente
            return TimeSpan.Parse(duration);
        }

        private async Task<bool> CheckInternetConnection()
        {
            try
            {
                // Option 1: Try multiple reliable endpoints
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);

                    // Try multiple endpoints in case one is blocked
                    var endpoints = new[]
                    {
                "https://www.google.com",
                "https://www.microsoft.com",
                "https://www.cloudflare.com"
            };

                    foreach (var endpoint in endpoints)
                    {
                        try
                        {
                            var response = await client.GetAsync(endpoint);
                            if (response.IsSuccessStatusCode)
                            {
                                return true;
                            }
                        }
                        catch
                        {
                            // Continue to the next endpoint
                            continue;
                        }
                    }

                    // All endpoints failed
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Internet connectivity check failed: {ex.Message}");

                // Option 2: Use platform-specific connectivity APIs as fallback
                try
                {
#if __ANDROID__
                    // Android-specific connectivity check
                    var connectivityManager = Android.App.Application.Context.GetSystemService(Android.Content.Context.ConnectivityService) as Android.Net.ConnectivityManager;
                    var activeNetworkInfo = connectivityManager?.ActiveNetworkInfo;
                    return activeNetworkInfo?.IsConnected ?? false;
#else
            // Default fallback for other platforms
            return false;
#endif
                }
                catch (Exception platformEx)
                {
                    Debug.WriteLine($"Platform-specific connectivity check failed: {platformEx.Message}");
                    return false;
                }
            }
        }

        private ObservableCollection<T> GetMockItems<T>(string nodePath)
        {
            var collection = new ObservableCollection<T>();

            // Genera dati fittizi in base al tipo e al percorso
            if (typeof(T) == typeof(ClimbingRoute))
            {
                // Mock per percorsi di arrampicata
                for (int i = 1; i <= 10; i++)
                {
                    var route = new ClimbingRoute
                    {
                        Id = $"mock-route-{i}",
                        Name = $"Percorso Mock {i}",
                        Color = i % 3 == 0 ? "Rosso" : (i % 3 == 1 ? "Blu" : "Verde"),
                        ColorHex = i % 3 == 0 ? "#FF0000" : (i % 3 == 1 ? "#0000FF" : "#00FF00"),
                        Difficulty = (i % 5) + 1,
                        PanelType = i % 2 == 0 ? "Verticale" : "Strapiombo",
                        CreatedDate = DateTime.Now.AddDays(-i),
                        IsActive = i % 4 != 0
                    };

                    collection.Add((T)(object)route);
                }
            }
            else if (typeof(T) == typeof(UserProfile))
            {
                // Mock per profili utente
                for (int i = 1; i <= 5; i++)
                {
                    var profile = new UserProfile
                    {
                        Id = $"mock-user-{i}",
                        DisplayName = $"Utente Test {i}",
                        Email = $"test{i}@example.com",
                        PhotoUrl = $"https://example.com/avatar{i}.jpg",
                        Bio = $"Biografia dell'utente test {i}",
                        PreferredGym = $"mock-gym-{i % 3 + 1}",
                        CreatedAt = DateTime.Now.AddMonths(-i),
                        LastLoginAt = DateTime.Now.AddDays(-i)
                    };

                    collection.Add((T)(object)profile);
                }
            }
            else if (typeof(T) == typeof(Gym))
            {
                // Mock per palestre
                for (int i = 1; i <= 3; i++)
                {
                    var gym = new Gym
                    {
                        Id = $"mock-gym-{i}",
                        Name = $"Palestra Test {i}",
                        Address = $"Via Test {i}, Città Test",
                        Location = new GeoPoint { Latitude = 45.0 + (i * 0.1), Longitude = 9.0 + (i * 0.1) },
                        HasVerticalWall = i % 2 == 0,
                        HasOverhangWall = i % 2 == 1,
                        Website = $"https://example.com/gym{i}",
                        PhoneNumber = $"+39 123 456{i}",
                        ImageUrl = $"https://example.com/gym{i}.jpg",
                        Amenities = new List<string> { "Docce", "Spogliatoi", "Bar" },
                        OpeningHours = new Dictionary<string, string>
                {
                    { "Lunedì", "9:00-22:00" },
                    { "Martedì", "9:00-22:00" },
                    { "Mercoledì", "9:00-22:00" },
                    { "Giovedì", "9:00-22:00" },
                    { "Venerdì", "9:00-22:00" },
                    { "Sabato", "10:00-20:00" },
                    { "Domenica", "10:00-18:00" }
                }
                    };

                    collection.Add((T)(object)gym);
                }
            }
            else if (typeof(T) == typeof(TrainingSession))
            {
                // Mock per sessioni di allenamento
                string userId = "mock-user-1";
                if (nodePath.Contains("/"))
                {
                    userId = nodePath.Split('/').Last();
                }

                for (int i = 1; i <= 5; i++)
                {
                    var session = new TrainingSession
                    {
                        Id = $"mock-session-{i}",
                        UserId = userId,
                        PanelType = i % 2 == 0 ? "Verticale" : "Strapiombo",
                        Timestamp = DateTime.Now.AddDays(-i),
                        Duration = TimeSpan.FromMinutes(30 + (i * 10)),
                        CompletedRoutes = new List<CompletedRoute>
                {
                    new CompletedRoute { RouteId = $"mock-route-{i}", Completed = true, Attempts = 2 },
                    new CompletedRoute { RouteId = $"mock-route-{i+1}", Completed = i % 2 == 0, Attempts = 3 }
                }
                    };

                    collection.Add((T)(object)session);
                }
            }

            return collection;
        }

        private T GetMockItem<T>(string nodePath, string itemId)
        {
            // Genera un singolo elemento fittizio in base al tipo
            if (typeof(T) == typeof(ClimbingRoute))
            {
                int id = 1;
                if (itemId.Contains("-"))
                {
                    int.TryParse(itemId.Split('-').Last(), out id);
                }

                var route = new ClimbingRoute
                {
                    Id = itemId,
                    Name = $"Percorso Mock {id}",
                    Color = id % 3 == 0 ? "Rosso" : (id % 3 == 1 ? "Blu" : "Verde"),
                    ColorHex = id % 3 == 0 ? "#FF0000" : (id % 3 == 1 ? "#0000FF" : "#00FF00"),
                    Difficulty = (id % 5) + 1,
                    PanelType = id % 2 == 0 ? "Verticale" : "Strapiombo",
                    CreatedDate = DateTime.Now.AddDays(-id),
                    IsActive = id % 4 != 0
                };

                return (T)(object)route;
            }
            else if (typeof(T) == typeof(UserProfile))
            {
                int id = 1;
                if (itemId.Contains("-"))
                {
                    int.TryParse(itemId.Split('-').Last(), out id);
                }

                var profile = new UserProfile
                {
                    Id = itemId,
                    DisplayName = $"Utente Test {id}",
                    Email = $"test{id}@example.com",
                    PhotoUrl = $"https://example.com/avatar{id}.jpg",
                    Bio = $"Biografia dell'utente test {id}",
                    PreferredGym = $"mock-gym-{id % 3 + 1}",
                    CreatedAt = DateTime.Now.AddMonths(-id),
                    LastLoginAt = DateTime.Now.AddDays(-id)
                };

                return (T)(object)profile;
            }
            else if (typeof(T) == typeof(Gym))
            {
                int id = 1;
                if (itemId.Contains("-"))
                {
                    int.TryParse(itemId.Split('-').Last(), out id);
                }

                var gym = new Gym
                {
                    Id = itemId,
                    Name = $"Palestra Test {id}",
                    Address = $"Via Test {id}, Città Test",
                    Location = new GeoPoint { Latitude = 45.0 + (id * 0.1), Longitude = 9.0 + (id * 0.1) },
                    HasVerticalWall = id % 2 == 0,
                    HasOverhangWall = id % 2 == 1,
                    Website = $"https://example.com/gym{id}",
                    PhoneNumber = $"+39 123 456{id}",
                    ImageUrl = $"https://example.com/gym{id}.jpg",
                    Amenities = new List<string> { "Docce", "Spogliatoi", "Bar" },
                    OpeningHours = new Dictionary<string, string>
            {
                { "Lunedì", "9:00-22:00" },
                { "Martedì", "9:00-22:00" },
                { "Mercoledì", "9:00-22:00" },
                { "Giovedì", "9:00-22:00" },
                { "Venerdì", "9:00-22:00" },
                { "Sabato", "10:00-20:00" },
                { "Domenica", "10:00-18:00" }
            }
                };

                return (T)(object)gym;
            }
            else if (typeof(T) == typeof(TrainingSession))
            {
                int id = 1;
                if (itemId.Contains("-"))
                {
                    int.TryParse(itemId.Split('-').Last(), out id);
                }

                string userId = "mock-user-1";
                if (nodePath.Contains("/"))
                {
                    userId = nodePath.Split('/').Last();
                }

                var session = new TrainingSession
                {
                    Id = itemId,
                    UserId = userId,
                    PanelType = id % 2 == 0 ? "Verticale" : "Strapiombo",
                    Timestamp = DateTime.Now.AddDays(-id),
                    Duration = TimeSpan.FromMinutes(30 + (id * 10)),
                    CompletedRoutes = new List<CompletedRoute>
            {
                new CompletedRoute { RouteId = $"mock-route-{id}", Completed = true, Attempts = 2 },
                new CompletedRoute { RouteId = $"mock-route-{id+1}", Completed = id % 2 == 0, Attempts = 3 }
            }
                };

                return (T)(object)session;
            }

            // Se il tipo non è gestito, restituisci il valore predefinito
            return default;
        }

        // Classi per deserializzare i file JSON
        public class RoutesData
        {
            public List<PanelData> Panels { get; set; }
        }

        public class PanelData
        {
            public string Name { get; set; }
            public List<RouteData> Routes { get; set; }
        }

        public class RouteData
        {
            public string Name { get; set; }
            public string Color { get; set; }
            public string ColorHex { get; set; }
            public int Difficulty { get; set; }
            public bool IsActive { get; set; }
            public string CreatedDate { get; set; }
        }

        public class UsersData
        {
            public List<UserData> Users { get; set; }
        }

        public class UserData
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public string Email { get; set; }
            public string PhotoUrl { get; set; }
            public string Bio { get; set; }
            public string PreferredGym { get; set; }
            public string CreatedAt { get; set; }
            public string LastLoginAt { get; set; }
        }

        public class GymsData
        {
            public List<GymData> Gyms { get; set; }
        }

        public class GymData
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public GeoPointData Location { get; set; }
            public bool HasVerticalWall { get; set; }
            public bool HasOverhangWall { get; set; }
            public string Website { get; set; }
            public string PhoneNumber { get; set; }
            public string ImageUrl { get; set; }
            public List<string> Amenities { get; set; }
            public Dictionary<string, string> OpeningHours { get; set; }
        }

        public class GeoPointData
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

        public class TrainingSessionsData
        {
            public List<UserSessionsData> UserSessions { get; set; }
        }

        public class UserSessionsData
        {
            public string UserId { get; set; }
            public List<SessionData> Sessions { get; set; }
        }

        public class SessionData
        {
            public string Id { get; set; }
            public string PanelType { get; set; }
            public string Timestamp { get; set; }
            public string Duration { get; set; }
            public List<CompletedRouteData> CompletedRoutes { get; set; }
        }

        public class CompletedRouteData
        {
            public string RouteId { get; set; }
            public bool Completed { get; set; }
            public int Attempts { get; set; }
        }

        public class UserModelsData
        {
            public List<UserModelData> UserModels { get; set; }
        }

        public class UserModelData
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public int ClimbsCompleted { get; set; }
            public string SkillLevel { get; set; }
            public string JoinDate { get; set; }
        }
    }
}
