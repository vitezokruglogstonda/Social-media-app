using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Instakilogram.Service
{
    public class MailSettings
    {
        public string Address { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string Subject { get; set; }
        public string AdminCode { get; set; }
    }
}
