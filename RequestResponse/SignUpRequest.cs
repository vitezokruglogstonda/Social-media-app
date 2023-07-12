using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Instakilogram.RequestResponse
{
    public class SignUpRequest
    {
        [Required]
        public string Mail { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public IFormFile? Picture { get; set; }

    }
}
