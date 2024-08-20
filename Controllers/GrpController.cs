using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SJPCORE.Models;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using Dapper;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace SJPCORE.Controllers
{
    [Route("station/group")]
    public class GrpController : Controller
    {

        private readonly ILogger<GrpController> _logger;
        private readonly DapperContext _context;



        public GrpController(ILogger<GrpController> logger, DapperContext context)
        {
            _logger = logger;
            _context = context;
        }


        [HttpGet("get")]
        public ActionResult index()
        {
            return View("group-station");
        }

        [HttpPost("create")]
        public async Task<ActionResult> create_assign([FromBody] _GroupAssignModel body)
        {
            if (body == null)
            {
                return Ok(new { success = false, message = "Invalid request body." });
            }

            string name = body.name;
            string[] nodesArray = body.nodes;

            if (string.IsNullOrEmpty(name) || nodesArray == null || nodesArray.Length == 0)
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
                        var exits = await con.GetListAsync<GroupModel>(new { name = name });
                        if (exits.Count() > 0)
                        {
                            return Ok(new { success = false, message = "มีกลุ่มนี้อยู่ในระบบแล้ว" });
                        }

                        string sql = "INSERT INTO sjp_grp (key, name, update_at, create_at) VALUES (@key, @name, @update_at, @create_at)";
                        var groupModel = new GroupModel
                        {
                            name = name
                        };
                        int rowsAffected = await con.ExecuteAsync(sql, new { key = groupModel.key, name = groupModel.name, update_at = groupModel.update_at, create_at = groupModel.create_at }, transaction);
                        if (rowsAffected <= 0)
                        {
           
                            return Ok(new { success = false, message = "Failed to create group." });

                        }

                        sql = "INSERT INTO sjp_grp_assign (grp_id, station_id) SELECT @grp_id, key FROM sjp_station WHERE key IN @station_ids;";
                        rowsAffected = await con.ExecuteAsync(sql, new { grp_id = groupModel.key, station_ids = nodesArray }, transaction);
                        if (rowsAffected <= 0)
                        {
                            return Ok(new { success = false, message = "Failed to assign nodes to group." });
                        }

   
                        transaction.Commit();
                        return Ok(new { success = true, message = $"สร้างกลุ่ม [{groupModel.key}] {groupModel.name}(จำนวน {nodesArray.Length} สถานี) สำเร็จ!!" });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Ok(new { success = false, message = $"An error occurred: {ex.Message}" });
                    }
                }
            }
        }

        [HttpPost("assign")]
        public async Task<ActionResult> assign([FromBody] JObject body)
        {
            if (body == null)
            {
                return BadRequest("Invalid request body.");
            }

            string groupKey = body.GetValue("group_key").ToString();
            JArray nodesArray = body.GetValue("nodes") as JArray;
            if (string.IsNullOrEmpty(groupKey) || nodesArray == null || nodesArray.Count == 0)
            {
                return BadRequest("Invalid request body.");
            }

            using (var con = _context.CreateConnection())
            {
                try
                {
                    // Insert the new group assignments into the database
                    string sql = "INSERT INTO sjp_grp_assign (grp_id, station_id) VALUES (@grp_id, @station_id)";
                    var parameters = nodesArray.Select(x => new { grp_id = groupKey, station_id = x.ToString() });
                    int rowsAffected = await con.ExecuteAsync(sql, parameters);
                    if (rowsAffected <= 0)
                    {
                        return StatusCode(500, "Failed to assign nodes to group.");
                    }

                    // Return a success response
                    return Ok("Nodes assigned to group successfully.");
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that occur
                    return StatusCode(500, $"An error occurred: {ex.Message}");
                }
            }
        }

        [HttpDelete("del")]
        public async Task<IActionResult> YtPlayer_Delete(string id)
        {
            using (var con = _context.CreateConnection())
            {

                var del = await con.DeleteAsync<GroupModel>(id);
                var del_sub = await con.DeleteAsync<GroupAssignModel>(id);
                return Json(new { success = true, message = $"ลบกลุ่ม [{id}] สำเร็จ!!" });

            }
        }
    }
}
