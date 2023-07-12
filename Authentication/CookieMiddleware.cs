using Instakilogram.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Neo4jClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Instakilogram.Authentication
{
    public class CookieMiddleware
    {
        private readonly RequestDelegate _next;

        public CookieMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, IUserService service, IGraphClient gc)
        {
            string cookie = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (cookie != null)
                await AttachUser(context, service, cookie);

            await _next(context);
        }

   
        private async Task AttachUser(HttpContext context, IUserService service, string cookie)
        {
            try
            {
                string mail = service.CheckCookie(cookie);
                if (!String.IsNullOrEmpty(mail))
                {
                    context.Items["User"] = mail;
                    context.Items["UserType"] = service.FindUserType(mail);
                }
            }
            catch
            { }
        }
    }
}
