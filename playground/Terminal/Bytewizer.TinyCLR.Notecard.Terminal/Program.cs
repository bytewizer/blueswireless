using System;
using System.Diagnostics;

using Bytewizer.TinyCLR.Http;
using Bytewizer.TinyCLR.Notecard.Terminal.Properties;

using GHIElectronics.TinyCLR.Devices.Network;

namespace Bytewizer.TinyCLR.Notecard.Terminal
{
    class Program
    {
        private static HttpServer _httpServer;

        static void Main()
        {
            // Initialize wireless connection using a wifi network
            NetworkProvider.Initialize("ssid", "password"); // Set your wifi credentials

            // Initialize a direct one to one connection with to the notecard
            //NetworkProvider.Initialize("blues-wireless", "", WiFiMode.AccessPoint); // Set an empty password for a open network
            NetworkProvider.Controller.NetworkAddressChanged += NetworkAddressChanged;

            // Initialize Blues Wireless Notecard
            NotecardProvider.Initialize();

            // Load webpage resource files into memory
            var logoFile = Resources.GetBytes(Resources.BinaryResources.Logo);
            var indexFile = Resources.GetBytes(Resources.BinaryResources.Index);
            var iconFile = Resources.GetBytes(Resources.BinaryResources.Favicon);

            // Setup webserver to deliver files and request endpoint
            _httpServer = new HttpServer(options =>
            {
                options.Pipeline(app =>
                {
                    app.UseCors();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        // Endpoint for main index.html page
                        endpoints.Map("/", context =>
                        {
                            context.Response.Headers.Add(HeaderNames.CacheControl, "no-cache");
                            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                            context.Response.SendFile(indexFile, "text/html; charset=UTF-8", "index.html");
                        });

                        // Endpoint for favicon
                        endpoints.Map("/images/favicon.ico", context =>
                        {
                            context.Response.Headers.Add(HeaderNames.CacheControl, "max-age=31536000, immutable");
                            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                            context.Response.SendFile(iconFile, "image/x-icon", "favicon.ico");
                        });

                        // Endpoint for logo
                        endpoints.Map("/images/logo.png", context =>
                        {
                            context.Response.Headers.Add(HeaderNames.CacheControl, "max-age=31536000, immutable");
                            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                            context.Response.SendFile(logoFile, "image/png", "logo.png");
                        });

                        // Endpoint for getting request results
                        endpoints.Map("/request", context => 
                        {
                            if (context.Request.Method != HttpMethods.Post)
                            {
                                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                                return;
                            }

                            var query = context.Request.ReadFromUrlEncoded();
                            if (query != null)
                            {
                                if (query.TryGetValue("request", out string request))
                                {
                                    context.Response.Headers.Add(HeaderNames.CacheControl, "no-cache");
                                    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                                    context.Response.ContentType = "application/json; charset=utf-8";

                                    try
                                    {
                                        // This is enable by default and removes whitespace and validates basic json
                                        // message structure setting this to false will process the request exactly as provided 
                                        //NotecardProvider.Controller.ValidateRequest = false;

                                        var results = NotecardProvider.Controller.Request(request);
                                        context.Response.Write(results.Response);
                                    }
                                    catch (Exception ex)
                                    {
                                        var jsonMessage = "{\"err\":\"" + ex.Message.ToLower() + "\"}";
                                        context.Response.Write(jsonMessage);

                                        NotecardProvider.Controller.Reset();
                                    }
                                }
                            }
                        });
                    });
                });
            });
            
            _httpServer.Start();
        }

        private static void NetworkAddressChanged(
            NetworkController sender,
            NetworkAddressChangedEventArgs e)
        {
            var ipProperties = sender.GetIPProperties();
            var address = ipProperties.Address.GetAddressBytes();

            if (address != null && address[0] != 0 && address.Length > 0)
            {
                var scheme = _httpServer.ListenerOptions.IsTls ? "https" : "http";
                Debug.WriteLine($"Launch On: {scheme}://{ipProperties.Address}:{_httpServer.ActivePort}");
            }
        }
    }
}