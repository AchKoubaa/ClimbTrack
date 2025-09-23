using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public interface IPhotoPickerService
    {
        Task<string> PickAndSavePhotoAsync();
        bool IsPathValid(string path);
    }
}
