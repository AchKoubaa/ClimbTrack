using ClimbTrack.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public class ClimbingService : IClimbingService
    {
        private ObservableCollection<string> _cachedPanelTypes;
        private DateTime _panelTypesCacheTime = DateTime.MinValue;
        private readonly TimeSpan _panelTypesCacheDuration = TimeSpan.FromMinutes(10);

        private readonly IDatabaseService _databaseService;
        

        public ClimbingService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<ObservableCollection<ClimbingRoute>> GetRoutesAsync(string panelType = null)
        {
            try
            {
                ObservableCollection<ClimbingRoute> routes;

                if (string.IsNullOrEmpty(panelType))
                {
                    // Se non è specificato un tipo di pannello, ottieni tutti i percorsi
                    var allRoutes = new List<ClimbingRoute>();
                    var panelTypes = await GetPanelTypesAsync();

                    foreach (var panel in panelTypes)
                    {
                        var panelRoutes = await _databaseService.GetItems<ClimbingRoute>($"routes/{panel}");
                        foreach (var route in panelRoutes)
                        {
                            allRoutes.Add(route);
                        }
                    }

                    // Order all routes by difficulty and convert to ObservableCollection
                    routes = new ObservableCollection<ClimbingRoute>(
                        allRoutes.OrderBy(route => route.Difficulty)
                    );
                }
                else
                {
                    // Ottieni i percorsi per il tipo di pannello specificato
                    var panelRoutes = await _databaseService.GetItems<ClimbingRoute>($"routes/{panelType}");

                    // Order panel routes by difficulty
                    routes = new ObservableCollection<ClimbingRoute>(
                        panelRoutes.OrderBy(route => route.Difficulty)
                    );
                }

                return routes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting routes: {ex.Message}");
                return new ObservableCollection<ClimbingRoute>();
            }
        }

        public async Task<ClimbingRoute> GetRouteAsync(string panelType, string routeId)
        {
            if (string.IsNullOrEmpty(panelType))
            {
                throw new ArgumentException("Panel type cannot be empty", nameof(panelType));
            }

            if (string.IsNullOrEmpty(routeId))
            {
                throw new ArgumentException("Route ID cannot be empty", nameof(routeId));
            }

            return await _databaseService.GetItem<ClimbingRoute>($"routes/{panelType}", routeId);
        }

        public async Task<string> AddRouteAsync(ClimbingRoute route)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (string.IsNullOrEmpty(route.PanelType))
            {
                throw new ArgumentException("Route must have a panel type", nameof(route));
            }

            // Imposta la data di creazione se non è già impostata
            if (route.CreatedDate == default)
            {
                route.CreatedDate = DateTime.UtcNow;
            }

            // Aggiungi il percorso al database
            return await _databaseService.AddItem($"routes/{route.PanelType}", route);
        }

        public async Task<bool> UpdateRouteAsync(ClimbingRoute route)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (string.IsNullOrEmpty(route.PanelType) || string.IsNullOrEmpty(route.Id))
            {
                throw new ArgumentException("Route must have a panel type and ID", nameof(route));
            }

            // Aggiorna il percorso nel database
            return await _databaseService.UpdateItem($"routes/{route.PanelType}", route.Id, route);
        }

        public async Task<bool> DeleteRouteAsync(string panelType, string routeId)
        {
            if (string.IsNullOrEmpty(panelType))
            {
                throw new ArgumentException("Panel type cannot be empty", nameof(panelType));
            }

            if (string.IsNullOrEmpty(routeId))
            {
                throw new ArgumentException("Route ID cannot be empty", nameof(routeId));
            }

            // Elimina il percorso dal database
            return await _databaseService.DeleteItem($"routes/{panelType}", routeId);
        }

        public async Task<ObservableCollection<string>> GetPanelTypesAsync()
        {
            

            try
            {
                
                var panelTypes = await _databaseService.GetChildKeys("routes");

               
                _cachedPanelTypes = panelTypes;
                _panelTypesCacheTime = DateTime.Now;

                


                return panelTypes;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting panel types: {ex.Message}");

                // In caso di errore, restituisci i tipi di pannello predefiniti
                //var defaultPanelTypes = new ObservableCollection<string>
                //{
                //    "Verticale",
                //    "Strapiombo"
                //};

                return null;
            }
        }
    }
}
