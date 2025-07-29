using Firebase.Auth;
using System.Collections.ObjectModel;

namespace ClimbTrack.Services
{
    public interface IFirebaseService
    {
        Task<UserCredential> SignUpWithEmailAndPassword(string email, string password);
        Task<UserCredential> SignInWithEmailAndPassword(string email, string password);
        Task ResetPassword(string email);

        Task<string> AddItem<T>(string nodePath, T item);
        Task<bool> UpdateItem<T>(string nodePath, string itemId, T item);
        Task<T> GetItem<T>(string nodePath, string itemId);
        Task<ObservableCollection<T>> GetItems<T>(string nodePath);
        Task<bool> DeleteItem(string nodePath, string itemId);
        string ExtractServiceAccountToLocalStorage();

        Task<UserCredential> SignInWithGoogleAccessTokenAsync(string accessToken);


    }
}
