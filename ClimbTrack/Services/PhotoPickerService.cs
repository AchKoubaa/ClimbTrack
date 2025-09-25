using CommunityToolkit.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ClimbTrack.Services
{
    public class PhotoPickerService : IPhotoPickerService
    {
        public async Task<string> PickAndSavePhotoAsync()
        {
            try
            {
                var result = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                {
                    Title = "Seleziona una foto profilo"
                });

                if (result == null)
                    return null;

                // Create a unique filename
                string fileName = $"profile_{Guid.NewGuid()}{Path.GetExtension(result.FileName)}";

                try
                {
                    // Get the app's cache directory (more permissive than AppDataDirectory)
                    string targetPath = Path.Combine(FileSystem.CacheDirectory, fileName);

                    using (var sourceStream = await result.OpenReadAsync())
                    using (var targetStream = File.Create(targetPath))
                    {
                        await sourceStream.CopyToAsync(targetStream);
                    }

                    return targetPath;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"File saving exception: {ex}");

                    // Windows-specific handling
                    if (DeviceInfo.Platform == DevicePlatform.WinUI)
                    {
                        // On Windows, we might need to use the original path
                        return result.FullPath;
                    }

                    throw; // Re-throw if we can't handle it
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Photo picking failed: {ex}");
                await Shell.Current.DisplayAlert("Errore",
                    $"Impossibile selezionare la foto: {ex.Message}", "OK");
                return null;
            }
        }

        public bool IsPathValid(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            try
            {
                // Check if the file exists and is accessible
                return File.Exists(path);
            }
            catch
            {
                return false;
            }
        }
    }
}
