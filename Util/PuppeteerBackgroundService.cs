using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using PuppeteerSharp.AspNetFramework;
using SJPCORE.Models.Attribute;

namespace SJPCORE.Util
{
    public class PuppeteerBackgroundService : BackgroundService
    {
        private readonly ILogger<PuppeteerBackgroundService> _logger;
        private IBrowser _browser;
        private IPage _page;
        private readonly string _url;

        public PuppeteerBackgroundService(ILogger<PuppeteerBackgroundService> logger)
        {
            _logger = logger;
            _url = "http://localhost:5000/rtcmcu";
            _logger.LogWarning($"Puppeteer background service is running for URL: {_url}");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var browserFetcher = new BrowserFetcher();
            await browserFetcher.DownloadAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // เปิด Browser และหน้าเว็บ (ถ้ายังไม่มี)
                    if (_browser == null || _browser.IsClosed)
                    {
                        _logger.LogInformation("Starting new browser instance...");
                        _browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
                    }

                    if (_page == null || _page.IsClosed)
                    {
                        _logger.LogInformation("Opening new page...");
                        _page = await _browser.NewPageAsync();

                        _page.Console += (sender, e) => 
                        {
                            _logger.LogInformation($"Browser console message: {e.Message.Text}");
                        };

                        // ตั้งค่า Header และเปิด URL
                        await _page.SetExtraHttpHeadersAsync(new Dictionary<string, string>
                        {
                            { InternalRequestFlag.INTERNAL_REQUEST_KEY, InternalRequestFlag.INTERNAL_REQUEST_VALUE }
                        });

                        await _page.GoToAsync(_url);
                        await _page.SetExtraHttpHeadersAsync(new Dictionary<string, string>());
                        await _page.ClickAsync("body");
                    }

                    // รอ 5 นาทีแล้วตรวจสอบสถานะอีกครั้ง
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Service was canceled.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred. Restarting browser...");
                    await CleanupResources(); // ปิด Browser และหน้าเว็บก่อนเริ่มใหม่
                }
            }
        }

        // ปิด Browser และหน้าเว็บ
        private async Task CleanupResources()
        {
            if (_page != null)
            {
                await _page.CloseAsync();
                _page = null;
            }
            if (_browser != null)
            {
                await _browser.CloseAsync();
                _browser = null;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Puppeteer service.");
            await CleanupResources();
            await base.StopAsync(cancellationToken);
        }

        public async void ReloadPageAsync()
        {
            if (_page != null)
            {
                
                await _page.SetExtraHttpHeadersAsync(new Dictionary<string, string>
                {
                    { InternalRequestFlag.INTERNAL_REQUEST_KEY, InternalRequestFlag.INTERNAL_REQUEST_VALUE }
                });

                await _page.ReloadAsync();
            }
        }
    }
}