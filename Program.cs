using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using MQTTnet.AspNetCore;
using System.Threading;
using System;
using SJPCORE.Controllers;
using System.Diagnostics;
using Dapper;
using System.Windows.Forms;

namespace SJPCORE
{
    public class Program
    {
        private static System.Threading.Timer _timer;
        private static NotifyIcon _trayIcon;
        private static Mutex _mutex = new Mutex(true, "{B1AFCF9A-5F6D-4D3A-8F3A-2A9D1E1A1A1A}"); // GUID ที่ไม่ซ้ำกันสำหรับแอปพลิเคชันของคุณ

        [STAThread]
        public static void Main(string[] args)
        {
            // ตรวจสอบว่ามีการสร้างอินสแตนซ์ของแอปพลิเคชันแล้วหรือไม่
            if (!_mutex.WaitOne(TimeSpan.Zero, true))
            {
                MessageBox.Show("Application is already running.");
                return;
            }

            // สร้าง Thread สำหรับ Tray Icon
            Thread trayThread = new Thread(() =>
            {
                _trayIcon = new NotifyIcon
                {
                    Icon = new System.Drawing.Icon("favicon.ico"),
                    Visible = true,
                    Text = "ASP.NET Core Tray App",
                    ContextMenuStrip = new ContextMenuStrip()
                };

                _trayIcon.ContextMenuStrip.Items.Add("Open", null, (s, e) => OpenBrowser("http://localhost:5000"));
                _trayIcon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => Application_Exiting(s, e));

                Application.Run(); // รัน Tray Icon
            });


            trayThread.SetApartmentState(ApartmentState.STA);
            trayThread.Start();

            // สร้าง Thread สำหรับ ASP.NET Core Web Host
            Thread webHostThread = new Thread(() =>
            {
                var host = CreateHostBuilder(args).Build();
                var url = "http://localhost:5000";
                OpenBrowser(url);
                host.Run();
            });

            webHostThread.Start();
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

        private static void Application_Exiting(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            Environment.Exit(0);
            Application.Exit();
        }
    }
}
