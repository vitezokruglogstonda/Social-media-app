using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Instakilogram.RequestResponse
{
    public class ChangeAccountRequest
    {
        public string? UserName { get; set; }
        public string? Name { get; set; }        
        [StringLength(50)]
        public string? Description { get; set; }
        public FormFile? Picture { get; set; }
    }
}
