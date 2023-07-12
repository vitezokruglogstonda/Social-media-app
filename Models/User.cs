using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Instakilogram.Models
{
    public class User
    {
        public string UserName { get; set; }
        public string? Name { get; set; }
        public string Mail { get; set; }
        [JsonIgnore]
        public string Password { get; set; }
        [JsonIgnore]
        public string Salt { get; set; }
        public string UserType { get; set; }
        [StringLength(50)]
        public string? Description { get; set; }
        public string? ProfilePicture { get; set; }
        public bool Online { get; set; }
        [JsonIgnore]
        public int? PIN { get; set; }

        [JsonIgnore]
        public bool IsBeingFollowed { get; set; }

        public bool? IsModerator { get; set; }

        public bool? IsFollowed { get; set; }

    }
}
