using AngleSharp.Dom;
using Dapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Internal;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MySqlX.XDevAPI;
using Newtonsoft.Json;
using SJPCORE.Models;
using SJPCORE.Models.Mqtt;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SJPCORE.Util
    {
        public sealed class MQTTController
        {
            private readonly MqttServer _mqttServer;
            private readonly DapperContext _context;
            private readonly IHubContext<ChatHub> _hubContext;
            private readonly ILogger<MQTTController> _logger;

        public MQTTController(MqttServer mqttServer, IHubContext<ChatHub> hubContext, DapperContext dapperContext, ILogger<MQTTController> logger)
        {
            this._mqttServer = mqttServer;
            this._hubContext = hubContext;
            this._context = dapperContext;
            this._logger = logger;

            mqttServer.InterceptingPublishAsync += async args =>
            {
                await HandleInterceptedMessage(args.ApplicationMessage);
            };

        }


        //_hubContext.Clients.All.SendAsync("ClientConnected", $"[{message.Topic}] " + System.Text.Encoding.UTF8.GetString(message.Payload));
        private async Task HandleInterceptedMessage(MqttApplicationMessage message)
        {
            await _hubContext.Clients.All.SendAsync("ClientConnected", Encoding.UTF8.GetString(message.Payload));
            try
            {
                if (message.Topic == "/sub/node/media")
                {
                    // Deserialize the message payload into a MediaMessage object
                    var serializerSettings = new JsonSerializerSettings { DateFormatString = "dd-MM-yyyy HH:mm:ss.ffffff" };
                    var mediaMessage = JsonConvert.DeserializeObject<MediaMessage>(Encoding.UTF8.GetString(message.Payload), serializerSettings);

                    // Extract the command category and values from the MediaMessage object
                    var commandCategory = mediaMessage.Category;
                    var commandValue = "";
                    switch (commandCategory)
                    {
                        case MediaMessage.CommandCategory.Volume:
                            commandValue = $"({mediaMessage.VolumeValue})";
                            break;
                        case MediaMessage.CommandCategory.Mute:
                            commandValue = $"({mediaMessage.MuteValue})";
                            break;
                        default:
                            break;
                    }

                    try
                    {
                        using (var con = _context.CreateConnection())
                        {
                            if (commandCategory == MediaMessage.CommandCategory.Mute)
                            {
                                var station = con.ExecuteAsync("UPDATE sjp_station SET `mute` = @MuteStation  WHERE key = @StationID", new { StationID = mediaMessage.Id, MuteStation = Convert.ToBoolean(commandValue.Replace("(",string.Empty).Replace(")",string.Empty))});
                            }
                            // else if(commandCategory == MediaMessage.CommandCategory.Volume)
                            // {
                            //     var station = con.ExecuteAsync("UPDATE sjp_station SET `vol` = @VolStation  WHERE key = @StationID", new { StationID = mediaMessage.Id, VolStation = Convert.ToInt16(commandValue.Replace("(", string.Empty).Replace(")", string.Empty)) });
                            // }
                        }
                    }
                    catch
                    {
                        throw;
                    }

                    
                    // Send the MediaMessage object to the connected SignalR clients
                    await _hubContext.Clients.All.SendAsync("ClientConnected", JsonConvert.SerializeObject(new
                    {
                        Id = mediaMessage.Id,
                        Code = mediaMessage.Code,
                        Time = mediaMessage.Time,
                        Type = mediaMessage.Type,
                        Cmd = $"{commandCategory.ToString().ToLower()} {commandValue}",
                        Key = mediaMessage.Key
                    }));
                }

                else if (message.Topic == "/sub/node/response")
                {
                    var serializerSettings = new JsonSerializerSettings { DateFormatString = "dd-MM-yyyy HH:mm:ss.ffffff" };
                    var stateMessage = JsonConvert.DeserializeObject<StateMessage>(Encoding.UTF8.GetString(message.Payload).ToLower(), serializerSettings);

                    if (stateMessage != null)
                    {
                        // Handle StateMessage
                        using (var con = _context.CreateConnection())
                        {
                            foreach (var playerStatus in stateMessage.Player)
                            {
                                if (playerStatus.name == "media")
                                {
                                    await con.ExecuteAsync("update `sjp_station` set `status_media` = @status_media where `key` = @station_key", new { status_media = playerStatus.status, station_key = stateMessage.Id.ToUpper() });
                                }
                                else if (playerStatus.name == "stream")
                                {
                                     await con.ExecuteAsync("update `sjp_station` set `status_stream` = @status_stream where `key` = @station_key", new { status_stream = playerStatus.status, station_key = stateMessage.Id.ToUpper() });
                                }
                                else if (playerStatus.name == "schedule")
                                {
                                     await con.ExecuteAsync("update `sjp_station` set `status_schedule` = @status_schedule where `key` = @station_key", new { status_schedule = playerStatus.status, station_key = stateMessage.Id.ToUpper() });
                                }
                            }
                        } 
                        //await _hubContext.Clients.All.SendAsync("ClientConnected", $"Received response message: {stateMessage.Id}, {stateMessage.Code}, {stateMessage.Cmd}");
                    }
                }
                else if (message.Topic == "/sub/node/webrtc")
                {

                }
                else if (message.Topic == "/sub/node/info")
                {

                }
                else if (message.Topic == "/sub/node/file")/*  */
                {

                }
                else if (message.Topic == "/sub/node/schedule")
                {

                }
                else if (message.Topic == "/sub/node/")
                {

                }
                
                else if (message.Topic == "/sub/node/stream")
                {
                    // Deserialize the message payload into a StreamMessage object
                    var streamMessage = JsonConvert.DeserializeObject<StreamMessage>(Encoding.UTF8.GetString(message.Payload));

                    // Handle the "play" and "stop" commands
                    if (streamMessage.Cmd.ToLower() == "play")
                    {
                        // Handle the "play" command
                    }
                    else if (streamMessage.Cmd.ToLower() == "stop")
                    {
                        // Handle the "stop" command
                    }

                    // Send the deserialized message to the connected SignalR clients
                    await _hubContext.Clients.All.SendAsync("ClientConnected", $"Received ควย stream message: {streamMessage.Id}, {streamMessage.Code}, {streamMessage.Cmd}, {streamMessage.Time}");
                }
                
            }
            catch(Exception ex)
            {
               await _hubContext.Clients.All.SendAsync("ClientConnected", $"ควย ERROR {ex.Message}");
            }
          
        }
        public async Task HandleClientAcknowledgedPublishPacketAsync(ClientAcknowledgedPublishPacketEventArgs eventArgs)
        {
            await _hubContext.Clients.All.SendAsync("ClientConnected", "HandleClientAcknowledgedPublishPacketAsync " + eventArgs.PublishPacket);
        }
        public async Task HandleMessageNotConsumedAsync(ApplicationMessageNotConsumedEventArgs eventArgs)
        {
            await _hubContext.Clients.All.SendAsync("ClientConnected", "HandleMessageNotConsumedAsync " + eventArgs.ApplicationMessage);
        }

        public async Task OnClientConnected(ClientConnectedEventArgs eventArgs)
        {
            _logger.LogCritical($"Client connected: {eventArgs.ClientId} eiei Zapp");

            var result = await _mqttServer.GetClientsAsync();

            var list_result = new List<OnlineModel>();

            using (var con = _context.CreateConnection())
            {
                var list_station = await con.GetListAsync<StationModel>();

                foreach (var item in result)
                {
                    var station = list_station.Where(w => w.key == item.Id).FirstOrDefault();

                    if(station != null)
                    {
                        var list_status = new Dictionary<string, bool>() {
                              {"media", station.status_media},
                              {"stream", station.status_stream},
                              {"schedule", station.status_schedule}
                        };
                        var all_false = list_status.Values.All(x => !x);
                        var status = all_false ? "ออนไลน์" : string.Join(", ", list_status.Where(x => x.Value).Select(x => x.Key == "media" ? "กำลังเล่นเสียง" : x.Key == "stream" ? "กำลังกระจายเสียงตามสาย" : "กำลังเล่นตารางที่กำหนด"));
                        list_result.Add(new OnlineModel() { Id = item.Id, Status = status });
                    }
                   
                };

            }
            await _hubContext.Clients.All.SendAsync("ReceiveStationStatus", list_result);
            
        }

        public async Task OnClientDisconnected(ClientDisconnectedEventArgs eventArgs)
        {
            _logger.LogCritical($"Client disconnected: {eventArgs.ClientId} eiei Zapp");
            var result = await _mqttServer.GetClientsAsync();

            var list_result = new List<OnlineModel>();

            using (var con = _context.CreateConnection())
            {
                var list_station = await con.GetListAsync<StationModel>();

                foreach (var item in result)
                {
                    var station = list_station.Where(w => w.key == item.Id).FirstOrDefault();

                    if (station != null)
                    {
                        var list_status = new Dictionary<string, bool>() {
                              {"media", station.status_media},
                              {"stream", station.status_stream},
                              {"schedule", station.status_schedule}
                        };
                        var all_false = list_status.Values.All(x => !x);
                        var status = all_false ? "ออนไลน์" : string.Join(", ", list_status.Where(x => x.Value).Select(x => x.Key == "media" ? "กำลังเล่นเสียง" : x.Key == "stream" ? "กำลังกระจายเสียงตามสาย" : "กำลังเล่นตารางที่กำหนด"));
                        list_result.Add(new OnlineModel() { Id = item.Id, Status = status });
                    }

                }
                ;

            }
            await _hubContext.Clients.All.SendAsync("ReceiveStationStatus", list_result);
            
            
        }


        public async Task OnClientSubscribedTopic(ClientSubscribedTopicEventArgs eventArgs)
        {
            await _hubContext.Clients.All.SendAsync("ClientConnected", "ClientSubscribedTopic " + eventArgs.ClientId, eventArgs.TopicFilter.Topic);
        }

        public async Task OnClientUnsubscribedTopic(ClientUnsubscribedTopicEventArgs eventArgs)
        {
            await _hubContext.Clients.All.SendAsync("ClientConnected", "ClientUnsubscribedTopic " + eventArgs.ClientId, eventArgs.TopicFilter);
        }

        public Task ValidateConnection(ValidatingConnectionEventArgs args)
        {
                try
                {
                    var currentUser_Username = @GlobalParameter.Config.Where(w => w.key == "CONFIG_MQTT_USER").FirstOrDefault().value;
                    var currentUser_Password = @GlobalParameter.Config.Where(w => w.key == "CONFIG_MQTT_PASS").FirstOrDefault().value;

                    if (currentUser_Username == null || currentUser_Password == null)
                    {
                        args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;  
                        return Task.CompletedTask;
                    }

                    if (args.UserName != currentUser_Username)
                    {
                        args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                        return Task.CompletedTask;
                    }

                    if (args.Password != currentUser_Password)
                    {
                        args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                        return Task.CompletedTask;
                    }

                    args.ReasonCode = MqttConnectReasonCode.Success;
                    return Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    return Task.FromException(ex);
                }
            }
        }
    }



