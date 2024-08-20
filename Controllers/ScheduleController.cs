using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using SJPCORE.Models;
using MQTTnet.Client;
using MQTTnet;
using MQTTnet.Server;
using Newtonsoft.Json;
using System.IO;

namespace SJPCORE.Controllers
{
    [ApiController]
    [Route("api/station/schedule")]
    public class ScheduleController : ControllerBase
    {
        private static DapperContext _context;

        public ScheduleController(DapperContext context)
        {
            _context = context;
        }

        [HttpGet("station")]
        public IActionResult GetStationList()
        {
            try
            {
                var model = new List<SchStationModel>();
                var grps = SJPCORE.Util.GroupHelper.getGroupNode();
                var nodes = SJPCORE.Util.StationHelper.getStation();

                foreach (var grp in grps)
                {
                    model.Add(new SchStationModel()
                    {
                        key = grp.Key,
                        name = grp.Value.name,
                        type = "grp"
                    });
                }

                foreach (var node in nodes)
                {
                    model.Add(new SchStationModel()
                    {
                        key = node.Key,
                        name = node.Value,
                        type = "node"
                    });
                }

                return Ok(new { success = true, message = $"Get Success!!", data = model });

            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }
        
        public class ScheduleModelandName
        {
            public ScheduleModel schedule { get; set; }
            public string name_station { get; set; }
            public string name_play { get; set; }
        }

        [HttpGet]
        public IActionResult GetScheduleList()
        {
            try
            {
                using (var con = _context.CreateConnection())
                {
                    con.Open();
                    var sch = new List<ScheduleModelandName>();
                    var list = con.GetList<ScheduleModel>().OrderBy(o => o.schtime);
                    var nodes = SJPCORE.Util.StationHelper.getStation();
                    var grps = SJPCORE.Util.GroupHelper.getGroupNode();
                    var name_station = "";
                    var name_play = "";
                    foreach (var s in list)
                    {                        
                        if (s.type_station == "node")
                        {
                            name_station = nodes[s.key_station];
                        }
                        else if (s.type_station == "grp")
                        {
                            name_station = grps[s.key_station].name;
                        }
                        switch (s.type_play)
                        {
                            case "storage":
                                var listfile = con.GetList<StorageModel>();
                                foreach (var f in listfile)
                                {
                                    if (f.key == s.id_play)
                                    {
                                        name_play = f.name_defualt;
                                        break;
                                    }
                                }         
                                break;
                            case "youtube":
                                var listyoutube = con.GetList<YtModel>();
                                foreach (var f in listyoutube)
                                {
                                    if (f.id.ToString() == s.id_play)
                                    {
                                        name_play = f.name;
                                        break;
                                    }
                                }
                                break;
                            case "radio":
                                var listradio = con.GetList<RadioModel>();
                                foreach (var f in listradio)
                                {
                                    if (f.id.ToString() == s.id_play)
                                    {
                                        name_play = f.name;
                                        break;
                                    }
                                }
                                break;
                        }
                        sch.Add(new ScheduleModelandName()
                        {
                            schedule = s,
                            name_station = name_station,
                            name_play = name_play       
                        });

                    }
                    
                    return Ok(new { success = true, message = $"Get Success!!", data = sch });
                }
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPost]
        public IActionResult AddSchedule([FromBody] AddScheduleRequest schedule_req)
        {
            var schedule = new ScheduleModel();

            // Map form data to ScheduleModel properties
            schedule.type_station = schedule_req.type_station;
            schedule.key_station = schedule_req.key_station;
            schedule.type_play = schedule_req.type_play;
            schedule.id_play = schedule_req.id_play;
            schedule.schtime = schedule_req.schtime;
            schedule.duration = schedule_req.duration;
            schedule.key = SJPCORE.Util.StorageHelper.GenerateShortGuid("SCH");

            if (schedule.schtime <= DateTime.Now)
            {
                return Ok(new { success = false, message = $"เวลาตามกำหนดต้องมากกว่าเวลาปัจจุบัน" });
            }

            switch (schedule_req.repeat)
            {
                case "once":
                    schedule.once_time = true;
                    break;
                case "everyHour":
                    schedule.every_hour = true;
                    break;
                case "everyDay":
                    schedule.every_day = true;
                    break;
                case "everyWeek":
                    schedule.every_week = true;
                    break;
                default:
                    // Invalid repeat value, return BadRequest
                    return BadRequest();
            }

            try
            {
                using (var con = _context.CreateConnection())
                {
                    con.Open();
                    var listschedule = con.GetList<ScheduleModel>();
                    var nodes = SJPCORE.Util.StationHelper.getStation();
                    var newstart = schedule.schtime.Value;
                    var newend = schedule.schtime.Value.AddMinutes((double)schedule.duration);
                    if (schedule.type_station == "node")
                    {
                        foreach (var sch in listschedule)
                        {
                            var existingstart = sch.schtime.Value;
                            var existingend = sch.schtime.Value.AddMinutes((double)sch.duration);
                            if (sch.type_station == "node")
                            {
                                if(sch.key_station == schedule.key_station)
                                {
                                    if (newstart.Minute >= existingstart.Minute && newstart.Minute < existingstart.Minute + sch.duration ||
                                        newstart.Minute + schedule.duration > existingstart.Minute && newstart.Minute + schedule.duration <= existingstart.Minute + sch.duration ||
                                        existingstart.Minute >= newstart.Minute && existingstart.Minute < newstart.Minute + schedule.duration ||
                                        existingstart.Minute + sch.duration > newstart.Minute && existingstart.Minute + sch.duration <= newstart.Minute + schedule.duration)
                                    {
                                        if (sch.every_hour)
                                            return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key}" });
                                        if (newstart.TimeOfDay >= existingstart.TimeOfDay && newstart.TimeOfDay < existingend.TimeOfDay ||
                                            newend.TimeOfDay > existingstart.TimeOfDay && newend.TimeOfDay <= existingend.TimeOfDay ||
                                            existingstart.TimeOfDay >= newstart.TimeOfDay && existingstart.TimeOfDay < newend.TimeOfDay ||
                                            existingend.TimeOfDay > newstart.TimeOfDay && existingend.TimeOfDay <= newend.TimeOfDay)
                                        {
                                            if (sch.every_day)
                                                return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key}" });
                                            if (newstart.DayOfWeek == existingstart.DayOfWeek)
                                            {
                                                if (sch.every_week)
                                                    return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key}" });
                                                if (newstart >= existingstart && newstart < existingend ||
                                                newend > existingstart && newend <= existingend ||
                                                existingstart >= newstart && existingstart < newend ||
                                                existingend > newstart && existingend <= newend)
                                                    return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key}" });
                                            }
                                        }
                                    }
                                }
                            }
                            if (sch.type_station == "grp")
                            {
                                var Groups = SJPCORE.Util.GroupHelper.getGroupNode();
                                foreach (var Group in Groups)
                                {
                                    if (Group.Key == sch.key_station)
                                    {
                                        foreach (var node in Group.Value.nodes)
                                        {
                                            if (node == schedule.key_station)
                                            {
                                                if (newstart.Minute >= existingstart.Minute && newstart.Minute < existingstart.Minute + sch.duration ||
                                                    newstart.Minute + schedule.duration > existingstart.Minute && newstart.Minute + schedule.duration <= existingstart.Minute + sch.duration ||
                                                    existingstart.Minute >= newstart.Minute && existingstart.Minute < newstart.Minute + schedule.duration ||
                                                    existingstart.Minute + sch.duration > newstart.Minute && existingstart.Minute + sch.duration <= newstart.Minute + schedule.duration)
                                                {
                                                    if (sch.every_hour)
                                                        return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key} ที่กลุ่ม {Group.Value.name}" });
                                                    if (newstart.TimeOfDay >= existingstart.TimeOfDay && newstart.TimeOfDay < existingend.TimeOfDay ||
                                                        newend.TimeOfDay > existingstart.TimeOfDay && newend.TimeOfDay <= existingend.TimeOfDay ||
                                                        existingstart.TimeOfDay >= newstart.TimeOfDay && existingstart.TimeOfDay < newend.TimeOfDay ||
                                                        existingend.TimeOfDay > newstart.TimeOfDay && existingend.TimeOfDay <= newend.TimeOfDay)
                                                    {
                                                        if (sch.every_day)
                                                            return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key} ที่กลุ่ม {Group.Value.name}" });
                                                        if (newstart.DayOfWeek == existingstart.DayOfWeek)
                                                        {
                                                            if (sch.every_week)
                                                                return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key} ที่กลุ่ม {Group.Value.name}" });
                                                            if (newstart >= existingstart && newstart < existingend ||
                                                            newend > existingstart && newend <= existingend ||
                                                            existingstart >= newstart && existingstart < newend ||
                                                            existingend > newstart && existingend <= newend)
                                                                return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key} ที่กลุ่ม {Group.Value.name}" });
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (schedule.type_station == "grp")
                    {
                        foreach (var sch in listschedule)
                        {
                            var existingstart = sch.schtime.Value;
                            var existingend = sch.schtime.Value.AddMinutes((double)sch.duration);
                            if (sch.type_station == "node")
                            {
                                var Groups = SJPCORE.Util.GroupHelper.getGroupNode();
                                foreach (var Group in Groups)
                                {
                                    if (Group.Key == schedule.key_station)
                                    {
                                        foreach (var node in Group.Value.nodes)
                                        {
                                            if (node == sch.key_station)
                                            {
                                                if (newstart.Minute >= existingstart.Minute && newstart.Minute < existingstart.Minute + sch.duration ||
                                                    newstart.Minute + schedule.duration > existingstart.Minute && newstart.Minute + schedule.duration <= existingstart.Minute + sch.duration ||
                                                    existingstart.Minute >= newstart.Minute && existingstart.Minute < newstart.Minute + schedule.duration ||
                                                    existingstart.Minute + sch.duration > newstart.Minute && existingstart.Minute + sch.duration <= newstart.Minute + schedule.duration)
                                                {
                                                    if (sch.every_hour)
                                                        return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key} ที่สถานี {nodes[node]}" });
                                                    if (newstart.TimeOfDay >= existingstart.TimeOfDay && newstart.TimeOfDay < existingend.TimeOfDay ||
                                                        newend.TimeOfDay > existingstart.TimeOfDay && newend.TimeOfDay <= existingend.TimeOfDay ||
                                                        existingstart.TimeOfDay >= newstart.TimeOfDay && existingstart.TimeOfDay < newend.TimeOfDay ||
                                                        existingend.TimeOfDay > newstart.TimeOfDay && existingend.TimeOfDay <= newend.TimeOfDay)
                                                    {
                                                        if (sch.every_day)
                                                            return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key} ที่สถานี {nodes[node]}" });
                                                        if (newstart.DayOfWeek == existingstart.DayOfWeek)
                                                        {
                                                            if (sch.every_week)
                                                                return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key} ที่สถานี {nodes[node]}" });
                                                            if (newstart >= existingstart && newstart < existingend ||
                                                            newend > existingstart && newend <= existingend ||
                                                            existingstart >= newstart && existingstart < newend ||
                                                            existingend > newstart && existingend <= newend)
                                                                return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key} ที่สถานี {nodes[node]}" });
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (sch.type_station == "grp")
                            {
                                if (sch.key_station == schedule.key_station)
                                {   
                                    if (newstart.Minute >= existingstart.Minute && newstart.Minute < existingstart.Minute + sch.duration ||
                                        newstart.Minute + schedule.duration > existingstart.Minute && newstart.Minute + schedule.duration <= existingstart.Minute + sch.duration ||
                                        existingstart.Minute >= newstart.Minute && existingstart.Minute < newstart.Minute + schedule.duration ||
                                        existingstart.Minute + sch.duration > newstart.Minute && existingstart.Minute + sch.duration <= newstart.Minute + schedule.duration)
                                    {
                                        if (sch.every_hour)
                                            return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key}" });
                                        if (newstart.TimeOfDay >= existingstart.TimeOfDay && newstart.TimeOfDay < existingend.TimeOfDay ||
                                            newend.TimeOfDay > existingstart.TimeOfDay && newend.TimeOfDay <= existingend.TimeOfDay ||
                                            existingstart.TimeOfDay >= newstart.TimeOfDay && existingstart.TimeOfDay < newend.TimeOfDay ||
                                            existingend.TimeOfDay > newstart.TimeOfDay && existingend.TimeOfDay <= newend.TimeOfDay)
                                        {
                                            if (sch.every_day)
                                                return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key}" });
                                            if (newstart.DayOfWeek == existingstart.DayOfWeek)
                                            {
                                                if (sch.every_week)
                                                    return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key}" });
                                                if (newstart >= existingstart && newstart < existingend ||
                                                newend > existingstart && newend <= existingend ||
                                                existingstart >= newstart && existingstart < newend ||
                                                existingend > newstart && existingend <= newend)
                                                    return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key}" });
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    var Groups = SJPCORE.Util.GroupHelper.getGroupNode();
                                    foreach (var group in Groups)
                                    {
                                        if (group.Key == schedule.key_station)
                                        {
                                            foreach (var newnode in group.Value.nodes)
                                            {
                                                foreach (var group2 in Groups)
                                                {
                                                    if (group2.Key == sch.key_station)
                                                    {
                                                        foreach (var existnode in group2.Value.nodes)
                                                        {
                                                            if (newnode == existnode)
                                                            {
                                                                if (newstart.Minute >= existingstart.Minute && newstart.Minute < existingstart.Minute + sch.duration ||
                                                                    newstart.Minute + schedule.duration > existingstart.Minute && newstart.Minute + schedule.duration <= existingstart.Minute + sch.duration ||
                                                                    existingstart.Minute >= newstart.Minute && existingstart.Minute < newstart.Minute + schedule.duration ||
                                                                    existingstart.Minute + sch.duration > newstart.Minute && existingstart.Minute + sch.duration <= newstart.Minute + schedule.duration)
                                                                {
                                                                    if (sch.every_hour)
                                                                        return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key}" });
                                                                    if (newstart.TimeOfDay >= existingstart.TimeOfDay && newstart.TimeOfDay < existingend.TimeOfDay ||
                                                                        newend.TimeOfDay > existingstart.TimeOfDay && newend.TimeOfDay <= existingend.TimeOfDay ||
                                                                        existingstart.TimeOfDay >= newstart.TimeOfDay && existingstart.TimeOfDay < newend.TimeOfDay ||
                                                                        existingend.TimeOfDay > newstart.TimeOfDay && existingend.TimeOfDay <= newend.TimeOfDay)
                                                                    {
                                                                        if (sch.every_day)
                                                                            return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key}" });
                                                                        if (newstart.DayOfWeek == existingstart.DayOfWeek)
                                                                        {
                                                                            if (sch.every_week)
                                                                                return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key}" });
                                                                            if (newstart >= existingstart && newstart < existingend ||
                                                                            newend > existingstart && newend <= existingend ||
                                                                            existingstart >= newstart && existingstart < newend ||
                                                                            existingend > newstart && existingend <= newend)
                                                                                return Ok(new { success = false, message = $"เวลาของกำหนดการใหม่ ขัดกับเวลาของกำหนดการ ID {sch.key}" });
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }   
                                                    }
                                                }
                                            }
                                        } 
                                    }
                                }
                            }
                        }
                    }
                    
                    if (schedule.type_play == "storage")
                    {
                        var sameplay = con.GetList<ScheduleModel>(new { schedule.type_play, schedule.id_play, schedule.key_station, schedule.type_station });
                        if (!(sameplay.Count() > 0))
                        {
                            List<string> client_id = new List<string>();
                            if (schedule.type_station == "grp")
                            {
                                var grp = con.GetList<GroupAssignModel>();
                                if (grp.Count() == 0)
                                    throw new Exception("ไม่พบกลุ่มสถานี");

                                var list = grp.Where(w => w.grp_id == schedule.key_station).Select(s => s.station_id).ToList();
                                client_id = list;
                            }
                            else if (schedule.type_station == "node")
                            {
                                client_id.Add(schedule.key_station);
                            }
                            var listfile = con.GetList<StorageModel>(new { key = schedule.id_play }).FirstOrDefault();
                                
                                if (listfile == null) throw new Exception("ไม่พบไฟล์นี้");

                                var objfile = new
                                {
                                    id = client_id,
                                    key = schedule.id_play,
                                    cmd = "keep",
                                    audio = Convert.ToBase64String(System.IO.File.ReadAllBytes(Path.Combine(listfile.path, listfile.name_server))),
                                    file = Path.GetExtension(Path.Combine(listfile.path, listfile.name_server)).Replace(".", ""),
                                };
                            var message = new MqttApplicationMessageBuilder()
                                    .WithTopic("/sub/node")
                                    .WithPayload(JsonConvert.SerializeObject(objfile))
                                    .Build();

                            SJPCORE.Controllers.StationController._mqttserver.InjectApplicationMessage(
                                new InjectedMqttApplicationMessage(message)
                                {
                                    SenderClientId = "commander"
                                });
                        }
                    }

                    


                    using (var tx = con.BeginTransaction())
                    {
                        // Insert new schedule
                        int scheduleId = con.QuerySingle<int>(@"INSERT INTO sjp_schedule
                                    (key, type_station,key_station, type_play, id_play, schtime, duration, once_time, every_hour, every_day, every_week, lastest_played, timestamp_create)
                                    VALUES
                                    (@key,@type_station, @key_station, @type_play, @id_play, @schtime, @duration, @once_time, @every_hour, @every_day, @every_week, @lastest_played, @timestamp_create);
                                    SELECT last_insert_rowid();",
                                            schedule, tx);

                        // Commit transaction
                        tx.Commit();

                        // Return success response with schedule id
                        return Ok(new { success = true, message = "Schedule added successfully", id = scheduleId });
                    }
                }
            }
            catch (Exception ex)
            {
                // Return error response with error message
                return Ok(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        public class DeleteScheduleRequest
        {
            public List<string> key { get; set; }
            public ScheduleModel schedule { get; set; }
            public bool success { get; set; }
            public string message { get; set; }
            public bool IsPlaying { get; set; }
            public bool filedelete { get; set; }
        }

        public static DeleteScheduleRequest DeleteSchedule(string id)
        {   
            try
            {        
                using (var con = _context.CreateConnection())
                {
                    con.Open();
                    var sch = new DeleteScheduleRequest();
                    
                    
                    sch.schedule = con.Get<ScheduleModel>(id);
                    List<string> client_id = new List<string>();
                    if (sch.schedule.type_station == "grp")
                    {
                        var grp = con.GetList<GroupAssignModel>();

                        if (grp.Count() == 0) throw new Exception("ไม่พบกลุ่มสถานี");

                        var list = grp.Where(w => w.grp_id == sch.schedule.key_station).Select(s => s.station_id).ToList();
                        client_id = list;
                    }
                    else if (sch.schedule.type_station == "node")
                    {
                        client_id.Add(sch.schedule.key_station);
                    }
                    sch.key = client_id;

                    if (sch.schedule == null)
                    {
                        sch.success = false;
                        sch.message = "ไม่พบข้อมูล" + id + " ในระบบ";
                        return sch;  
                    }
                    var sameplay = con.GetList<ScheduleModel>(new { sch.schedule.type_play, sch.schedule.id_play });
                    if (sameplay.Count() > 1)
                    {
                        sch.filedelete = false;
                    }
                    else
                    {
                        sch.filedelete = true;
                    }
                    if (sch.schedule.schtime <= DateTime.Now && sch.schedule.schtime.Value.AddMinutes((double)sch.schedule.duration) > DateTime.Now)
                    {
                        sch.IsPlaying = true;
                    }
                    else
                    {
                        sch.IsPlaying = false;
                    }
                    con.Delete(sch.schedule);
                    sch.success = true;
                    sch.message = $"ลบข้อมูล {sch.key} เรียบร้อย";
                    return sch;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                var sch = new DeleteScheduleRequest();
                sch.success = false;
                sch.message = ex.Message;
                return sch;
            }
        }
    }

}
