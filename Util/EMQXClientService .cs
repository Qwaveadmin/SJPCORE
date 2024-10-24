using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using Microsoft.Data.Sqlite;
using Dapper;
using SJPCORE.Models;
using SJPCORE.Controllers;

namespace SJPCORE.Util
{
    public class EMQXClientService : BackgroundService
    {
        private readonly ILogger<EMQXClientService> _logger;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly DapperContext _context;
        private IMqttClient _mqttClient;

        public EMQXClientService(ILogger<EMQXClientService> logger, IHubContext<ChatHub> hubContext, DapperContext context)
        {
            _logger = logger;
            _hubContext = hubContext;
            _context = context;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string site_id = "";
            using (var con = new SqliteConnection("Data Source=database.db"))
            {
                // Get site_id Where key = 'SITE_ID'
                site_id = con.Get<ConfigModel>("SITE_ID").value;
            }

            var options = new MqttClientOptionsBuilder()
                .WithClientId(site_id)
                .WithTcpServer("qwaveoffice.trueddns.com", 12660) // กำหนดค่า MQTT Broker และพอร์ต
                .WithCredentials("apiwat59", "panyoi59") // กำหนด Username และ Password
                .WithCleanSession()
                .Build();

            _mqttClient = new MqttFactory().CreateMqttClient();

            // Subscribe to events
            _mqttClient.ConnectedAsync += e => {
                _logger.LogInformation("Connected to MQTT Broker.");
                _ = SubscribeToTopicAsync(site_id);        
                return Task.CompletedTask;
            };

            _mqttClient.DisconnectedAsync += e => {
                _logger.LogInformation("Disconnected from MQTT Broker.");
                return Task.CompletedTask;
            };

            _mqttClient.ApplicationMessageReceivedAsync += async e => {
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                var messageobj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(payload);
                
                _logger.LogInformation($"Message received on topic {e.ApplicationMessage.Topic}: {payload}");
                // ตรวจสอบค่า 'type' ใน messageobj
                if (messageobj.type == "Signaling")
                {
                    await _hubContext.Clients.All.SendAsync("Signaling", payload);
                }
                else if (messageobj.type == "schedule")
                {
                    _logger.LogInformation($"Received schedule message: {messageobj.action}");
                    
                    // แปลง action เป็น string อย่างชัดเจน
                    string action = (string)messageobj.action;
                    if (!string.IsNullOrEmpty(action))
                    {
                        switch (action.ToLower())
                        {
                            case "get": // ดึงข้อมูลตารางเวลา
                                _logger.LogInformation("Getting schedule data...");
                                using (var con = _context.CreateConnection())
                                {
                                    var scheduleController = new ScheduleController(_context);
                                    var schedules = scheduleController.GetScheduleList(); // เรียกใช้ GetScheduleList
                                    var json = Newtonsoft.Json.JsonConvert.SerializeObject( new { site_id = site_id, user = messageobj.user, type = "schedule", action = "get", data = schedules});
                                    await PublishMessageAsync("response", json);
                                }
                                break;    
                            default:
                                _logger.LogWarning("Unknown action.");
                                break;            
                        }
                    }
                    else
                    {
                        Console.WriteLine("Action is null or empty.");
                    }
                }
                else
                {
                    _logger.LogWarning("Unknown message type.");
                }
            };


            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_mqttClient.IsConnected)
                {
                    try
                    {
                        await _mqttClient.ConnectAsync(options, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"MQTT connection failed: {ex.Message}");
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            await _mqttClient.DisconnectAsync();
        }

        public async Task PublishMessageAsync(string topic, string payload)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
                .Build();

            if (_mqttClient.IsConnected)
            {
                await _mqttClient.PublishAsync(message);
                _logger.LogInformation($"Published message to topic {topic}: {payload}");
            }
            else
            {
                _logger.LogWarning("Unable to publish message, MQTT client is not connected.");
            }
        }

        public async Task SubscribeToTopicAsync(string topic)
        {
            if (_mqttClient.IsConnected)
            {
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
                _logger.LogInformation($"Subscribed to topic {topic}");
            }
            else
            {
                _logger.LogWarning("Unable to subscribe to topic, MQTT client is not connected.");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                try
                {
                    await _mqttClient.DisconnectAsync();
                    _logger.LogInformation("Disconnected from MQTT Broker.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error while disconnecting from MQTT Broker: {ex.Message}");
                }
            }
            await base.StopAsync(stoppingToken);
        }
    }
}
