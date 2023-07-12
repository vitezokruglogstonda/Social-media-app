using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Instakilogram.RequestResponse
{
    public class PasswordChangeRequest
    {
        [Required]
        public string Old { get; set; }
        [Required]
        public string New { get; set; }
    }
}
