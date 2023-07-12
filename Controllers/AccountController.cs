using Instakilogram.Models;
using Instakilogram.RequestResponse;
using Instakilogram.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Neo4jClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Instakilogram.Authentication;
using System.IO;

namespace Instakilogram.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private IUserService Service;
        private IGraphClient Neo;
        private URLs URL;
        private MailSettings Mail;

        public AccountController(IUserService service, IGraphClient gc, IOptions<URLs> url, IOptions<MailSettings> mailSettings)
        {
            this.Service = service;
            this.Neo = gc;
            this.URL = url.Value;
            this.Mail = mailSettings.Value;
        }
                
        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> SignUp([FromForm] SignUpRequest request)
        {

            if (this.Service.UserExists(request.UserName, request.Mail))
            {
                return BadRequest(new { message = "Korisnik vec postoji. Probajte drugi mail ili korisnicko ime, ili izvrsiti validaciju putem mejla." });
            }

            string hash, salt;
            this.Service.PasswordHash(out hash, out salt, request.Password);

            User newUser = new User
            {
                UserName = request.UserName,
                Name = request.Name,
                Mail = request.Mail,
                Password = hash,
                Salt = salt,
                UserType = IUserService.UserType.Standard.ToString(),
                Description = request.Description,
                ProfilePicture = "",
                Online = false,
                PIN = null
            };
         
            if (request.Picture == null)
            {
                this.Service.TmpStoreAccount(newUser);
            }
            else
            {
                this.Service.TmpStoreAccount(newUser, request.Picture);
            }

            return Ok(new { message = "Poslat vam je mail za validaciju." });
        }

        [HttpPost]
        [Route("Verify/{key}")]
        public async Task<IActionResult> ValidateAccount(string key)
        {
            string link = this.Service.ApproveAccount(key);
            if (!String.IsNullOrEmpty(link))
            {
                return Ok(new { message = "Uspesno odobren" });
            }
            return BadRequest(new { message = "Doslo je do greske."});
        }

        [Auth]
        [HttpPost]
        [Route("ChangeAccountInfo")]
        public async Task<IActionResult> ChangeAccountInfo([FromForm] ChangeAccountRequest request)
        {
            string mail = (string)HttpContext.Items["User"];

            Neo4jClient.Cypher.ICypherFluentQuery query = this.Neo.Cypher
                    .Match("(n:User)")
                    .Where((User n) => n.Mail == mail);

           
            if (request != null)
            {

                if (!String.IsNullOrEmpty(request.UserName))
                {
                    if (this.Service.UserExists(request.UserName))
                    {
                        return BadRequest(new { message = "Korisnik vec postoji. Probajte drugi mail ili korisnicko ime, ili izvrsiti validaciju putem mejla." });
                    }
                    query.Set("n.userName: $new_user_name")
                        .WithParam("new_user_name", request.UserName);
                }
                if (!String.IsNullOrEmpty(request.Name))
                {
                    query.Set("n.name: $new_name")
                        .WithParam("new_name", request.Name);
                }
                if (!String.IsNullOrEmpty(request.Description))
                {
                    query.Set("n.description: $new_description")
                        .WithParam("new_description", request.Description);
                }
                await query.ExecuteWithoutResultsAsync();
            }
            if (request.Picture != null)
            {
             
                ImageAsBase64 picture = new ImageAsBase64(this.Service.FormFileToBase64(request.Picture));

                User user = query.Return(n => n.As<User>())
                    .ResultsAsync.Result.ToList().Single();
                this.Service.DeleteImage(user.ProfilePicture, IUserService.ImageType.Profile);
                await query.Set("n.profilePicture: $new_profile_picture")
                    .ExecuteWithoutResultsAsync();
            }

            return Ok();

        }

        [Auth]
        [HttpPost]
        [Route("PasswordReset")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordChangeRequest request)
        {
            string mail = (string)HttpContext.Items["User"];

            Neo4jClient.Cypher.ICypherFluentQuery query = this.Neo.Cypher
                    .Match("(n:User)")
                    .Where((User n) => n.Mail == mail);

            User user = query.Return(n => n.As<User>())
                    .ResultsAsync.Result.ToList().Single();

            if (!this.Service.CheckPassword(user.Password, user.Salt, request.Old))
            {
                return BadRequest(new { message = "Pogresna sifra." });
            }

            string hash, salt;
            this.Service.PasswordHash(out hash, out salt, request.New);

          
            await query.Set("n.password: $new.new_pass, n.salt: $new.new_salt")
                .WithParam("new", new {new_pass = hash, new_salt = salt})
                .ExecuteWithoutResultsAsync();

            return Ok(new { message = "Uspesno promenjena sifra." });

        }

        [HttpPost]
        [Route("PasswordRecoverRequest")]
        public async Task<IActionResult> PasswordRecoverRequest([FromBody] string mail)
        {
            if(!this.Service.UserExists("", mail))
            {
                return BadRequest(new { message = "Pogresan mejl." });
            }

            User user = this.Neo.Cypher
                    .Match("(n:User)")
                    .Where((User n) => n.Mail == mail)
                    .Return(u=>u.As<User>())
                    .ResultsAsync.Result.ToList().Single();

            IUserService.MailType mail_type = IUserService.MailType.ResetPassword;
            this.Service.SendMail(user, mail_type);

            return Ok(new { message = "Poslat vam je mejl za promenu sifre." });
        }

        [HttpPost]
        [Route("PasswordRecover")]
        public async Task<IActionResult> PasswordRecover([FromBody] PasswordRecoverRequest request)
        {
            if(this.Service.CheckPin(request.Mail, request.PIN))
            {
                return BadRequest(new { message = "Pogresan pin." });
            }

            Neo4jClient.Cypher.ICypherFluentQuery query = this.Neo.Cypher
                    .Match("(n:User)")
                    .Where((User n) => n.Mail == request.Mail);

            User user = query.Return(u => u.As<User>())
                .ResultsAsync.Result.ToList().Single();

            string hash, salt;
            this.Service.PasswordHash(out hash, out salt, request.NewPassword);

         
            await query.Set("n.password: $new.new_pass, n.salt: $new.new_salt")
                .WithParam("new", new { new_pass = hash, new_salt = salt })
                .ExecuteWithoutResultsAsync();

            return Ok(new { message = "Uspesno promenjena sifra." });
        }

        [HttpPost]
        [Route("LogIn")]
        public async Task<IActionResult> SignIn([FromForm] LogInRequest request)
        {
            if(this.Service.UserExists("",request.Mail))
            {
                var query = this.Neo.Cypher
                    .Match("(u:User)")
                    .Where((User u) => u.Mail == request.Mail);

                User user = query.Return(u => u.As<User>())
                    .ResultsAsync.Result.ToList().Single();

                if(this.Service.CheckPassword(user.Password, user.Salt, request.Password))
                {
                    string cookie = this.Service.GenerateCookie();
                    this.Service.StoreCookie(cookie, user.Mail);
                    await query.Set("u.online = 'true'").ExecuteWithoutResultsAsync(); 
                    
                    LogInResponse response = new LogInResponse
                    {
                        UserName = user.UserName,
                        Name = user.Name,
                        Description = user.Description,
                        Cookie = cookie,
                        ProfilePicture = this.URL.ProfileImagesPath + user.ProfilePicture
                    };

                    return Ok(response);
                }
                else
                {
                    return BadRequest(new {message = "Pogresna sifra."});    
                }
            }
            else
            {
                return BadRequest(new {message = "Korisnik ne postoji, pogresan mail."});
            }
        }

        [Auth]
        [HttpPost]
        [Route("LogOut")]
        public async Task<IActionResult> LogOut()
        {
            string mail = (string)HttpContext.Items["User"];

            await this.Neo.Cypher
                .Match("(u:User)")
                .Where((User u) => u.Mail == mail)
                .Set("u.online = 'false'")
                .ExecuteWithoutResultsAsync();

            this.Service.DeleteCookie(HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last());

            return Ok();
        }
        
        [HttpPost]
        [Route("CreateAdmin/{code}")]
        public async Task<IActionResult> CreateAdmin(string code)
        {

            if (this.Service.UserExists(this.Mail.Name, this.Mail.Address))
            {
                return BadRequest(new { message = "Administrator vec postoji." });
            }

            string hash, salt;
            this.Service.PasswordHash(out hash, out salt, this.Mail.Password);

            User admin = new User
            {
                UserName = this.Mail.Name,
                Name = this.Mail.Name,
                Mail = this.Mail.Address,
                Password = hash,
                Salt = salt,
                UserType = IUserService.UserType.Admin.ToString(),
                Description = "",
                ProfilePicture = "default.png",
                Online = false,
                PIN = null
            };

            this.Service.StoreAdminAccount(admin);

            return Ok(new { message = "Administrator uspesno kreiran." });
        }
    }
}
