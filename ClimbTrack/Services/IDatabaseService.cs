using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public interface IDatabaseService
    {
        Task<string> AddItem<T>(string nodePath, T item);
        Task<bool> UpdateItem<T>(string nodePath, string itemId, T item);
        Task<T> GetItem<T>(string nodePath, string itemId);
        Task<ObservableCollection<T>> GetItems<T>(string nodePath);
        Task<ObservableCollection<string>> GetChildKeys(string nodePath);
        Task<bool> DeleteItem(string nodePath, string itemId);
        Task InitializeDatabaseAsync();
        Task SeedDatabaseIfNeeded();
        Task<bool> CheckIfDatabaseNeedsSeedingAsync();


    }
}
