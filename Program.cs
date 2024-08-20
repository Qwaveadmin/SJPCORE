using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using MQTTnet.AspNetCore;
using System.Threading;
using System;
using SJPCORE.Controllers;
using System.Diagnostics;

namespace SJPCORE
{
    public class Program
    {
        private static Timer _timer;
        public static void Main(string[] args)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                StationController.DeletePassed();
                _timer = new Timer(RunSchedule, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(500));
            });

            var host = CreateHostBuilder(args).Build();
            var url = "http://localhost:5000";
            OpenBrowser(url);
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();

                    webBuilder.UseKestrel(o =>
                    {
                        o.ListenAnyIP(5000); // Default HTTP pipeline
                        o.ListenAnyIP(11257, l => l.UseMqtt());
                    });
                });

        private static void RunSchedule(object state)
        {
            // Run the code on a separate thread to avoid blocking the main thread
            ThreadPool.QueueUserWorkItem(_ =>
            {
                StationController.CheckScheduleAsync(state);
            });
        }

        private static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open browser: {ex.Message}");
            }
        }
    }
}
