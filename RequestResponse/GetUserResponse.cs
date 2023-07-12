using Instakilogram.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Instakilogram.RequestResponse
{
    public class GetUserResponse
    {
        public User User{ get; set; }
        public List<Photo> UploadedPhotos { get; set; }
        public List<Photo> TaggedPhotos { get; set; }
    }
}
