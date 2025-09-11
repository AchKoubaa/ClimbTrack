using System;
using Newtonsoft.Json;

namespace ClimbTrack.Models
{
    public class ClimbingRoute : BaseModel
    {
        private string _id;
        private string _name;
        private string _color;
        private string _colorHex;
        private int _difficulty;
        private string _panelType;
        private DateTime _createdDate;
        private bool _isActive;

        [JsonProperty("id")]
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        [JsonProperty("color")]
        public string Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        [JsonProperty("colorHex")]
        public string ColorHex
        {
            get => _colorHex;
            set => SetProperty(ref _colorHex, value);
        }

        [JsonProperty("difficulty")]
        public int Difficulty
        {
            get => _difficulty;
            set => SetProperty(ref _difficulty, value);
        }

        [JsonProperty("panelType")]
        public string PanelType
        {
            get => _panelType;
            set => SetProperty(ref _panelType, value);
        }

        [JsonProperty("createdDate")]
        public DateTime CreatedDate
        {
            get => _createdDate;
            set => SetProperty(ref _createdDate, value);
        }

        [JsonProperty("isActive")]
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }
    }
}