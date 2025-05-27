using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SJPCORE.Models;
using MQTTnet.Client;
using MQTTnet;
using MQTTnet.Server;
using Microsoft.AspNetCore.Authorization;
using SJPCORE.Models.Attribute;
using System.Threading;
using Dapper.Contrib.Extensions;
using SJPCORE.Models.Mqtt;

namespace SJPCORE.Controllers
{
    [Authorize]
    public class StationController : Controller
    {
        private readonly ILogger<StationController> _logger;
        private readonly DapperContext _context;
        public static MqttServer _mqttserver;


        public StationController(ILogger<StationController> logger, DapperContext context ,MqttServer mqttserver)
        {
            _logger = logger;
            _context = context;
            _mqttserver = mqttserver;

        }
        [ActionName("Status")]
        public IActionResult Status()
        {
            using (var con = _context.CreateConnection())
            {
                var station = con.GetList<StationModel>();
                return View("status-station", station);
            }
        }

        [ActionName("Player")]
        public IActionResult Player()
        {
            return View("player");
        }
        [ActionName("Info")]
        public IActionResult Info(string id)
        {
            using (var con = _context.CreateConnection())
            {
                var station = con.GetList<StationModel>().Where(w=>w.key== id);
                if (station.Count() == 0) return NotFound(new { messeage = $"ไม่พบไอดีสถานีที่ท่านร้องขอ ({id})" });
               
                return View("status-station-info", station.FirstOrDefault());
            }
        }
        [ActionName("Settings")]
        public IActionResult Settings(string id)
        {
            using (var con = _context.CreateConnection())
            {
                var station = con.GetList<StationModel>().Where(w => w.key == id);
                if (station.Count() == 0) return NotFound(new { messeage = $"ไม่พบไอดีสถานีที่ท่านร้องขอ ({id})" });

                return View("status-station-settings", station.FirstOrDefault());
            }
        }

