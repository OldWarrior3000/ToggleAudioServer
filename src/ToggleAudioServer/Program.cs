using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;

namespace ToggleAudioServer
{
    class Program
    {
        static void Main()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables().Build();

            var host = new WebHostBuilder()
                .UseKestrel(x =>{x.Limits.MaxConcurrentConnections = 10;})
                .UseConfiguration(config)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseUrls(new []{"http://localhost:5888/","https://localhost:5889/"} )
                .ConfigureServices(s => s.AddRouting())
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .Configure(app =>
                {
                    app.UseRouter(r =>
                    {
                        r.MapGet("api/audio", async (request, response, routeData) =>
                        {
                            var result = GetAudioDevices(app, response);
                            await response.WriteAsync(result);
                        });
                    });
                }).Build();

            host.Run();
        }

        private static string GetAudioDevices(IApplicationBuilder app, HttpResponse response)
        {
            var logger = app.ApplicationServices.GetService<ILogger<Program>>();

            var devices = new List<Device>();
            var enumerator = new MMDeviceEnumerator();
            foreach (var audioDevice in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.All))
            {
                try
                {
                    devices.Add(new Device()
                        {
                            Name = audioDevice.FriendlyName,
                            State = Convert.ToString(audioDevice.State)
                        }
                    );
                }
                catch (COMException ce)
                {
                    logger.LogWarning($"Could not read audio device: {ce}");
                }
            }

            var result = JsonSerializer.Serialize(devices);
            response.ContentType = "application/json";
            return result;
        }
    }

    class Device
    {
        public string Name { get; set; }
        public string State { get; set; }
    }
}
