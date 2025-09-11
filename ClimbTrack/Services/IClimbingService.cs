using ClimbTrack.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public interface IClimbingService
    {
        Task<ObservableCollection<ClimbingRoute>> GetRoutesAsync(string panelType = null);
        Task<ClimbingRoute> GetRouteAsync(string panelType, string routeId);
        Task<string> AddRouteAsync(ClimbingRoute route);
        Task<bool> UpdateRouteAsync(ClimbingRoute route);
        Task<bool> DeleteRouteAsync(string panelType, string routeId);
        Task<ObservableCollection<string>> GetPanelTypesAsync();
    }

}
