using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public interface IAnalyticsService
    {
        Task InitializeAsync(string appId);
        Task TrackEventAsync(string eventName, Dictionary<string, string> properties = null);
        Task TrackPageViewAsync(string pageName, Dictionary<string, string> properties = null);
        Task IdentifyUserAsync(string userId, Dictionary<string, string> traits = null);

     
        Task TrackErrorAsync(Exception exception, string context = null);
    }
    }

