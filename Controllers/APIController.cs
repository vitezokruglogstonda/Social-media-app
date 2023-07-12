using Instakilogram.Models;
using Instakilogram.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using Neo4jClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Instakilogram.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Instakilogram.RequestResponse;
using StackExchange.Redis;

namespace Instakilogram.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class APIController : ControllerBase
    {
        private IGraphClient Neo;
        public IHostingEnvironment hostingEnvironment;
        private IUserService Service;
        private IConnectionMultiplexer Redis;

        public APIController(IGraphClient gc, IHostingEnvironment hostingEnv, IUserService service, IConnectionMultiplexer redis)
        {
            this.Neo = gc;
            hostingEnvironment = hostingEnv;
            Service = service;
            Redis = redis;
        }

        [HttpGet]
        [Route("GetPhoto/{photoName}")]
        public async Task<IActionResult> GetPhoto(string photoName)
        {
            string Mail = (string)HttpContext.Items["User"];
            string picture = photoName;

            var qphoto = await this.Neo.Cypher
                .Match("(p:Photo)")
                .Where((Photo p) => p.Path == picture)
                .Return(p => p.As<Photo>())
                .ResultsAsync;

                        Photo photo;

            if (qphoto.Count() == 0)
            {
                photo = null;
            }
            else
            {

                photo = qphoto.Single();
                if (photo != null) photo = Service.ComputePhotoProp(Mail, photo);
            }

            return Ok(photo);

        }

        [HttpGet]
        [Route("GetFeed24h")] 
        public async Task<IActionResult> GetFeed24h()
        {
            string Mail = (string)HttpContext.Items["User"];

            var usersFollowed = await this.Neo.Cypher
                .Match("(a:User)-[:FOLLOWS]->(b:User)")
                .Where((User a) => a.Mail == Mail)
                .Return<User>("b").ResultsAsync;

            var photos = new List<Photo>();
            foreach (User u in usersFollowed)
            {

                DateTime now = DateTime.Now;
                var phList = await this.Neo.Cypher
                    .Match("(a:User{UserName:$nameParam})-[:UPLOADED]->(p:Photo)")
                    .WithParam("nameParam", u.UserName)
                    .Return<Photo>("p").ResultsAsync;

                var photolist = phList.ToList<Photo>();
                for (int i = 0; i < photolist.Count(); i++)
                {
                    if (true || Service.IsFromLast24h(photolist[i].TimePosted))                     {
                        photolist[i] = Service.ComputePhotoProp(Mail, photolist[i]);
                        var ph = photolist[i];
                        photos.Add(ph);
                    }
                }

            }
            return Ok(photos);
        }

        [HttpPost]
        [Route("FollowUser/{usernameToFollow}")]
        public async Task<IActionResult> FollowUser(string usernameToFollow)
        {
            string Mail = (string)HttpContext.Items["User"];

            await this.Neo.Cypher
                .Match("(a:User),(b:User)")
                .Where("a.Mail = $userA AND b.UserName = $userB")
                .WithParams(new { userA = Mail, userB = usernameToFollow })
                .Merge("(a)-[r:FOLLOWS]->(b)")
                .ExecuteWithoutResultsAsync();
            return Ok();
        }

        [HttpDelete]
        [Route("UnfollowUser/{usernameToUnfollow}")]
        public async Task<IActionResult> UnfollowUser(string usernameToUnfollow)
        {
            string Mail = (string)HttpContext.Items["User"];
            await this.Neo.Cypher
                .Match("(a:User)-[r:FOLLOWS]->(b:User)")
                .Where("a.Mail = $userA AND b.UserName = $userB")
                .WithParams(new { userA = Mail, userB = usernameToUnfollow })
                .Delete("r")
                .ExecuteWithoutResultsAsync();
            return Ok();
        }

        [HttpPost]
        [Route("FollowHashtag/{hashtagToFollow}")]
        public async Task<IActionResult> FollowHashtag(string hashtagToFollow)
        {
            string Mail = (string)HttpContext.Items["User"];

            await this.Neo.Cypher
                .Match("(a:User),(b:Hashtag)")
                .Where("a.Mail = $userA AND b.Title = $hashtagB")
                .WithParams(new { userA = Mail, hashtagB = hashtagToFollow })
                .Merge("(a)-[r:FOLLOWS]->(b)")
                .ExecuteWithoutResultsAsync();
            return Ok();
        }

        [HttpDelete]
        [Route("UnfollowHashtag/{hashtagToUnfollow}")]
        public async Task<IActionResult> Unfollow(string hashtagToUnfollow)
        {
            string Mail = (string)HttpContext.Items["User"];

            await this.Neo.Cypher
                .Match("(a:User)-[r:FOLLOWS]->(b:Hashtag)")
                .Where("a.Mail = $userA AND b.Title = $hashtagB")
                .WithParams(new { userA = Mail, hashtagB = hashtagToUnfollow })
                .Delete("r")
                .ExecuteWithoutResultsAsync();
            return Ok();
        }

        [HttpPost]
        [Route("UploadProfilePic")]
        public async Task<IActionResult> UploadProfilePic(IFormFile file)
        {
            string Mail = (string)HttpContext.Items["User"];

            if (!file.ContentType.Contains("image"))
            {
                return Ok("bad image");
            }

            var extension = "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
            string fileName = DateTime.Now.Ticks + extension; 
            var pathBuilt = Path.Combine(Directory.GetCurrentDirectory(), "Upload\\callerUsername\\profilepics");

            if (!Directory.Exists(pathBuilt))
            {
                Directory.CreateDirectory(pathBuilt);
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), "Upload\\callerUsername\\profilepics",
                fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok();
        }

        [HttpGet]
        [Route("Search/{username}")]
        public async Task<IActionResult> Search(string username)
        {
            string Mail = (string)HttpContext.Items["User"];

            var matchingUsers = this.Neo.Cypher
               .Match("(a:User)")
               .Where((User a) => a.UserName.Contains(username))
               .Return<User>("a").ResultsAsync.Result.ToList<User>();

            for (int i = 0; i < matchingUsers.Count(); i++)
            {
                matchingUsers[i] = this.Service.ComputeUserFollowB(Mail, matchingUsers[i]);
            }
            return Ok(matchingUsers);
        }

        [HttpGet]
        [Route("GetRecommendedUsers")]
        public async Task<IActionResult> GetRecommendedUsers()
        {
            string Mail = (string)HttpContext.Items["User"];

            int minimumConnectedPeople = 1; 
            Dictionary<User, int> peopleToRecommend = new Dictionary<User, int>();

            var myFriendList = await this.Neo.Cypher
              .Match("(a:User)-[:FOLLOWS]->(b:User)")
              .Where((User a) => a.Mail == Mail)
              .Return<User>("b").ResultsAsync;

            foreach (User friend in myFriendList)
            {
                var friend_friendList = await this.Neo.Cypher
                    .Match("(a:User)-[:FOLLOWS]->(b:User)")
                    .Where((User a) => a.UserName == friend.UserName)
                    .Return<User>("b").ResultsAsync;

                foreach (User friendOfFriend in friend_friendList)
                {
                    if (!myFriendList.Contains(friendOfFriend) && friendOfFriend.Mail != Mail)
                    {
                        int currentCount;

                       peopleToRecommend.TryGetValue(friendOfFriend, out currentCount);

                        peopleToRecommend[friendOfFriend] = currentCount + 1;
              
                    }
                }
            }

            var matchedKvp = peopleToRecommend.Where(kvp => kvp.Value >= minimumConnectedPeople);
            var matches = (from kvp in matchedKvp select kvp.Key).ToList();

            return Ok(matches);
        }

        [HttpGet]
        [Route("GetUser/{userName}")]
        public async Task<IActionResult> GetUser(string userName)
        {
            string Mail = (string)HttpContext.Items["User"];

            var user_query = await this.Neo.Cypher
                .Match("(a:User)")
                .Where((User a) => a.UserName == userName)
                .Return(a => a.As<User>())
                .ResultsAsync;

            User user = user_query.Count() == 0 ? null : user_query.Single();

            var photos_query = await this.Neo.Cypher
               .Match("(a:User{UserName:$nameParam})-[:UPLOADED]->(p:Photo)")
               .WithParam("nameParam", userName)
               .Return(p => p.CollectAs<Photo>())
               .ResultsAsync;
            List<Photo> uploadedPhotos = photos_query.Count() == 0 ? null : photos_query.ToList().Single().ToList();

            if (uploadedPhotos != null)
            {
                for (int i = 0; i < uploadedPhotos.Count(); i++)
                {
                    uploadedPhotos[i] = Service.ComputePhotoProp(Mail, uploadedPhotos[i]);
                }
            }

            var taggedOnPhotos_query = await this.Neo.Cypher
                .Match("(p:Photo)-[:TAGS]->(a:User{UserName:$nameParam})")
                .WithParam("nameParam", userName)
                .Return(p => p.CollectAs<Photo>())
                .ResultsAsync;
            List<Photo> taggedOnPhotos = taggedOnPhotos_query.Count() == 0 ? null : taggedOnPhotos_query.ToList().Single().ToList();

            if (taggedOnPhotos != null)
            {
                for (int i = 0; i < taggedOnPhotos.Count(); i++)
                {
                    taggedOnPhotos[i] = Service.ComputePhotoProp(Mail, taggedOnPhotos[i]); ;
                }

            }
            user = this.Service.ComputeUserFollowB(Mail, user);
            return Ok(new GetUserResponse
            {
                User = user,
                UploadedPhotos = uploadedPhotos,
                TaggedPhotos = taggedOnPhotos
            });
        }

        [HttpGet]
        [Route("GetHtagImages/{title}")]
        public async Task<IActionResult> GetHtagImages(string title)
        {
            string Mail = (string)HttpContext.Items["User"];

            var photos = await this.Service.GetHtagImages(Mail, title);

            return Ok(photos);
        }
        [HttpGet]
        [Route("GetHtagFeed24h")]
        public async Task<IActionResult> GetHtagFeed24h()
        {
            string Mail = (string)HttpContext.Items["User"];

                        var htagsFollowed = await this.Neo.Cypher
               .Match("(a:User)-[:FOLLOWS]->(b:Hashtag)")
               .Where((User a) => a.Mail == Mail)
               .Return<Hashtag>("b").ResultsAsync;

            HashSet<Photo> combinedphotos = new HashSet<Photo>();

            foreach (Hashtag h in htagsFollowed)
            {
                var singleHphotos = await this.Service.GetHtagImages(Mail, h.Title);
                if (singleHphotos != null)
                {
                    for (int i = 0; i < singleHphotos.Count(); i++)
                    {
                        if (!(true || Service.IsFromLast24h(singleHphotos[i].TimePosted))) //remove true in production
                        {
                            singleHphotos.Remove(singleHphotos[i]);
                        }
                        else
                        {
                            singleHphotos[i] = Service.ComputePhotoProp(Mail, singleHphotos[i]);
                            combinedphotos.Add(singleHphotos[i]);
                        }
                    }
                }

            }
            return Ok(combinedphotos.ToList<Photo>());
        }
        [HttpGet]
        [Route("GetNew12")]
        public async Task<IActionResult> GetNew12()
        {
            string Mail = (string)HttpContext.Items["User"];
            var db = this.Redis.GetDatabase();

            var listOfPhotos = new List<Photo>();

            if (db.KeyExists("latest12"))
            {

                var photos = db.ListRange("latest12", 0, 11);
                foreach (var rv in photos)
                {

                    Photo photo = JsonConvert.DeserializeObject<Photo>(rv);
                    photo = this.Service.ComputePhotoProp(Mail, photo);
                    listOfPhotos.Add(photo);
                }

            }
            return Ok(listOfPhotos);
        }
        [HttpGet]
        [Route("GetLiked")]
        public async Task<IActionResult> GetLiked()
        {
            string Mail = (string)HttpContext.Items["User"];

            var phList = await this.Neo.Cypher
                    .Match("(a:User{Mail:$ma})-[:LIKES]->(p:Photo)")
                    .WithParam("ma", Mail)
                    .Return<Photo>("p").ResultsAsync;

            var photolist = phList.ToList<Photo>();
            for (int i = 0; i < photolist.Count(); i++)
            {
                photolist[i] = Service.ComputePhotoProp(Mail, photolist[i]);
            }

            return Ok(photolist);
        }

        [HttpGet]
        [Route("GetTop")]
        public async Task<IActionResult> GetTop()
        {
            string Mail = (string)HttpContext.Items["User"];
            var phList = await this.Neo.Cypher
              .Match("(a:Photo)")
              .With("a")
           
              .Return<Photo>("a")
              .OrderBy("a.NumberOfLikes DESC")
              .Limit(12).ResultsAsync;

            var photolist = phList.ToList<Photo>();
            for (int i = 0; i < photolist.Count(); i++)
            {
                photolist[i] = Service.ComputePhotoProp(Mail, photolist[i]);
            }

            return Ok(phList);
        }

        [HttpGet]
        [Route("SearchHtag/{title}")]
        public async Task<IActionResult> SearchHtag(string title)
        {
            string Mail = (string)HttpContext.Items["User"];

            var matchingHtags = await this.Neo.Cypher
                .Match("(h:Hashtag)")
                .Where((Hashtag h) => h.Title.ToLower().Contains(title.ToLower()))
                .Return<Hashtag>("h").ResultsAsync;

            var matchingHtagsC = matchingHtags.ToList<Hashtag>();
            for (int i = 0; i < matchingHtagsC.Count(); i++)
            {
                matchingHtagsC[i] = Service.ComputeUserFollowH(Mail, matchingHtagsC[i]);
            }
            return Ok(matchingHtagsC);

        }

        [HttpGet]
        [Route("GetRecommendedHtags")]
        public async Task<IActionResult> GetRecommendedHtags()
        {
            string Mail = (string)HttpContext.Items["User"];

            int minimumConnectedPeople = 1; //how much friends need to follow Htag to recommend it

            Dictionary<Hashtag, int> htagsToRecommend = new Dictionary<Hashtag, int>();

            var myHtagList = await this.Neo.Cypher
             .Match("(a:User)-[:FOLLOWS]->(b:Hashtag)")
             .Where((User a) => a.Mail == Mail)
             .Return<Hashtag>("b").ResultsAsync;

            var myFriendList = await this.Neo.Cypher
              .Match("(a:User)-[:FOLLOWS]->(b:User)")
              .Where((User a) => a.Mail == Mail)
              .Return<User>("b").ResultsAsync;

            foreach (User friend in myFriendList)
            {
                var friend_HtagList = await this.Neo.Cypher
                    .Match("(a:User)-[:FOLLOWS]->(h:Hashtag)")
                    .Where((User a) => a.UserName == friend.UserName)
                    .Return<Hashtag>("h").ResultsAsync;

                foreach (Hashtag friends_Htag in friend_HtagList)
                {
                    if (!myHtagList.Contains(friends_Htag))
                    {
                        int currentCount;

                        htagsToRecommend.TryGetValue(friends_Htag, out currentCount);

                        htagsToRecommend[friends_Htag] = currentCount + 1;

                    }
                }
            }

            var matchedKvp = htagsToRecommend.Where(kvp => kvp.Value >= minimumConnectedPeople);
            var matches = (from kvp in matchedKvp select kvp.Key).ToList();

            return Ok(matches);
        }

    } 
}
