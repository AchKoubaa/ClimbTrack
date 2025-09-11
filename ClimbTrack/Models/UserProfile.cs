using System;
using Newtonsoft.Json;

namespace ClimbTrack.Models
{
    public class UserProfile : BaseModel
    {
        private string _id;
        private string _displayName;
        private string _email;
        private string _photoUrl;
        private string _bio;
        private string _preferredGym;
        private DateTime _createdAt;
        private DateTime _lastLoginAt;

        [JsonProperty("id")]
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        [JsonProperty("displayName")]
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        [JsonProperty("email")]
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        [JsonProperty("photoUrl")]
        public string PhotoUrl
        {
            get => _photoUrl;
            set => SetProperty(ref _photoUrl, value);
        }

        [JsonProperty("bio")]
        public string Bio
        {
            get => _bio;
            set => SetProperty(ref _bio, value);
        }

        [JsonProperty("preferredGym")]
        public string PreferredGym
        {
            get => _preferredGym;
            set => SetProperty(ref _preferredGym, value);
        }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        [JsonProperty("lastLoginAt")]
        public DateTime LastLoginAt
        {
            get => _lastLoginAt;
            set => SetProperty(ref _lastLoginAt, value);
        }

        // Proprietà calcolate
        [JsonIgnore]
        public string Initials => !string.IsNullOrEmpty(DisplayName)
            ? string.Join("", DisplayName.Split(' ').Select(s => s[0]))
            : "?";

        [JsonIgnore]
        public bool HasPhoto => !string.IsNullOrEmpty(PhotoUrl);
    }
}