using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
//using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Instakilogram.Config;
using Instakilogram.Service;
using Instakilogram.Authentication;
//using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using Neo4jClient;
using System;
using StackExchange.Redis;
using Instakilogram.Service;

namespace Instakilogram
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        //public Neo4jConfig Neo4jConf;//{ get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            // In production, the Angular files will be served from this directory
            //services.AddSpaStaticFiles(configuration =>
            //{
            //    configuration.RootPath = "ClientApp/dist";
            //});

            services.AddCors(options =>
            {
                options.AddPolicy("CORS", builder =>
                {
                    builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
                });
            });

            services.Configure<URLs>(Configuration.GetSection("URLs"));
            services.Configure<MailSettings>(Configuration.GetSection("MailSettings"));
      
            //services.Configure<Neo4jConfig>(Configuration.GetSection("NeO4jConnectionSettings"));
            services.AddScoped<IUserService, UserService>();

            //konekcija na neo4j

            //GraphClient client = new GraphClient(new Uri(this.Neo4jConf.Server), this.Neo4jConf.UserName, this.Neo4jConf.Password);
            var client = new BoltGraphClient(new Uri("bolt://localhost:7687"), "neo4j", "neo");
            client.ConnectAsync();
            services.AddSingleton<IGraphClient>(client);
            services.AddSingleton(Configuration.GetSection("Moderation").Get<Moderation>());
            //konekcija na redis

            var multiplexer = ConnectionMultiplexer.Connect("localhost"); //port:6379
            services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            if (!env.IsDevelopment())
            {
                //app.UseSpaStaticFiles();
            }

            app.UseRouting();
            app.UseCors("CORS");

            app.UseMiddleware<CookieMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });

            //app.UseSpa(spa =>
            //{
            //    // To learn more about options for serving an Angular SPA from ASP.NET Core,
            //    // see https://go.microsoft.com/fwlink/?linkid=864501

            //    spa.Options.SourcePath = "ClientApp";

            //    if (env.IsDevelopment())
            //    {
            //        //spa.UseAngularCliServer(npmScript: "start");
            //    }
            //});
        }
    }
}
