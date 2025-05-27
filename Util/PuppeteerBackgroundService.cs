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
                        _logger.LogDebug("กำลังเปิด browser ใหม่...");
                        KillOrphanedChromeProcesses();
                        _browser = await Puppeteer.LaunchAsync(new LaunchOptions 
                        { 
                            Headless = true,
                            Args = new[] { "--no-sandbox", "--disable-extensions", "--disable-background-networking" }
                        });
                        _logger.LogDebug($"เปิด browser สำเร็จ PID: {_browser.Process.Id}");
                    }

                    if (_page == null || _page.IsClosed)
                    {
                        _logger.LogDebug("กำลังเปิดหน้าใหม่...");
                        _page = await _browser.NewPageAsync();

                        _page.Console += (sender, e) => 
                        {
                            _logger.LogDebug($"ข้อความจาก browser console: {e.Message.Text}");
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
                    _logger.LogDebug("บริการถูกยกเลิก");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "เกิดข้อผิดพลาด กำลังเริ่ม browser ใหม่...");
                    await CleanupResources(); // ปิด Browser และหน้าเว็บก่อนเริ่มใหม่
                }
            }
        }

        // ปิด Browser และหน้าเว็บ
        private async Task CleanupResources()
        {
            try
            {
                if (_page != null)
                {
                    await _page.CloseAsync();
                    await _page.DisposeAsync();
                    _page = null;
                }
                if (_browser != null)
                {
                    await _browser.CloseAsync();
                    await _browser.DisposeAsync();
                    _browser = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "เกิดข้อผิดพลาดตอนปิดทรัพยากร");
            }
        }

        // ฆ่าโปรเซส Chrome ที่ค้างอยู่
        private void KillOrphanedChromeProcesses()
        {
            try
            {
                foreach (var process in System.Diagnostics.Process.GetProcessesByName("chrome"))
                {
                    if (process.MainModule.FileName.Contains("Google Chrome for Testing"))
                    {
                        process.Kill();
                        process.WaitForExit();
                        _logger.LogDebug($"ปิดโปรเซส Chrome ที่ค้างอยู่ PID: {process.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ไม่สามารถปิดโปรเซส Chrome ที่ค้างได้");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("กำลังหยุดบริการ Puppeteer...");
            await CleanupResources();
            await base.StopAsync(cancellationToken);
            _logger.LogDebug("หยุดบริการ Puppeteer สำเร็จ");
        }

        public async Task ReloadPageAsync()
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

        ~PuppeteerBackgroundService()
        {
            // ปิดทรัพยากรในกรณีที่ service ถูกปิดแบบไม่คาดคิด
            if (_page != null)
            {
                _page.CloseAsync().GetAwaiter().GetResult();
                _page.DisposeAsync().GetAwaiter().GetResult();
            }
            if (_browser != null)
            {
                _browser.CloseAsync().GetAwaiter().GetResult();
                _browser.DisposeAsync().GetAwaiter().GetResult();
            }
        }
    }
}