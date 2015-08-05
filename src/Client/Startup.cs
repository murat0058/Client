using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Thinktecture.IdentityModel.Client;
using System.Security.Claims;
using Microsoft.AspNet.Authentication.Cookies;
using System.Net.Http;

namespace Client
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDataProtection();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCookieAuthentication(options =>
            {
                options.AuthenticationScheme = "Cookies";
                options.AutomaticAuthentication = true;
            });

            app.Map("/code", application =>
            {
                application.Use((context, next) =>
                {

                    var client = new OAuth2Client(new Uri("http://localhost:5000/core/connect/authorize"));

                    var url = client.CreateCodeFlowUrl(clientId: "0011FF", redirectUri: "http://localhost:5001/callback", scope: "read write");

                    context.Response.Redirect(url);

                    return Task.FromResult(0);
                    
                });
            });

            app.Map("/callback", application =>
            {
                application.Use(next => async context =>
               {
                   var client = new OAuth2Client(new Uri("http://localhost:5000/core/connect/token"), "0011FF", "ABCDEFG");

                   var code = context.Request.Query["code"];

                   TokenResponse response = await client.RequestAuthorizationCodeAsync(code, "http://localhost:5001/callback");

                   if (!string.IsNullOrEmpty(response.AccessToken))
                   {
                       List<Claim> claims = new List<Claim>();
                       claims.Add(new Claim("access_token", response.AccessToken));
                       claims.Add(new Claim("expires_at", (DateTime.UtcNow.ToEpochTime() + response.ExpiresIn).ToDateTimeFromEpoch().ToString()));

                       ClaimsIdentity id = new ClaimsIdentity(claims, "cookie");
                       ClaimsPrincipal principal = new ClaimsPrincipal(id);

                       context.Response.SignIn("Cookies", principal);
                   }
               });
            });

            app.Map("/info", application =>
            {
                application.Use(next => async context =>
                 {
                     ClaimsPrincipal principal = context.User;

                     await context.Response.WriteAsync(principal.FindFirst("access_token").Value);
                 });
            });

            app.Map("/call", application =>
            {
                application.Use(next => async context =>
                 
                
                {
                     HttpClient client = new HttpClient();

                     string accessToken = context.User.FindFirst("access_token").Value;

                     client.SetBearerToken(accessToken);


                     string response = await client.GetStringAsync("http://localhost:5002/action");
                    await context.Response.WriteAsync(response);

                });
            });


        }
    }
}
