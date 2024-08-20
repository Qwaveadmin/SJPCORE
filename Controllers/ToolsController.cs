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
using Microsoft.AspNetCore.Connections;
using System.Security.Policy;
using SJPCORE.Models.Attribute;
using SJPCORE.Util;

namespace SJPCORE.Controllers
{
    [AuthorizationHeader]
    public class ToolsController : Controller
    {
        private readonly ILogger<ToolsController> _logger;
        private readonly DapperContext _context;

        public ToolsController(ILogger<ToolsController> logger, DapperContext context)
        {
            _logger = logger;
            _context = context;
        }
        [ActionName("Tts")]
        public IActionResult Tts()
        {
            return View("tts-tools");
        }


        [HttpGet]
        [ActionName("YtPlayer")]
        public async Task<IActionResult> YtPlayer()
        {
            using (var con = _context.CreateConnection())
            {
                return View("yt-tools", await con.GetListAsync<YtModel>());
            }
        }
        [HttpPost]
        [ActionName("YtPlayer")]
        public async Task<IActionResult> YtPlayer_Add([FromBody] YtModel body)
        {
            using (var con = _context.CreateConnection())
            {
                if (!Uri.TryCreate(body.url, UriKind.Absolute, out Uri uri) || !(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    return Json(new { success = false, message = "รูปเเบบ URL ไม่ถูกต้องครับ!!" });
                }

                if ((await con.GetListAsync<YtModel>()).Where(w => w.url == body.url).Count() > 0)
                {
                    return Json(new { success = false, message = "มี URL นี้ในระบบเเล้วครับ!!" });
                }

                string sql = @"INSERT INTO sjp_youtube (url,name,update_at, create_at)
                                           VALUES (@url, @name,@update_at, @create_at)";

                var current_file = new YtModel
                {
                    name= body.name,
                    create_at = DateTime.Now,
                    update_at = DateTime.Now,
                    url = body.url
                };
                await con.ExecuteAsync(sql, current_file);

                return Json(new { success = true, message = "เพิ่ม URL Youtube สำเร็จ!!" });

            }
        }

        [HttpDelete]
        [ActionName("YtPlayer")]
        public async Task<IActionResult> YtPlayer_Delete(string id)
        {
            using (var con = _context.CreateConnection())
            {

                await con.DeleteAsync<YtModel>(id);
                return Json(new { success = true, message = "ลบ URL Youtube สำเร็จ!!" });

            }
        }

        [HttpPost("tools/GobYtPlayer")]
        public async Task<IActionResult> YtPlayer_Url(string url)
        {
            return Json(new { success = true, message = await YoutubeHelper.GetAudioStreamLinkAsync(url) });
        }

        [HttpGet]
        [ActionName("FmPlayer")]
        public async Task<IActionResult> FmPlayer()
        {
            using (var con = _context.CreateConnection())
            {
                return View("fm-tools", await con.GetListAsync<RadioModel>());
            }
        }
        [HttpPost]
        [ActionName("FmPlayer")]
        public async Task<IActionResult> FmPlayer_Add([FromBody] RadioBody body)
        {
            using (var con = _context.CreateConnection())
            {
                if (!Uri.TryCreate(body.url, UriKind.Absolute, out Uri uri) || !(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    return Json(new { success = false, message = "รูปเเบบ URL ไม่ถูกต้องครับ!!" });
                }

                if ((await con.GetListAsync<RadioModel>()).Where(w=>w.url == body.url).Count() > 0)
                {
                    return Json(new { success = false, message = "มี URL นี้ในระบบเเล้วครับ!!" });
                }

                string sql = @"INSERT INTO sjp_radio (url,name,update_at, create_at)
                                           VALUES (@url,@name,@update_at, @create_at)";

                var current_file = new RadioModel
                {
                    name= body.name,
                    create_at = DateTime.Now,
                    update_at = DateTime.Now,
                    url = body.url
                };
                await con.ExecuteAsync(sql, current_file);

                return Json(new { success = true, message = "เพิ่มสถานีวิทยุสำเร็จ!!" });

            }
        }

        [HttpDelete]
        [ActionName("FmPlayer")]
        public async Task<IActionResult> FmPlayer_Delete(string id)
        {
            using (var con = _context.CreateConnection())
            {

                await con.DeleteAsync<RadioModel>(id);
                return Json(new { success = true, message = "ลบสถานีวิทยุสำเร็จ!!" });

            }
        }

        [ActionName("Test")]
        public IActionResult Test()
        {
            return View("testmic-tools");
        }

    }
}