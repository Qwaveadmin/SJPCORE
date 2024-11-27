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
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;

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
            string emqx_ip = "";
            string emqx_port = "";
            string emqx_username = "";
            string emqx_password = "";

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
                if (messageobj.type == "schedule")
                {
                    _logger.LogInformation($"Received schedule message: {messageobj.action}");
                    
                    // แปลง action เป็น string อย่างชัดเจน
                    string action = (string)messageobj.action;
                    if (!string.IsNullOrEmpty(action))
                    {
                        switch (action.ToLower())
                        {
                            case "post":
                                _logger.LogInformation("Posting schedule data...");
                                using (var con = _context.CreateConnection())
                                {
                                    try
                                    {
                                        _logger.LogInformation($"Body: {messageobj.body}");
                                        string bodyString = Convert.ToString(messageobj.body);
                                        _logger.LogInformation($"Body as string: {bodyString}");
                                        var body = JsonConvert.DeserializeObject<AddScheduleRequest>(bodyString);
                                        _logger.LogInformation($"Deserialized body: {body}");
                                        
                                        var scheduleController = new ScheduleController(_context);
                                        _logger.LogInformation("Calling AddSchedule...");
                                        var response = scheduleController.AddSchedule(body);
                                        _logger.LogInformation($"AddSchedule response: {response}");
                                        
                                        _logger.LogInformation("Publishing response...");
                                        var json = Newtonsoft.Json.JsonConvert.SerializeObject(new { site_id = site_id, user = messageobj.user, type = "schedule", action = "post", data = response });
                                        await PublishMessageAsync("response", json);
                                        _logger.LogInformation("Published response.");
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError($"Error during deserialization or AddSchedule call: {ex.Message}");
                                    }
                                }
                                break;
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
                            case "get/station": // ดึงข้อมูลสถานี
                                _logger.LogInformation("Getting station data...");
                                using (var con = _context.CreateConnection())
                                {
                                    var scheduleController = new ScheduleController(_context);
                                    var stations = scheduleController.GetStationList(); // เรียกใช้ GetStationList
                                    var json = Newtonsoft.Json.JsonConvert.SerializeObject( new { site_id = site_id, user = messageobj.user, type = "schedule", action = "get/station", data = stations});
                                    await PublishMessageAsync("response", json);
                                }
                                break;
                            case "del" :
                                _logger.LogInformation("Deleting schedule data...");
                                using (var con = _context.CreateConnection())
                                {
                                    try
                                    {
                                    _logger.LogInformation($"Body: {messageobj.body}");
                                    string bodyString = Convert.ToString(messageobj.body);
                                    var response = ScheduleController.DeleteSchedule(bodyString);
                                    _logger.LogInformation($"DeleteSchedule response: {response}");
                                    var json = Newtonsoft.Json.JsonConvert.SerializeObject( new { site_id = site_id, user = messageobj.user, type = "schedule", action = "del", data = response});
                                    await PublishMessageAsync("response", json);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError($"Error during deserialization or DeleteSchedule call: {ex.Message}");
                                    }
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
                else if (messageobj.type == "request")
                {
                    await _hubContext.Clients.All.SendAsync("Signaling", payload);
                }
                else if (messageobj.type == "soundList")
                {
                    _logger.LogInformation($"Received soundList message: {messageobj.action}");
                    
                    // แปลง action เป็น string อย่างชัดเจน
                    string action = (string)messageobj.action;
                    if (!string.IsNullOrEmpty(action))
                    {
                        switch (action.ToLower())
                        {
                            case "get": // ดึงข้อมูลตารางเวลา
                                _logger.LogInformation("Getting soundList data...");
                                try
                                {
                                    using (var con = _context.CreateConnection())
                                    {
                                        var list_sound = await con.GetListAsync<StorageModel>();
                                        var list_yt = await con.GetListAsync<YtModel>();
                                        var list_fm = await con.GetListAsync<RadioModel>();

                                        var mergedList = new List<object>();
                                        mergedList.AddRange(list_sound.Select(s => new { type = "storage", s.key, name = s.name_defualt }));
                                        mergedList.AddRange(list_yt.Select(s => new { type = "youtube", key = s.id, name = s.name }));
                                        mergedList.AddRange(list_fm.Select(s => new { type = "radio", key = s.id, name = s.name }));
                                        var response = new { success = true, message = mergedList };

                                        var json = Newtonsoft.Json.JsonConvert.SerializeObject( new { site_id = site_id, user = messageobj.user, type = "soundList", action = "get", data = response});
                                        await PublishMessageAsync("response", json);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"Error while getting soundList data: {ex.Message}");
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
                        using (var con = new SqliteConnection("Data Source=database.db"))
                        {
                            // Get site_id Where key = 'SITE_ID'
                            site_id = con.Get<ConfigModel>("SITE_ID").value;
                            emqx_ip = con.Get<ConfigModel>("EMQX_IP").value;
                            emqx_port = con.Get<ConfigModel>("EMQX_PORT").value;
                            emqx_username = con.Get<ConfigModel>("EMQX_USER").value;
                            emqx_password = con.Get<ConfigModel>("EMQX_PASS").value;
                        }

                        var options = new MqttClientOptionsBuilder()
                            .WithClientId(site_id)
                            .WithTcpServer(emqx_ip, int.Parse(emqx_port))
                            .WithCredentials(emqx_username, emqx_password)
                            .WithCleanSession()
                            .Build();

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

        // Restart the service
        public void Reconnect()
        {
            // disconnect from the broker
            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                try
                {
                    _mqttClient.DisconnectAsync();
                    _logger.LogInformation("Disconnected from MQTT Broker.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error while disconnecting from MQTT Broker: {ex.Message}");
                }
            }
        }

    }
}
