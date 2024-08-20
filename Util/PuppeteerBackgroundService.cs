using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using PuppeteerSharp.AspNetFramework;

namespace SJPCORE.Util
{
    public class PuppeteerBackgroundService : BackgroundService
    {

        private readonly ILogger<PuppeteerBackgroundService> _logger;
        private readonly string _url;

        public PuppeteerBackgroundService(ILogger<PuppeteerBackgroundService> logger)
        {
            _logger = logger;
            _url = "http://localhost:5000/rtcmcu"; // URL ของแอปพลิเคชัน ASP.NET ของคุณ
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                    {
                        Headless = true,
                    });
                    using var page = await browser.NewPageAsync();

                    // สมัครสมาชิกกับเหตุการณ์ Console ของ Puppeteer
                    page.Console += (sender, e) => 
                    {
                        _logger.LogInformation($"Browser console message: {e.Message.Text}");
                    };

                    // เพิ่ม HTTP Header เพื่อส่งข้อมูลรับรอง
                    await page.SetExtraHttpHeadersAsync(new Dictionary<string, string>
                    {
                        { "X-Custom-Auth", "เย็ดหีแม่มึง" } // ต้องตรงกับค่าที่กำหนดใน Middleware
                    });

                    await page.GoToAsync(_url);

                    // รอสักครู่ก่อนที่จะรีเฟรชหน้าเว็บใหม่
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while running Puppeteer.");
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Puppeteer background service is stopping.");

            await base.StopAsync(cancellationToken);
        }

        
    }
}