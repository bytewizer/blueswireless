using System.Diagnostics;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;

namespace Bytewizer.Notecard.Terminal.WebUI
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseFileServer();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/request", async context =>
                {
                    var response = "{  \"hours\": 65,  \"mode\": \"usb\",  \"value\": 5.006489616652111,  \"vmin\": 3.579999999999999,  \"vmax\": 5.27,  \"vavg\": 4.965538461538461}";

                    if (context.Request.Query.TryGetValue("request", out StringValues request))
                    {
                        Debug.WriteLine($"request: {request}");
                        
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(response);
                    }
                });
            });
        }
    }
}
