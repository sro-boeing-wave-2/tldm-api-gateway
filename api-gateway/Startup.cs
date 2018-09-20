using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Ocelot.Provider.Consul;
using System;
using Consul;
using Chilkat;
using Microsoft.AspNetCore.Http;

namespace api_gateway
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddOcelot(Configuration).AddConsul(); ;
            services.AddSignalR();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }
            app.Use(async (context, next) =>
            {
                Console.WriteLine("hello" + "\n");
                if (context.Request.Query.TryGetValue("access_token", out var token1))
                {
                    context.Request.Headers.Add("Authorization", $"Bearer {token1}");
                }
                if (context.Request.Path == "/onboard/login" || context.Request.Path == "/onboard/create/workspace" || context.Request.Path == "/onboard/create/workspace/email" || context.Request.Path == "/onboard/workspacedetails" || context.Request.Path == "/onboard/verify" || context.Request.Path == "/onboard/invite/verify")
                {
                    await next();
                }

                Chilkat.Global global = new Chilkat.Global();
                global.UnlockBundle("Anything for 30-day trail");
                Chilkat.Jwt jwt = new Chilkat.Jwt();

                using (var client = new ConsulClient())
                {

                    var getPair = await client.KV.Get("secretkey");
                    string token = context.Request.Headers["Authorization"];

                    Console.WriteLine(token + "\n");
                    if (token != null)
                    {
                        var x = token.Replace("Bearer ", "");
                        Console.WriteLine(x + "\n");
                        Rsa rsaPublicKey = new Rsa();
                        rsaPublicKey.ImportPublicKey(Encoding.UTF8.GetString(getPair.Response.Value));

                        Console.WriteLine(Encoding.UTF8.GetString(getPair.Response.Value) + "\n");

                        var isTokenVerified = jwt.VerifyJwtPk(x, rsaPublicKey.ExportPublicKeyObj());

                        Console.WriteLine(rsaPublicKey.ExportPublicKeyObj() + "\n");

                        Console.WriteLine(isTokenVerified + "\n");
                        if (isTokenVerified)
                        {
                            Console.WriteLine(isTokenVerified + "\n");
                            await next();
                        }
                        else
                        {
                            await context.Response.WriteAsync("unauthorized");
                        }
                    }
                }
            });
            app.UseOcelot().Wait();
            app.UseWebSockets();
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
