using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Instakilogram.RequestResponse
{
    public class PasswordRecoverRequest
    {
        [Required]
        public string Mail { get; set; }
        [Required]
        public string NewPassword { get; set; }
        [Required]
        public int PIN { get; set; }
    }
}
