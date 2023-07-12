using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Instakilogram.RequestResponse
{
    public class LogInResponse
    {
        public string UserName{ get; set; }

        public string Name { get; set; }

        public string Cookie { get; set; }

        public string ProfilePicture { get; set; }
        public string? Description { get; set; }

        // public LogInResponse(string username, string name, string cookie, string profilepic = null, string description = null)
        // {
        //     this.UserName = username;
        //     this.Name = name;
        //     this.Cookie = cookie;
        //     this.ProfilePicture = profilepic;
        //     this.Description = description;
        // }

    }
}
