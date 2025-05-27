using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Threading;
using Microsoft.Extensions.Logging;
using MQTTnet.Server;
using Microsoft.AspNetCore.SignalR;

namespace SJPCORE.Util
{
    public class StatusWatchdogBackgroundService : BackgroundService
    {
        private readonly ILogger<StatusWatchdogBackgroundService> _logger;
        private readonly MqttServer _mqttServer;
        private readonly IHubContext<ChatHub> _hubContext;

        public StatusWatchdogBackgroundService(
            ILogger<StatusWatchdogBackgroundService> logger,
            MqttServer mqttServer,
            IHubContext<ChatHub> hubContext)
        {
            _logger = logger;
            _mqttServer = mqttServer;
            _hubContext = hubContext;
            _logger.LogWarning("Status Watchdog background service is running.");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // _mqttServer.ClientConnectedAsync += async e =>
            // {
            //     _logger.LogCritical($"MQTT Client Connected: {e.ClientId}");
            //     await _hubContext.Clients.All.SendAsync("MqttClientStatusChanged", new
            //     {
            //         ClientId = e.ClientId,
            //         Status = "Connected",
            //         Time = DateTime.UtcNow
            //     });
            // };

            // _mqttServer.ClientDisconnectedAsync += async e =>
            // {
            //     _logger.LogInformation($"MQTT Client Disconnected: {e.ClientId}");
            //     await _hubContext.Clients.All.SendAsync("MqttClientStatusChanged", new
            //     {
            //         ClientId = e.ClientId,
            //         Status = "Disconnected",
            //         Time = DateTime.UtcNow
            //     });
            // };

            // รอจนกว่าจะถูกยกเลิก
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}