using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Instakilogram.Models
{
    public class Photo
    {
        public Photo()
        {
         
        }

        public string Title { get; set; }
        public string Path { get; set; }
        public DateTime TimePosted { get; set; }
        public string? Description { get; set; }
        public int NumberOfLikes { get; set; }
        public bool? IsLiked { get; set; }

        public string? Uploader { get; set; }
        public string? TaggedUsers { get; set; }
        public string? Hashtags { get; set; }
    }
}
