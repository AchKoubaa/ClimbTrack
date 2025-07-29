using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Database;
using Firebase.Database.Query;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Reflection;

namespace ClimbTrack.Services
{
    public class FirebaseService : IFirebaseService
    {
        // Your Firebase project configuration
        private const string FirebaseApiKey = "AIzaSyCsVPB4co-5f9gY1EL52-ghM8LC3Lmu8dE";
        private const string FirebaseDatabaseUrl = "https://test1-7f758-default-rtdb.firebaseio.com/";
        private const string ServiceAccountFileName = "test1-7f758-firebase-adminsdk-fbsvc-7f6afb52e6.json";
      
        private readonly FirebaseAuthClient _authClient;
        private readonly FirebaseClient _databaseClient;
        private string _serviceAccountJson;

        public FirebaseService()
        {
            // Load the service account key
            LoadServiceAccount();

            // Set up Firebase Auth
            var config = new FirebaseAuthConfig
            {
                ApiKey = FirebaseApiKey,
                AuthDomain = "test1-7f758.firebaseapp.com",
                Providers = new FirebaseAuthProvider[]
                {
                    new EmailProvider(),
                    new GoogleProvider()

                }
            };

            _authClient = new FirebaseAuthClient(config);
            _databaseClient = new FirebaseClient(FirebaseDatabaseUrl);
        }

        private void LoadServiceAccount()
        {
            try
            {
                // Get the service account JSON from embedded resources
                using var stream = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream($"ClimbTrack.{ServiceAccountFileName}");

                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    _serviceAccountJson = reader.ReadToEnd();

                    // For debugging purposes, verify the file was loaded
                    Console.WriteLine("Service account loaded successfully");
                }
                else
                {
                    Console.WriteLine("Failed to load service account - stream is null");

                    // List all available resources for debugging
                    var resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                    foreach (var resource in resources)
                    {
                        Console.WriteLine($"Available resource: {resource}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading Firebase service account: {ex.Message}");
            }
        }

        // Authentication Methods
        public async Task<UserCredential> SignUpWithEmailAndPassword(string email, string password)
        {
            return await _authClient.CreateUserWithEmailAndPasswordAsync(email, password);
        }

        public async Task<UserCredential> SignInWithEmailAndPassword(string email, string password)
        {
            return await _authClient.SignInWithEmailAndPasswordAsync(email, password);
        }

        public async Task ResetPassword(string email)
        {
            await _authClient.ResetEmailPasswordAsync(email);
        }

        // Database Methods
        public async Task<string> AddItem<T>(string nodePath, T item)
        {
            try
            {
                var result = await _databaseClient
                    .Child(nodePath)
                    .PostAsync(JsonConvert.SerializeObject(item));

                return result.Key;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding item: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateItem<T>(string nodePath, string itemId, T item)
        {
            try
            {
                await _databaseClient
                    .Child(nodePath)
                    .Child(itemId)
                    .PutAsync(JsonConvert.SerializeObject(item));
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating item: {ex.Message}");
                return false;
            }
        }

        public async Task<T> GetItem<T>(string nodePath, string itemId)
        {
            try
            {
                var item = await _databaseClient
                    .Child(nodePath)
                    .Child(itemId)
                    .OnceSingleAsync<T>();
                return item;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting item: {ex.Message}");
                return default;
            }
        }

        public async Task<ObservableCollection<T>> GetItems<T>(string nodePath)
        {
            try
            {
                var items = await _databaseClient
                    .Child(nodePath)
                    .OnceAsync<T>();

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
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting items: {ex.Message}");
                return new ObservableCollection<T>();
            }
        }

        public async Task<bool> DeleteItem(string nodePath, string itemId)
        {
            try
            {
                await _databaseClient
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

        // Method to extract service account file to local storage if needed
        public string ExtractServiceAccountToLocalStorage()
        {
            try
            {
                string localPath = Path.Combine(FileSystem.AppDataDirectory, ServiceAccountFileName);

                // Write the JSON content to a file
                File.WriteAllText(localPath, _serviceAccountJson);

                return localPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting service account: {ex.Message}");
                return null;
            }
        }


        // Add Google Sign-In method
        public async Task<UserCredential> SignInWithGoogleAccessTokenAsync(string accessToken)
        {
            try
            {
                Console.WriteLine("Signing in with Google access token");

                // Create a credential from the access token
                Console.WriteLine("Creating Google credential");
                var credential = GoogleProvider.GetCredential(accessToken, OAuthCredentialTokenType.AccessToken);

                // Sign in with the credential
                Console.WriteLine("Calling SignInWithCredentialAsync");
                var result = await _authClient.SignInWithCredentialAsync(credential);

                Console.WriteLine($"Successfully signed in with Google: {result.User.Info.Email}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error signing in with Google: {ex.Message}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }

                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }


    }
}
