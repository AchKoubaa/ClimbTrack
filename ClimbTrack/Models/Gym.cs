using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Models
{
    public class Gym : BaseModel
    {
        private string _id;
        private string _name;
        private string _address;
        private GeoPoint _location;
        private bool _hasVerticalWall;
        private bool _hasOverhangWall;
        private string _website;
        private string _phoneNumber;
        private string _imageUrl;
        private List<string> _amenities;
        private Dictionary<string, string> _openingHours;

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

        [JsonProperty("address")]
        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
        }

        [JsonProperty("location")]
        public GeoPoint Location
        {
            get => _location;
            set => SetProperty(ref _location, value);
        }

        [JsonProperty("hasVerticalWall")]
        public bool HasVerticalWall
        {
            get => _hasVerticalWall;
            set => SetProperty(ref _hasVerticalWall, value);
        }

        [JsonProperty("hasOverhangWall")]
        public bool HasOverhangWall
        {
            get => _hasOverhangWall;
            set => SetProperty(ref _hasOverhangWall, value);
        }

        [JsonProperty("website")]
        public string Website
        {
            get => _website;
            set => SetProperty(ref _website, value);
        }

        [JsonProperty("phoneNumber")]
        public string PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }

        [JsonProperty("imageUrl")]
        public string ImageUrl
        {
            get => _imageUrl;
            set => SetProperty(ref _imageUrl, value);
        }

        [JsonProperty("amenities")]
        public List<string> Amenities
        {
            get => _amenities;
            set => SetProperty(ref _amenities, value);
        }

        [JsonProperty("openingHours")]
        public Dictionary<string, string> OpeningHours
        {
            get => _openingHours;
            set => SetProperty(ref _openingHours, value);
        }
    }

    public class GeoPoint : BaseModel
    {
        private double _latitude;
        private double _longitude;

        [JsonProperty("latitude")]
        public double Latitude
        {
            get => _latitude;
            set => SetProperty(ref _latitude, value);
        }

        [JsonProperty("longitude")]
        public double Longitude
        {
            get => _longitude;
            set => SetProperty(ref _longitude, value);
        }
    }
}
