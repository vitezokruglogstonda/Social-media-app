using Instakilogram.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Instakilogram.Authentication
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthAttribute : Attribute, IAuthorizationFilter
    {
        public bool JustAdmin { get; set; }
        public AuthAttribute()
        {
            this.JustAdmin = false;
        }
        public AuthAttribute(string type)
        {
            if (String.Equals(type, IUserService.UserType.Admin.ToString()))
            {
                this.JustAdmin = true;
            }
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            string mail = (string)context.HttpContext.Items["User"];
            if (mail == null)
            {
                context.Result = new JsonResult(new { message = "Neovlascen" }) { StatusCode = StatusCodes.Status401Unauthorized };
            }
            else if (this.JustAdmin && !String.Equals(IUserService.UserType.Admin.ToString(), (string)context.HttpContext.Items["UserType"]))
            {
                context.Result = new JsonResult(new { message = "Neovlascen" }) { StatusCode = StatusCodes.Status401Unauthorized };
            }
        }
    }
}
