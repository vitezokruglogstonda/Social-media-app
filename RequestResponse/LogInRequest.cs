using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Instakilogram.RequestResponse
{
    public class LogInRequest
    {
        [Required]
        public string Mail { get; set; }

        [Required]
        public string Password { get; set; }

    }
}
