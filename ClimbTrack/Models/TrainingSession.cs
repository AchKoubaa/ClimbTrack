using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace ClimbTrack.Models
{
    public class TrainingSession : BaseModel
    {
        private string _id;
        private string _userId;
        private string _panelType;
        private DateTime _timestamp;
        private List<CompletedRoute> _completedRoutes;
        private TimeSpan _duration;

        [JsonProperty("id")]
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        [JsonProperty("userId")]
        public string UserId
        {
            get => _userId;
            set => SetProperty(ref _userId, value);
        }

        [JsonProperty("panelType")]
        public string PanelType
        {
            get => _panelType;
            set => SetProperty(ref _panelType, value);
        }

        [JsonProperty("timestamp")]
        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
        }

        [JsonProperty("completedRoutes")]
        public List<CompletedRoute> CompletedRoutes
        {
            get => _completedRoutes;
            set => SetProperty(ref _completedRoutes, value);
        }

        [JsonProperty("duration")]
        public TimeSpan Duration
        {
            get => _duration;
            set => SetProperty(ref _duration, value);
        }

        // Proprietà calcolate (non salvate direttamente nel database)
        [JsonIgnore]
        public int TotalRoutes => CompletedRoutes?.Count ?? 0;

        [JsonIgnore]
        public int CompletedRoutesCount => CompletedRoutes?.Count(r => r.Completed) ?? 0;

        [JsonIgnore]
        public string FormattedDuration => $"{Duration.Hours:00}:{Duration.Minutes:00}:{Duration.Seconds:00}";

        [JsonIgnore]
        public string FormattedDate => Timestamp.ToString("dd/MM/yyyy HH:mm");
    }

    public class CompletedRoute : BaseModel
    {
        private string _routeId;
        private bool _completed;
        private int _attempts;

        [JsonProperty("routeId")]
        public string RouteId
        {
            get => _routeId;
            set => SetProperty(ref _routeId, value);
        }

        [JsonProperty("completed")]
        public bool Completed
        {
            get => _completed;
            set => SetProperty(ref _completed, value);
        }

        [JsonProperty("attempts")]
        public int Attempts
        {
            get => _attempts;
            set => SetProperty(ref _attempts, value);
        }
    }
}