        [ActionName("Broadcast")]
        public IActionResult Broadcast()
        {

             using (var con = _context.CreateConnection())
            {
                var station = con.GetList<StationModel>();
                
                return View("broadcast", station);
            }
        }
        [ActionName("online")]
        public async Task<IActionResult> CheckOnline()
        {
            // var objfile_online = new
            // {
            //     id = "all",
            //     cmd = "state"
            // };

            // var message = new MqttApplicationMessageBuilder()
            //     .WithTopic("/sub/node")
            //     .WithPayload(JsonConvert.SerializeObject(objfile_online))
            //     .Build();

            // await _mqttserver.InjectApplicationMessage(
            //     new InjectedMqttApplicationMessage(message)
            //     {
            //         SenderClientId = "commander"
            //     });

            var result = await _mqttserver.GetClientsAsync();

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

            return Ok(new { success = true, message = list_result });
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpPost("station/create")]
        public async Task<ActionResult> create_assign([FromBody] StationModel body)
        {
            if (body == null)
            {
                return Ok(new { success = false, message = "Invalid request body." });
            }

            string key_station = body.key;
            string name = body.name;

            if (string.IsNullOrEmpty(name))
            {
                return Ok(new { success = false, message = "Invalid request body." + name });
            }

            using (var con = _context.CreateConnection())
            {
                con.Open();
                using (var transaction = con.BeginTransaction())
                {
                    try
                    {
                        var exits = (await con.GetListAsync<StationModel>()).Where(w=>w.name == name);
                        if (exits.Count() > 0)
                        {
                            return Ok(new { success = false, message = "มีสถานีกระจายเสียงนี้อยู่ในระบบแล้ว" });
                        }

                        if (!string.IsNullOrEmpty(key_station))
                        {
                            var exits_key = (await con.GetListAsync<StationModel>()).Where(w => w.key == key_station);
                            if (exits_key.Count() > 0)
                            {
                                return Ok(new { success = false, message = "มี ID สถานีกระจายเสียงนี้อยู่ในระบบแล้ว" });
                            }
                        }
                        string sql = "INSERT INTO sjp_station (active ,key, name, update_at, create_at) VALUES (@active,@key, @name, @update_at, @create_at)";
                        
                        var stationModel = new StationModel
                        {
                            key = string.IsNullOrEmpty(key_station) ? SJPCORE.Util.StorageHelper.GenerateShortGuid("STA") : key_station.ToUpper(),
                            name = name
                        };
                        int rowsAffected = await con.ExecuteAsync(sql, new { active = stationModel.active, key = stationModel.key, name = stationModel.name, update_at = stationModel.update_at, create_at = stationModel.create_at }, transaction);
                        if (rowsAffected <= 0)
                        {

                            return Ok(new { success = false, message = "Failed to create station." });

                        }


                        transaction.Commit();
                        return Ok(new { success = true, message = $"สร้างสถานี [{stationModel.key}] {stationModel.name} สำเร็จ!!" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Ok(new { success = false, message = $"An error occurred: {ex.Message}" });
                    }
                }
            }
        }


        [HttpPost("station/update/{key}")]
        public async Task<ActionResult> update_station(string key, [FromBody] StationModel body)
        {
            if (string.IsNullOrEmpty(key) || body == null)
            {
                return Ok(new { success = false, message = "Invalid request." });
            }

            using (var con = _context.CreateConnection())
            {
                con.Open();
                using (var transaction = con.BeginTransaction())
                {
                    try
                    {
                        var exits = (await con.GetListAsync<StationModel>()).Where(w => w.key == key);
                        if (exits.Count() == 0)
                        {
                            return Ok(new { success = false, message = "ไม่พบสถานีกระจายเสียงที่ต้องการแก้ไข" });
                        }

                        string sql = "UPDATE sjp_station SET name=@name, description=@description, update_at=@update_at WHERE key=@key";
                        int rowsAffected = await con.ExecuteAsync(sql, new { name = body.name, description = body.description, update_at = DateTime.Now, key = key }, transaction);
                        if (rowsAffected <= 0)
                        {
                            return Ok(new { success = false, message = "Failed to update station." });
                        }

                        transaction.Commit();
                        return Ok(new { success = true, message = $"แก้ไขข้อมูลสถานี [{key}] สำเร็จ!!" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Ok(new { success = false, message = $"An error occurred: {ex.Message}" });
                    }
                }
            }
        }

        [Authorize(Policy = "RequireAdmin")]
        [HttpDelete("station/del/{key}")]
        public async Task<ActionResult> delete_station(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return Ok(new { success = false, message = "Invalid request." });
            }

            using (var con = _context.CreateConnection())
            {
                con.Open();
                using (var transaction = con.BeginTransaction())
                {
                    try
                    {
                        var exits = await con.GetListAsync<StationModel>(new { key = key });
                        if (exits.Count() == 0)
                        {
                            return Ok(new { success = false, message = "ไม่พบสถานีกระจายเสียงที่ต้องการลบ" });
                        }

                        string sql = "DELETE FROM sjp_station WHERE key=@key";
                        int rowsAffected = await con.ExecuteAsync(sql, new { key = key }, transaction);
                        if (rowsAffected <= 0)
                        {
                            return Ok(new { success = false, message = "Failed to delete station." });
                        }

                        transaction.Commit();
                        return Ok(new { success = true, message = $"ลบสถานี [{key}] สำเร็จ!!" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Ok(new { success = false, message = $"An error occurred: {ex.Message}" });
                    }
                }
            }
        }
        [ActionName("Group")]
        public IActionResult Group()
        {

            var model = new List<_GroupModel>();

            using (var con = _context.CreateConnection())
            {
                var grp = con.GetList<GroupModel>();

                foreach (var item in grp)
                {

                    var nodes = con.GetList<GroupAssignModel>().Where(w => w.grp_id == item.key).Select(s => s.station_id);
                    model.Add(new _GroupModel()
                    {
                        key = item.key,
                        name = item.name,
                        nodes = nodes.Select(s=>s).ToArray(),
                        vol = item.vol,
                        group_info = item
                    });
                }

            }
            return View("group-station",model);
        }

        [ActionName("GroupInfo")]
        public IActionResult GroupInfo(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            //var model = new _GroupModel();

            //using (var con = _context.CreateConnection())
            //{
            //    var grplist = con.GetList<GroupModel>().Where(w=>w.key == id);

            //    if(grplist.Count() == 0) return NotFound(new { messeage = $"ไม่พบไอดีกลุ่มที่ท่านร้องขอ ({id})"});

            //    var grp = grplist.FirstOrDefault();
            //    model.name = grp.name;
            //    model.key = grp.key;
            //    var node = con.GetList<GroupAssignModel>().Where(w => w.grp_id == grp.key).Select(s => s.station_id);
            //    model.nodes = node.Select(s=>s).ToArray();

            //}
            var grpmodel = new _ManageGroupModel();
            var model = new List<_GroupModel>();

            using (var con = _context.CreateConnection())
            {
                var _grp_list = con.GetList<GroupModel>();
                var _station_list = con.GetList<StationModel>();
                var grp_list = _grp_list.Where(w => w.key == id);

                if (grp_list.Count() == 0) return NotFound(new { messeage = $"ไม่พบไอดีกลุ่มที่ท่านร้องขอ ({id})" });

                var grp = grp_list.FirstOrDefault();

                grpmodel.name = grp.name;

                var nodes = con.GetList<GroupAssignModel>().Where(w => w.grp_id == grp.key);

                foreach (var item in nodes)
                {
                    //model.Add(new _GroupModel()
                    //{
                    //    key = grp_list.Where(w => w.key == item.station_id).FirstOrDefault().key,
                    //    name = grp_list.Where(w => w.key == item.station_id).FirstOrDefault().name,
                    //});

                    model.Add(new _GroupModel()
                    {
                        key = _station_list.Where(w => w.key == item.station_id).FirstOrDefault().key,
                        name = _station_list.Where(w => w.key == item.station_id).FirstOrDefault().name,
                    });
                }

                grpmodel.key = id;
                grpmodel.nodes = model;
                grpmodel.group_info = grp;
                grpmodel.station_info = _station_list.ToList(); 

            }

            

            return View("group-station-info", grpmodel);
        }

        [HttpGet("Station/Schedule")]
        public IActionResult Schedule()
        {
            return View("schedule-station");
        }

        [HttpPost("Station/Mqtt/Volume")]
        public async Task<IActionResult> SetVolume([FromBody] MqttStationModel_Volume model)
        {
            try
            {


                    var objfile = new MqttStationModel_Volume
                    {
                        user = model.user,
                        cmd = model.cmd,
                        client= model.client,
                        vol = model.vol
                    };

                    var objfile_stop = new MqttStationModel_Volume
                    {
                        user = model.user,
                        cmd = "stop",
                        client = model.client
                    };

                    var message_stop = new MqttApplicationMessageBuilder()
                        .WithTopic("/sub/node")
                        .WithPayload(JsonConvert.SerializeObject(objfile_stop))
                        .Build();

                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic("/sub/node")
                        .WithPayload(JsonConvert.SerializeObject(objfile))
                        .Build();



                    await _mqttserver.InjectApplicationMessage(
                        new InjectedMqttApplicationMessage(message_stop)
                        {
                            SenderClientId = "commander"
                        });

                    await _mqttserver.InjectApplicationMessage(
                        new InjectedMqttApplicationMessage(message)
                        {
                            SenderClientId = "commander"
                        });

                

                return Json(new { success = true, message = "changed volume success!!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        [HttpGet("Station/soundList")]
        public async Task<IActionResult> SoundList()
        {
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

                    return Json(new { success = true, message = mergedList });
                }

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        [HttpPost("station/WebRTC")]
        public async Task<IActionResult> station_webrtc([FromBody] MqttStationModelWebRTC webrtc)
        {
            try
            {
                using (var con = _context.CreateConnection())
                {
                    List<string> client_id = new List<string>();

                    if (webrtc.type_user == "group")
                    {
                        var grp = await con.GetListAsync<GroupAssignModel>();

                        var list = grp.Where(w => w.grp_id == webrtc.target).Select(s => s.station_id).ToList();

                        client_id = list;
                    }
                    else if (webrtc.type_user == "multi")
                    {
                        client_id = webrtc.target_multi;
                    }
                    else
                    {
                        if ((await _mqttserver.GetClientsAsync()).Where(w => w.Id == webrtc.target).Count() == 0) return Json(new { success = false, message = $"ขณะนี้สถานี {webrtc.target} ออฟไลน์" });

                        client_id.Add(webrtc.target);
                    }

                    
                    var id = webrtc.target.Equals("all") ? "all" : (object)client_id;
                    var objfile = webrtc.type == "offer" ? new {  user = webrtc.user, cmd = webrtc.cmd, type = webrtc.type, message = webrtc.message, id = (object)id } : new {  user = webrtc.user, cmd = webrtc.cmd, type = webrtc.type, message = webrtc.message, id = (object)id};

                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic("/sub/node")
                        .WithPayload(JsonConvert.SerializeObject(objfile))
                        .Build();

                    await _mqttserver.InjectApplicationMessage(
                        new InjectedMqttApplicationMessage(message)
                        {
                            SenderClientId = "commander"
                        });
                }

                return Json(new { success = true, message = "ส่งคำขอเปิดระบบกระจายเสียง สำเร็จ!!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                return Json(new { success = false, message = ex.Message });
            }

        }

        [HttpPost("station/broadcast")]
        public async Task<IActionResult> station_broadcast([FromBody] MqttStationModelBroadcast broadcast)
        {
            try
            {
                    using (var con = _context.CreateConnection())
                    {
                        List<string> client_id = new List<string>();

                        if (broadcast.type_user == "group")
                        {
                            var grp = await con.GetListAsync<GroupAssignModel>();

                            var list = grp.Where(w => w.grp_id == broadcast.key).Select(s => s.station_id).ToList();

                            client_id = list;
                        }
                        else if(broadcast.type_user == "multi")
                        {
                            client_id = broadcast.key_multi;
                        }
                        else
                        {
                            if ((await _mqttserver.GetClientsAsync()).Where(w => w.Id == broadcast.key).Count() == 0) return Json(new { success = false, message = $"ขณะนี้สถานี {broadcast.key} ออฟไลน์" });

                        client_id.Add(broadcast.key);
                        }

                        var objfile_start = new
                        {
                            id = client_id,
                            type = "stream",
                            cmd = "play"
                        };

                        var objfile_stop = new
                        {
                            id = client_id,
                            type = "stream",
                            cmd = "stop"
                        };

                    var objfile_stopmedia = new
                    {
                        id = client_id,
                        type = "media",
                        cmd = "stop"

                    };

                    var message_stopmedia = new MqttApplicationMessageBuilder()
                        .WithTopic("/sub/node")
                        .WithPayload(JsonConvert.SerializeObject(objfile_stopmedia))
                        .Build();

                    var message_start = new MqttApplicationMessageBuilder()
                            .WithTopic("/sub/node")
                            .WithPayload(JsonConvert.SerializeObject(objfile_start))
                            .Build();

                        var message_stop = new MqttApplicationMessageBuilder()
                            .WithTopic("/sub/node")
                            .WithPayload(JsonConvert.SerializeObject(objfile_stop))
                            .Build();

                        if (broadcast.broadcast == true)
                        {
                        await _mqttserver.InjectApplicationMessage(
                       new InjectedMqttApplicationMessage(message_stopmedia)
                       {
                           SenderClientId = "commander"
                       });

                        await _mqttserver.InjectApplicationMessage(
                          new InjectedMqttApplicationMessage(message_stop)
                          {
                              SenderClientId = "commander"
                          });
                        await _mqttserver.InjectApplicationMessage(
                          new InjectedMqttApplicationMessage(message_start)
                          {
                              SenderClientId = "commander"
                          });

                            return Json(new { success = true, message = $"เปิดระบบสตรีม {string.Join(",", client_id)} สำเร็จ" });
                        }
                        else
                        {
                        await _mqttserver.InjectApplicationMessage(
                       new InjectedMqttApplicationMessage(message_stopmedia)
                       {
                           SenderClientId = "commander"
                       });

                        await _mqttserver.InjectApplicationMessage(
                             new InjectedMqttApplicationMessage(message_stop)
                             {
                                 SenderClientId = "commander"
                             });
                        return Json(new { success = true, message = $"ปิดระบบสตรีม {string.Join(",", client_id)} สำเร็จ" });
                        }

                    }

                }


            
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("station/config")]
        public async Task<IActionResult> station_config([FromBody] MqttStationModelConfig config)
        {
            try
            {

                using (var client = new MqttFactory().CreateMqttClient())
                {
                    var options = new MqttClientOptionsBuilder()
                  .WithClientId(GlobalParameter.client)
                  .WithTcpServer($"{GlobalParameter.host}", Convert.ToInt16(GlobalParameter.port))
                  .WithCredentials(GlobalParameter.username, GlobalParameter.password)
                  .Build();

                    await client.ConnectAsync(options);


                    var objfile_config = new
                    {
                        id = config.key,
                        config = config.base64json,
                    };

                    var message_start = new MqttApplicationMessageBuilder()
                        .WithTopic("/sub/node")
                        .WithPayload(JsonConvert.SerializeObject(objfile_config))
                        .Build();

                    await client.PublishAsync(message_start);
                    await client.DisconnectAsync();

                    return Json(new { success = true, message = $"ส่งค่า config สำเร็จ" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("station/vol")]
        public async Task<IActionResult> station_vol([FromBody] MqttStationModelVolume volume)
        {
            try
            {
                    using (var con = _context.CreateConnection())
                    {
                        List<string> client_id = new List<string>();

                        if (volume.type_user == "group")
                        {
                            var grp = await con.GetListAsync<GroupAssignModel>();

                            var list = grp.Where(w => w.grp_id == volume.key).Select(s => s.station_id).ToList();

                            client_id = list;

                            var update = $"UPDATE `sjp_grp` SET vol = {volume.vol} WHERE `sjp_grp`.`key` = '{volume.key}';";
                            var ex = con.Execute(update);
                        }
                        else
                        {
                            if ((await _mqttserver.GetClientsAsync()).Where(w => w.Id == volume.key).Count() == 0) return Json(new { success = false, message = $"ขณะนี้สถานี {volume.key} ออฟไลน์" });
                            client_id.Add(volume.key);
                        }

                        var objfile_mute = new
                        {
                            id = client_id,
                            type = "media",
                            cmd = "ch-vol",
                            vol = volume.vol
                        };

                        foreach (var item in client_id)
                        {
                            var update = $"UPDATE `sjp_station` SET vol = {volume.vol} WHERE `sjp_station`.`key` = '{item}';";
                            var ex = con.Execute(update);
                        }
                        
                        var message_mute = new MqttApplicationMessageBuilder()
                            .WithTopic("/sub/node")
                            .WithRetainFlag()
                            .WithPayload(JsonConvert.SerializeObject(objfile_mute))
                            .Build();

                        await _mqttserver.InjectApplicationMessage(
                            new InjectedMqttApplicationMessage(message_mute)
                            {
                                SenderClientId = "commander"
                            });
                        return Json(new { success = true, message = $"ปรับเสียงเป็น {volume.vol} เปอร์เซ็น {string.Join(",", client_id)} สำเร็จ" });
                    }

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("station/stop")]
        public async Task<IActionResult> station_stop([FromBody] MqttStationModelStop stop)
        {
            try
            {
                using (var con = _context.CreateConnection())
                {
                    List<string> client_id = new List<string>();

                    if (stop.type_user == "group")
                    {
                        var grp = await con.GetListAsync<GroupAssignModel>();

                        var list = grp.Where(w => w.grp_id == stop.key).Select(s => s.station_id).ToList();

                        client_id = list;
                        client_id.Add(stop.key);
                    }
                    else
                    {
                        if ((await _mqttserver.GetClientsAsync()).Where(w => w.Id == stop.key).Count() == 0) return Json(new { success = false, message = $"ขณะนี้สถานี {stop.key} ออฟไลน์" });
                        client_id.Add(stop.key);
                    }

                    var objfile_mute = new
                    {
                        id = client_id,
                        type = stop.type,
                        cmd = "stop"

                    };

                    _logger.LogDebug(JsonConvert.SerializeObject(objfile_mute));

                    var message_mute = new MqttApplicationMessageBuilder()
                        .WithTopic("/sub/node")
                        .WithPayload(JsonConvert.SerializeObject(objfile_mute))
                        .Build();

                    await _mqttserver.InjectApplicationMessage(
                        new InjectedMqttApplicationMessage(message_mute)
                        {
                            SenderClientId = "commander"
                        });


                    return Json(new { success = true, message = $"หยุดการเล่นประเภท {stop.type} {string.Join(",", client_id)} สำเร็จ" });


                }

            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("station/mute")]
        public async Task<IActionResult> station_mute([FromBody] MqttStationModelMute mute)
        {
            try
            {
                using (var con = _context.CreateConnection())
                {
                    List<string> client_id = new List<string>();
                    if (mute.type_user == "group")
                    {
                        var grp = await con.GetListAsync<GroupAssignModel>();
                        var list = grp.Where(w => w.grp_id == mute.key).Select(s => s.station_id).ToList();
                        client_id = list;
                        var update = $"UPDATE `sjp_grp` SET mute = {mute.mute} WHERE `sjp_grp`.`key` = '{mute.key}';";
                        var ex = con.Execute(update);
                    }
                    else
                    {
                    if ((await _mqttserver.GetClientsAsync()).Where(w => w.Id == mute.key).Count() == 0) return Json(new { success = false, message = $"ขณะนี้สถานี {mute.key} ออฟไลน์" });
                    client_id.Add(mute.key);
                    }

                    int vol = 0;
                    if (mute.mute == false)
                    {
                        vol = client_id.Count == 1 ? con.GetList<StationModel>().Where(w => w.key == client_id.FirstOrDefault()).FirstOrDefault().vol : con.GetList<GroupModel>().Where(w => w.key == mute.key).FirstOrDefault().vol;
                    }

                    var objfile_mute = new
                    {
                        id = client_id,
                        type = "media",
                        cmd = "ch-vol",
                        vol = vol
                    };

                    foreach (var item in client_id)
                    {
                        var update = $"UPDATE `sjp_station` SET mute = {mute.mute} WHERE `sjp_station`.`key` = '{item}';";
                        var ex = con.Execute(update);
                    }
                    
                    var message_mute = new MqttApplicationMessageBuilder()
                        .WithTopic("/sub/node")
                        .WithRetainFlag()
                        .WithPayload(JsonConvert.SerializeObject(objfile_mute))
                        .Build();

                    await _mqttserver.InjectApplicationMessage(
                        new InjectedMqttApplicationMessage(message_mute)
                        {
                            SenderClientId = "commander"
                        });

                    if (mute.mute == true)
                        {
                            return Json(new { success = true, message = $"ปิดเสียง {string.Join(",", client_id)} สำเร็จ" });
                        }
                        else
                        {
                            return Json(new { success = true, message = $"เปิดเสียง {string.Join(",", client_id)} สำเร็จ" });
                        }
                        
                    }

                

               
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("Station/Mqtt")]
        public async Task<IActionResult> Schedule([FromBody] MqttStationModelPLay model)
        {
            try
            {
    
                    
                    using (var con = _context.CreateConnection())
                    {
                        var liststation = await con.GetListAsync<StationModel>();
                        var listgrp = await con.GetListAsync<GroupModel>();

                        List<string> client_id = new List<string>();

                        if (model.type_user == "group")
                        {
                            var grp = await con.GetListAsync<GroupAssignModel>();

                            var list = grp.Where(w => w.grp_id == model.key).Select(s => s.station_id).ToList();

                            client_id = list;
                        }
                        else
                        {
                        if ((await _mqttserver.GetClientsAsync()).Where(w => w.Id == model.key).Count() == 0) return Json(new { success = false, message = $"ขณะนี้สถานี {model.key} ออฟไลน์" });
                        client_id.Add(model.key);
                        }

                        if (model.type_play == "storage")
                        {

                            var listfile = (await con.GetListAsync<StorageModel>()).Where(w => w.key == model.key_sound);
                            
                            if (listfile.Count() == 0) throw new Exception("ไม่พบไฟล์นี้");

                            var objfile = new
                            {
                                id = client_id,
                                key = model.key_sound,
                                cmd = "play-url",
                                vol = client_id.Count == 1 ? con.GetList<StationModel>().Where(w => w.key == client_id.FirstOrDefault()).FirstOrDefault().mute == true ? 0 :  liststation.Where(w=>w.key == client_id.FirstOrDefault()).FirstOrDefault().vol : con.GetList<GroupModel>().Where(w => w.key == model.key).FirstOrDefault().mute == true ? 0 :  listgrp.Where(w=>w.key==model.key).FirstOrDefault().vol  ,
                                audio = Url.Action("SoundFile", "Storage", new { key = model.key_sound }, protocol: Request.Scheme, host: SJPCORE.Util.NetworkHelper.GetLocalIPv4()+ ":5000"),
                                type = "media",
                            };

                            var objfile_stop = new 
                            {
                                id = client_id,
                                type = "media",
                                cmd = "stop"
                               
                            };

                            var message_stop = new MqttApplicationMessageBuilder()
                                .WithTopic("/sub/node")
                                .WithPayload(JsonConvert.SerializeObject(objfile_stop))
                                .Build();

                            var message = new MqttApplicationMessageBuilder()
                                .WithTopic("/sub/node")
                                .WithPayload(JsonConvert.SerializeObject(objfile))
                                .Build();


                            await _mqttserver.InjectApplicationMessage(
                                new InjectedMqttApplicationMessage(message_stop)
                                {
                                    SenderClientId = "commander"
                                });
                            await _mqttserver.InjectApplicationMessage(
                                new InjectedMqttApplicationMessage(message)
                                {
                                    SenderClientId = "commander"
                                });


                        }
                        if (model.type_play == "youtube")
                        {

                            var listyoutube = (await con.GetListAsync<YtModel>()).Where(w => w.id.ToString() == model.key_sound);

                            if (listyoutube.Count() == 0) throw new Exception("ไม่พบ URL นี้");

                            var objfile = new
                            {
                                id = client_id,
                                key = model.key_sound,
                                cmd = "play-url",
                                vol = client_id.Count == 1 ? con.GetList<StationModel>().Where(w => w.key == client_id.FirstOrDefault()).FirstOrDefault().mute == true ? 0 :  liststation.Where(w=>w.key == client_id.FirstOrDefault()).FirstOrDefault().vol : con.GetList<GroupModel>().Where(w => w.key == model.key).FirstOrDefault().mute == true ? 0 :  listgrp.Where(w=>w.key==model.key).FirstOrDefault().vol  ,
                                audio = await SJPCORE.Util.YoutubeHelper.GetAudioStreamLinkAsync(listyoutube.FirstOrDefault().url),
                                type = "media"
                            };

                            var objfile_stop = new
                            {
                                id = client_id,
                                type = "media",
                                cmd = "stop"

                            };

                            var message_stop = new MqttApplicationMessageBuilder()
                                .WithTopic("/sub/node")
                                .WithPayload(JsonConvert.SerializeObject(objfile_stop))
                                .Build();

                            var message = new MqttApplicationMessageBuilder()
                                .WithTopic("/sub/node")
                                .WithPayload(JsonConvert.SerializeObject(objfile))
                                .Build();


                        await _mqttserver.InjectApplicationMessage(
                            new InjectedMqttApplicationMessage(message_stop)
                            {
                                SenderClientId = "commander"
                            });
                        await _mqttserver.InjectApplicationMessage(
                            new InjectedMqttApplicationMessage(message)
                            {
                                SenderClientId = "commander"
                            });


                    }
                        if (model.type_play == "radio")
                        {

                            var listradio = (await con.GetListAsync<RadioModel>()).Where(w => w.id.ToString() == model.key_sound);

                            if (listradio.Count() == 0) throw new Exception("ไม่พบ URL นี้");

                            var objfile = new
                            {
                                id = client_id,
                                key = model.key_sound,
                                cmd = "play-url",
                                vol = client_id.Count == 1 ? con.GetList<StationModel>().Where(w => w.key == client_id.FirstOrDefault()).FirstOrDefault().mute == true ? 0 :  liststation.Where(w=>w.key == client_id.FirstOrDefault()).FirstOrDefault().vol : con.GetList<GroupModel>().Where(w => w.key == model.key).FirstOrDefault().mute == true ? 0 :  listgrp.Where(w=>w.key==model.key).FirstOrDefault().vol  ,
                                audio = listradio.FirstOrDefault().url,
                                type = "media"
                            };

                            var objfile_stop = new
                            {
                                id = client_id,
                                type = "media",
                                cmd = "stop"

                            };

                            var message_stop = new MqttApplicationMessageBuilder()
                                .WithTopic("/sub/node")
                                .WithPayload(JsonConvert.SerializeObject(objfile_stop))
                                .Build();

                            var message = new MqttApplicationMessageBuilder()
                                .WithTopic("/sub/node")
                                .WithPayload(JsonConvert.SerializeObject(objfile))
                                .Build();


                        await _mqttserver.InjectApplicationMessage(
                            new InjectedMqttApplicationMessage(message_stop)
                            {
                                SenderClientId = "commander"
                            });
                        await _mqttserver.InjectApplicationMessage(
                            new InjectedMqttApplicationMessage(message)
                            {
                                SenderClientId = "commander"
                            });


                    }
                    else
                    {

                    }

            
                }

                return Json(new { success = true, message = "ส่งคำสั่งเล่นเสียงสำเร็จ" });
            }
            catch(Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
           
        }

        public async static void CheckScheduleAsync(object state)
        {
            var now = DateTime.Now;
            var database = new DapperContext();
            try
            {
                var ipv4 = SJPCORE.Util.NetworkHelper.GetLocalIPv4();
                using (var con = database.CreateConnection())
                {
                    con.Open();
                    try
                    {
                        List<string> client_id = new List<string>();
                        var sch = con.GetList<ScheduleModel>().OrderBy(o => o.schtime);            
                        if (sch.Count() == 0) return;     

                        Console.WriteLine("Now : " + now.ToString());
                        
                        var IsOpening = sch.Where(w => w.schtime.Value.ToString() == now.ToString());
                        var IsStoping = sch.Where(w => w.schtime.Value.AddMinutes((double)w.duration).ToString() == now.ToString());
                        var IsPlaying = sch.Where(w => w.schtime.Value < now) ;
                        var IsPending = sch.Where(w => w.schtime.Value > now) ;

                        if (IsStoping.Count() > 0)                         
                        {

                            Console.WriteLine("IsStoping ; " + IsStoping.Count());

                            foreach (var item in IsStoping)
                            {       
                                string type = new string("media");
                                Console.WriteLine(item.key + " start : " + item.schtime.ToString() + " end : " + item.schtime.Value.AddMinutes((double)item.duration).ToString() );
                                var sch_time = item.schtime;
                                var com_time = now;
                                var end_time = item.schtime.Value.AddMinutes((double)item.duration);                 

                                if (com_time.ToString() == end_time.ToString())
                                {
                                    if (item.type_station == "grp")
                                    {
                                        var grp = con.GetList<GroupAssignModel>();
                                        if (grp.Count() == 0) return;
                                        var list = grp.Where(w => w.grp_id == item.key_station).Select(s => s.station_id).ToList();
                                        client_id = list;
                                    }
                                    else
                                    {
                                        client_id.Add(item.key_station);
                                    }

                                    if (item.type_play == "storage")
                                        type = "media";

                                    if (item.once_time == true)
                                    {
                                        var onceobjfile_stop = new
                                        {
                                            id = client_id,
                                            type = type,
                                            cmd = "stop"
                                        };

                                        var oncemessage_stop = new MqttApplicationMessageBuilder()
                                        .WithTopic("/sub/node")
                                        .WithPayload(JsonConvert.SerializeObject(onceobjfile_stop))
                                        .Build();

                                        await _mqttserver.InjectApplicationMessage(
                                            new InjectedMqttApplicationMessage(oncemessage_stop)
                                            {
                                                SenderClientId = "commander"
                                            });

                                        var sameplay = con.GetList<ScheduleModel>().Where(w => w.id_play == item.id_play && w.type_play == item.type_play);
                                        if (sameplay.Count() == 1)
                                        {
                                            var objfile = new
                                            {
                                                id = client_id,
                                                cmd = "del",
                                                key = item.id_play
                                            };
                                            var message = new MqttApplicationMessageBuilder()
                                                .WithTopic("/sub/node")
                                                .WithPayload(JsonConvert.SerializeObject(objfile))
                                                .Build();

                                            await _mqttserver.InjectApplicationMessage(
                                                new InjectedMqttApplicationMessage(message)
                                                {
                                                    SenderClientId = "commander"
                                                });
                                            Console.WriteLine("del" + item.id_play);
                                        };
                                        con.Delete<ScheduleModel>(item.key);
                                        Console.WriteLine("DeleteSchedule" + item.key);

                                    }
                                    else
                                    {
                                        var objfile_stop = new
                                        {
                                            id = client_id,
                                            type = type,
                                            cmd = "stop"
                                        };

                                        var message_stop = new MqttApplicationMessageBuilder()
                                        .WithTopic("/sub/node")
                                        .WithPayload(JsonConvert.SerializeObject(objfile_stop))
                                        .Build();

                                        await _mqttserver.InjectApplicationMessage(
                                            new InjectedMqttApplicationMessage(message_stop)
                                            {
                                                SenderClientId = "commander"
                                            });         

                                        if (item.every_hour == true)
                                        {
                                            string hours = "update sjp_schedule set schtime = @AddHours where key = '" + item.key + "'";
                                            con.Execute(hours, new { AddHours = sch_time.Value.AddHours(1) });
                                        }
                                        else if (item.every_day == true)
                                        {
                                            string days = "update sjp_schedule set schtime = @AddDays where key = '" + item.key + "'";
                                            con.Execute(days, new { AddDays = sch_time.Value.AddDays(1) });
                                        }
                                        else if (item.every_week == true)
                                        {
                                            string weeks = "update sjp_schedule set schtime = @AddWeeks where key = '" + item.key + "'";
                                            con.Execute(weeks, new { AddWeeks = sch_time.Value.AddDays(7) });
                                        }
                                        else
                                        {
                                            return;
                                        }
                                    }
                                }
                            }
                        }

                        if (IsOpening.Count() > 0)                        
                        {
                            Console.WriteLine("IsOpening : " + IsOpening.Count());

                            foreach (var item in IsOpening)
                            {
                                List<string> id = new List<string>();
                                string key = item.id_play;
                                string cmd = new string("");
                                int vol = 0;
                                string audio = new string("");
                                string type = new string("media");
                                string file = new string("");

                                Console.WriteLine(item.key + " start : " + item.schtime + " end : " + item.schtime.Value.AddMinutes((double)item.duration).ToString() );
                                var sch_time = item.schtime;
                                var com_time = now;                                

                                if (sch_time.ToString() == com_time.ToString())
                                {
                                    Console.WriteLine("sch_time == com_time");
                                    if (item.type_station == "grp")
                                    {
                                        var grp = con.GetList<GroupAssignModel>();

                                        if (grp.Count() == 0) return;

                                        var list = grp.Where(w => w.grp_id == item.key_station).Select(s => s.station_id).ToList();
                                        client_id = list;
                                    }
                                    else if (item.type_station == "node")
                                    {
                                        client_id.Add(item.key_station);
                                    }
                                    else
                                    {
                                        return;
                                    }

                                    Console.WriteLine("client_id : " + client_id.Count);

                                    if (item.type_play == "storage")
                                    {
                                        Console.WriteLine("item.type_play == storage");

                                        var listfile = con.GetList<StorageModel>().Where(w => w.key == item.id_play);

                                        if (listfile.Count() == 0) throw new Exception("ไม่พบไฟล์นี้");

                                        id = client_id;
                                        key = "<schedule>youtube";
                                        cmd = "play-url";
                                        vol = client_id.Count == 1 ? con.GetList<StationModel>().Where(w => w.key == client_id.FirstOrDefault()).FirstOrDefault().mute == true ? 0 :  con.GetList<StationModel>().Where(w => w.key == client_id.FirstOrDefault()).FirstOrDefault().vol : con.GetList<GroupModel>().Where(w => w.key == item.key_station).FirstOrDefault().mute == true ? 0 :  con.GetList<GroupModel>().Where(w => w.key == item.key_station).FirstOrDefault().vol ;
                                        type = "media";

                                        var objfile = new
                                        {
                                            id = id,
                                            key = key,
                                            cmd = cmd,
                                            vol = vol,
                                            type = type,
                                            audio = "http://" + SJPCORE.Util.NetworkHelper.GetLocalIPv4() + ":5000/soundfile/" + item.id_play
                                        };
                                        var objfile_stop = new
                                        {
                                            id = client_id,
                                            type = "media",
                                            cmd = "stop"

                                        };

                                        var message_stop = new MqttApplicationMessageBuilder()
                                        .WithTopic("/sub/node")
                                        .WithPayload(JsonConvert.SerializeObject(objfile_stop))
                                        .Build();

                                        var message = new MqttApplicationMessageBuilder()
                                        .WithTopic("/sub/node")
                                        .WithPayload(JsonConvert.SerializeObject(objfile))
                                        .Build();
                                        
                                        await _mqttserver.InjectApplicationMessage(
                                            new InjectedMqttApplicationMessage(message_stop)
                                            {
                                                SenderClientId = "commander"
                                            });
                                        Thread.Sleep(500);
                                        
                                        await _mqttserver.InjectApplicationMessage(
                                            new InjectedMqttApplicationMessage(message)
                                            {
                                                SenderClientId = "commander"
                                            });

                                    }
                                    else 
                                    {                               
                                        if (item.type_play == "youtube")
                                        {
                                            Console.WriteLine("item.type_play == youtube");

                                            var listyoutube = con.GetList<YtModel>().Where(w => w.id.ToString() == item.id_play);

                                            if (listyoutube.Count() == 0) throw new Exception("ไม่พบ URL นี้");

                                            id = client_id;
                                            key = "<schedule>youtube";
                                            cmd = "play-url";
                                            vol = client_id.Count == 1 ? con.GetList<StationModel>().Where(w => w.key == client_id.FirstOrDefault()).FirstOrDefault().mute == true ? 0 :  con.GetList<StationModel>().Where(w => w.key == client_id.FirstOrDefault()).FirstOrDefault().vol : con.GetList<GroupModel>().Where(w => w.key == item.key_station).FirstOrDefault().mute == true ? 0 :  con.GetList<GroupModel>().Where(w => w.key == item.key_station).FirstOrDefault().vol ;
                                            audio =  await SJPCORE.Util.YoutubeHelper.GetAudioStreamLinkAsync(listyoutube.FirstOrDefault().url);
                                            type = "media";
                                        }
                                        else if (item.type_play == "radio")
                                        {
                                            Console.WriteLine("item.type_play == radio");
                                            var listradio = con.GetList<RadioModel>().Where(w => w.id.ToString() == item.id_play);
                                            if (listradio.Count() == 0) throw new Exception($"ไม่พบ URL นี้");

                                            id = client_id;
                                            key = "<schedule>radio";
                                            cmd = "play-url";
                                            vol = client_id.Count == 1 ? con.GetList<StationModel>().Where(w => w.key == client_id.FirstOrDefault()).FirstOrDefault().mute == true ? 0 :  con.GetList<StationModel>().Where(w => w.key == client_id.FirstOrDefault()).FirstOrDefault().vol : con.GetList<GroupModel>().Where(w => w.key == item.key_station).FirstOrDefault().mute == true ? 0 :  con.GetList<GroupModel>().Where(w => w.key == item.key_station).FirstOrDefault().vol ;
                                            audio = listradio.FirstOrDefault().url;
                                            type = "media";
                                        }
                                        else
                                        {
                                            return;
                                        }
                                        var objfile = new
                                        {
                                            id = id,
                                            key = key,
                                            cmd = cmd,
                                            vol = vol,
                                            audio = audio,
                                            type = type,
                                            file = file
                                        };
                                        var objfile_stop = new
                                        {
                                            id = client_id,
                                            type = "media",
                                            cmd = "stop"

                                        };

                                        var message_stop = new MqttApplicationMessageBuilder()
                                        .WithTopic("/sub/node")
                                        .WithPayload(JsonConvert.SerializeObject(objfile_stop))
                                        .Build();

                                        var message = new MqttApplicationMessageBuilder()
                                        .WithTopic("/sub/node")
                                        .WithPayload(JsonConvert.SerializeObject(objfile))
                                        .Build();
                                        
                                        await _mqttserver.InjectApplicationMessage(
                                            new InjectedMqttApplicationMessage(message_stop)
                                            {
                                                SenderClientId = "commander"
                                            });
                                        Thread.Sleep(500);

                                        await _mqttserver.InjectApplicationMessage(
                                            new InjectedMqttApplicationMessage(message)
                                            {
                                                SenderClientId = "commander"
                                            });
                                    }
                                    

                                    string query = $"update sjp_schedule set lastest_played = @Now where key = '{item.key}'";
                                    con.Execute(query, new { Now = now });

                                    client_id.Clear();
                                }
                            }
                        }

                        if (IsPlaying.Count() > 0)
                        {
                            // Console.WriteLine("IsPlaying ; " + IsPlaying.Count());
                            foreach (var item in IsPlaying)
                            {       
                                // Console.WriteLine(item.key + " start : " + item.schtime.ToString() + " end : " + item.schtime.Value.AddMinutes((double)item.duration).ToString() );   
                            }                            
                        }

                        if (IsPending.Count() > 0)
                        {
                            // Console.WriteLine("IsPending : " + IsPending.Count());
                            foreach (var item in IsPending)
                            {       
                                // Console.WriteLine(item.key + " start : " + item.schtime.ToString() + " end : " + item.schtime.Value.AddMinutes((double)item.duration).ToString() );   
                            }                            
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (database != null)
                {
                    database = null;
                }
            }
        }

        [HttpDelete("schedule/del/{key}")]
        public async Task<IActionResult> DeleteSchedule(string key)
        {
            Console.WriteLine("DeleteSchedule" + key);

            var result = ScheduleController.DeleteSchedule(key);

            Console.WriteLine("DeleteSchedule" + result.message);
            if (result.success)
            {
                if (result.filedelete)
                {
                    var objfile = new
                    {
                        id = result.key,
                        cmd = "del",
                        key = result.schedule.id_play
                    };

                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic("/sub/node")
                        .WithPayload(JsonConvert.SerializeObject(objfile))
                        .Build();

                    await _mqttserver.InjectApplicationMessage(
                        new InjectedMqttApplicationMessage(message)
                        {
                            SenderClientId = "commander"
                        });
                };

                if (result.IsPlaying)
                {
                    var database = new DapperContext();
                    using(var con = database.CreateConnection())
                    {
                        var client_id = new List<string>();
                        if (result.schedule.type_station == "grp")
                        {
                            var grp = con.GetList<GroupAssignModel>();
                            if (grp.Count() == 0) return Ok(new { success = false, message = "ไม่พบข้อมูลกลุ่ม" }); 
                            var list = grp.Where(w => w.grp_id == result.schedule.key_station).Select(s => s.station_id).ToList();
                            client_id = list;
                        }
                        else
                        {
                            client_id.Add(result.schedule.key_station);
                        }

                        var type = "media";

                        if (result.schedule.type_play == "storage")
                            type = "media";

                        var objfile_stop = new
                        {
                            id = client_id,
                            type = type,
                            cmd = "stop"
                        };

                        var message_stop = new MqttApplicationMessageBuilder()
                        .WithTopic("/sub/node")
                        .WithPayload(JsonConvert.SerializeObject(objfile_stop))
                        .Build();

                        await _mqttserver.InjectApplicationMessage(
                            new InjectedMqttApplicationMessage(message_stop)
                            {
                                SenderClientId = "commander"
                            });
                    }
                };

                return Ok(new { success = true, message = result.message });
            }
            else
            {
                return Ok(new { success = false, message = result.message });
            }
        }

        public async static void DeletePassed()
        {
            Thread.Sleep(30000);
            var database = new DapperContext();
            try        
            {
                Console.WriteLine("DeletePassed");
                using(var con = database.CreateConnection())
                {  
                    Console.WriteLine("Connect Database");     
                    var client_id = new List<string>();
                    var sch = con.GetList<ScheduleModel>().Where(w => w.schtime.Value <= DateTime.Now);
                    if (sch.Count() == 0) return;                    
                        
                    Console.WriteLine("sch.Count() = " + sch.Count());
                    foreach (var item in sch)
                    {
                        if (item.type_station == "grp")
                        {
                            var grp = con.GetList<GroupAssignModel>();
                            if (grp.Count() == 0) return ;
                            var list = grp.Where(w => w.grp_id == item.key_station).Select(s => s.station_id).ToList();
                            client_id = list;

                        }
                        else
                        {
                            client_id.Add(item.key_station);
                        }

                        Console.WriteLine("client_id = " + client_id);

                        var objfile_stop = new
                        {
                            id = client_id,
                            type = "media",
                            cmd = "stop"

                        };

                        var message_stop = new MqttApplicationMessageBuilder()
                        .WithTopic("/sub/node")
                        .WithPayload(JsonConvert.SerializeObject(objfile_stop))
                        .Build();

                        await _mqttserver.InjectApplicationMessage(
                        new InjectedMqttApplicationMessage(message_stop)
                        {
                            SenderClientId = "commander"
                        });

                        if (item.once_time == true)
                        {   
                            Console.WriteLine("once_time = " + item.once_time);                       
                            var sameplay = con.GetList<ScheduleModel>().Where(w => w.id_play == item.id_play && w.type_play == item.type_play);
                            Console.WriteLine("sameplay.Count() = " + sameplay.Count());
                            if (sameplay.Count() == 1)
                            {
                                var objfile = new
                                {
                                    id = client_id,
                                    cmd = "del",
                                    key = item.id_play
                                };

                                var message = new MqttApplicationMessageBuilder()
                                    .WithTopic("/sub/node")
                                    .WithPayload(JsonConvert.SerializeObject(objfile))
                                    .Build();

                                Console.WriteLine("message = " + message);
                                await _mqttserver.InjectApplicationMessage(
                                    new InjectedMqttApplicationMessage(message)
                                    {
                                        SenderClientId = "commander"
                                    });

                                Console.WriteLine("InjectApplicationMessage");
                            };

                            con.Delete<ScheduleModel>(item.key);
                            Console.WriteLine("DeleteSchedule" + item.key);

                        }
                        else if (item.every_hour == true)
                        {
                            string hours = "update sjp_schedule set schtime = @AddHours where key = '" + item.key + "'";
                            DateTime now = DateTime.Now;
                            if (now.Minute >= item.schtime.Value.Minute)
                                now = now.AddHours(1);
                            con.Execute(hours, new { AddHours = new DateTime(now.Year, now.Month, now.Day, now.Hour, item.schtime.Value.Minute, 0)});
                        }
                        else if (item.every_day == true)
                        {
                            string days = "update sjp_schedule set schtime = @AddDays where key = '" + item.key + "'";
                            DateTime now = DateTime.Now;
                            if (now.TimeOfDay >= item.schtime.Value.TimeOfDay)
                                now = now.AddDays(1);
                            con.Execute(days, new { AddDays = new DateTime(now.Year, now.Month, now.Day, item.schtime.Value.Hour, item.schtime.Value.Minute, 0) });
                        }
                        else if (item.every_week == true)
                        {
                            string weeks = "update sjp_schedule set schtime = @AddWeeks where key = '" + item.key + "'";
                            DateTime now = DateTime.Now;
                            if (now.TimeOfDay >= item.schtime.Value.TimeOfDay && now.DayOfWeek >= item.schtime.Value.DayOfWeek)
                                now = now.AddDays(7);
                            con.Execute(weeks, new { AddWeeks = DateTime.Now.AddDays(7) });
                        }
                        else
                        {
                            return;
                        }                            
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (database != null)
                {
                    database = null;
                }
            }

            
        }

    }
}
