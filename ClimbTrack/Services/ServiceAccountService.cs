using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public class ServiceAccountService : IServiceAccountService
    {
        private static readonly string ServiceAccountFileName = "climbtracknew-firebase-adminsdk-fbsvc-1602593b84.json";
        private Dictionary<string, object> _serviceAccountData;
        private string _serviceAccountEmail;
        private string _privateKey;
        private string _projectId;
        private readonly HttpClient _httpClient;
        private bool _isServiceAccountInitialized = false;

        public ServiceAccountService()
        {
            _httpClient = new HttpClient();
        }

        // Initialize service account from JSON file
        private async Task InitializeServiceAccountAsync()
        {
            if (_isServiceAccountInitialized)
            {
                return; // Already initialized
            }

            try
            {
                Console.WriteLine($"Initializing service account from: {ServiceAccountFileName}");

                // Check if file exists first
                bool fileExists = await FileSystem.AppPackageFileExistsAsync(ServiceAccountFileName);
                if (!fileExists)
                {
                    throw new FileNotFoundException($"Service account file not found: {ServiceAccountFileName}");
                }

                // Load the service account JSON file
                using var stream = await FileSystem.OpenAppPackageFileAsync(ServiceAccountFileName);
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync();

                if (string.IsNullOrEmpty(json))
                {
                    throw new InvalidDataException("Service account file is empty");
                }

                // Parse the JSON
                _serviceAccountData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                // Validate required fields
                if (!_serviceAccountData.ContainsKey("project_id") ||
                    !_serviceAccountData.ContainsKey("client_email") ||
                    !_serviceAccountData.ContainsKey("private_key"))
                {
                    throw new InvalidDataException("Service account file is missing required fields");
                }

                // Extract key information
                _projectId = _serviceAccountData["project_id"].ToString();
                _serviceAccountEmail = _serviceAccountData["client_email"].ToString();
                _privateKey = _serviceAccountData["private_key"].ToString();

                _isServiceAccountInitialized = true;
                Console.WriteLine($"Service account initialized for project: {_projectId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing service account: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                _isServiceAccountInitialized = false;
                throw; // Rethrow to handle at a higher level
            }
        }

        // Check if service account is initialized
        private async Task EnsureServiceAccountInitializedAsync()
        {
            if (!_isServiceAccountInitialized)
            {
                await InitializeServiceAccountAsync();

                if (!_isServiceAccountInitialized)
                {
                    throw new InvalidOperationException("Service account is not initialized");
                }
            }
        }

        // Generate a JWT token for authenticating with Firebase APIs
        private async Task<string> GenerateJwtTokenAsync()
        {
            await EnsureServiceAccountInitializedAsync();

            // JWT header
            var header = new
            {
                alg = "RS256",
                typ = "JWT"
            };

            // Current time in seconds since epoch
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // JWT claims
            var claims = new
            {
                iss = _serviceAccountEmail,
                sub = _serviceAccountEmail,
                aud = "https://identitytoolkit.googleapis.com/google.identity.identitytoolkit.v1.IdentityToolkit",
                iat = now,
                exp = now + 3600, // Token expires in 1 hour
                uid = "service-account"
            };

            // Encode header and claims
            var headerJson = JsonConvert.SerializeObject(header);
            var claimsJson = JsonConvert.SerializeObject(claims);

            var headerBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            var claimsBase64 = Base64UrlEncode(Encoding.UTF8.GetBytes(claimsJson));

            var dataToSign = $"{headerBase64}.{claimsBase64}";

            // Sign the data with the private key
            var signature = SignData(dataToSign, _privateKey);
            var signatureBase64 = Base64UrlEncode(signature);

            // Combine to form the JWT
            return $"{dataToSign}.{signatureBase64}";
        }

        // Sign data with the private key
        private byte[] SignData(string data, string privateKey)
        {
            // Clean up the private key
            privateKey = privateKey
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\n", "");

            var keyBytes = Convert.FromBase64String(privateKey);

            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(keyBytes, out _);

            var dataBytes = Encoding.UTF8.GetBytes(data);
            return rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        // Base64Url encode
        private string Base64UrlEncode(byte[] data)
        {
            return Convert.ToBase64String(data)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        // Get an access token for Google APIs
        private async Task<string> GetGoogleAccessTokenAsync()
        {
            try
            {
                // Generate a JWT assertion
                var jwt = await GenerateJwtTokenAsync();

                // Exchange the JWT for an access token
                var tokenRequest = new Dictionary<string, string>
                {
                    { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                    { "assertion", jwt }
                };

                var content = new FormUrlEncodedContent(tokenRequest);
                var response = await _httpClient.PostAsync(
                    "https://oauth2.googleapis.com/token",
                    content);

                response.EnsureSuccessStatusCode();

                var responseData = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                return responseData["access_token"].ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting Google access token: {ex.Message}");
                throw;
            }
        }
        public async Task<string> CreateCustomTokenAsync(string uid, Dictionary<string, object> claims = null)
        {
            try
            {
                await EnsureServiceAccountInitializedAsync();

                Console.WriteLine($"Creating custom token for user: {uid}");

                // Get an access token for the Firebase API
                var accessToken = await GetGoogleAccessTokenAsync();

                // Prepare the request to the Firebase Auth API
                var requestData = new
                {
                    uid = uid,
                    claims = claims,
                    tenantId = (string)null
                };

                // Make the request
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.PostAsJsonAsync(
                    $"https://identitytoolkit.googleapis.com/v1/projects/{_projectId}/accounts:signInWithCustomToken",
                    requestData);

                response.EnsureSuccessStatusCode();

                // Parse the response
                var responseData = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                var token = responseData["idToken"].ToString();

                Console.WriteLine("Custom token created successfully");
                return token;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating custom token: {ex.Message}");
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetUserWithServiceAccountAsync(string uid)
        {
            try
            {
                await EnsureServiceAccountInitializedAsync();

                Console.WriteLine($"Getting user info for: {uid}");

                // Get an access token for the Firebase API
                var accessToken = await GetGoogleAccessTokenAsync();

                // Make the request
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.GetAsync(
                    $"https://identitytoolkit.googleapis.com/v1/projects/{_projectId}/accounts:lookup?localId={uid}");

                response.EnsureSuccessStatusCode();

                // Parse the response
                var responseData = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
                var users = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(responseData["users"].ToString());

                if (users.Count > 0)
                {
                    Console.WriteLine($"User info retrieved: {users[0]["email"]}");
                    return users[0];
                }
                else
                {
                    throw new Exception("User not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user: {ex.Message}");
                throw;
            }
        }

        public async Task<Dictionary<string, object>> CreateUserWithServiceAccountAsync(string email, string password, string displayName = null)
        {
            try
            {
                await EnsureServiceAccountInitializedAsync();

                Console.WriteLine($"Creating user with email: {email}");

                // Get an access token for the Firebase API
                var accessToken = await GetGoogleAccessTokenAsync();

                // Prepare the request
                var requestData = new
                {
                    email = email,
                    password = password,
                    displayName = displayName,
                    emailVerified = false
                };

                // Make the request
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.PostAsJsonAsync(
                    $"https://identitytoolkit.googleapis.com/v1/projects/{_projectId}/accounts",
                    requestData);

                response.EnsureSuccessStatusCode();

                // Parse the response
                var user = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

                Console.WriteLine($"User created with ID: {user["localId"]}");
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user: {ex.Message}");
                throw;
            }
        }

        public async Task<Dictionary<string, object>> VerifyIdTokenAsync(string idToken)
        {
            try
            {
                await EnsureServiceAccountInitializedAsync();

                Console.WriteLine("Verifying ID token");

                // Get an access token for the Firebase API
                var accessToken = await GetGoogleAccessTokenAsync();

                // Prepare the request
                var requestData = new
                {
                    idToken = idToken
                };

                // Make the request
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var response = await _httpClient.PostAsJsonAsync(
                    $"https://identitytoolkit.googleapis.com/v1/projects/{_projectId}/accounts:lookup",
                    requestData);

                response.EnsureSuccessStatusCode();

                // Parse the response
                var responseData = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

                Console.WriteLine("Token verified successfully");
                return responseData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verifying token: {ex.Message}");
                throw;
            }
        }
    }
}
