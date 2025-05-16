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
using SJPCORE.Models.Attribute;
using Microsoft.AspNetCore.Authorization;

namespace SJPCORE.Controllers
{
    [Authorize(Policy = "RequireAdmin")]
    public class SettingsController : Controller
    {
        private readonly ILogger<DashBoardController> _logger;
        private readonly DapperContext _context;

        public SettingsController(ILogger<DashBoardController> logger, DapperContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        [ActionName("Website")]
        public IActionResult Server()
        {
            return View("setttings-server");
        }

        [HttpGet]
        [ActionName("Export")]
        public IActionResult Report()
        {
            return View("report");
        }

        [HttpPost]
        [ActionName("Website")]
        public IActionResult WebSettings([FromBody]SettingModel settings)
        {
            try
            {
                using (var con = _context.CreateConnection())
                {

                Dictionary<string, string> dic = new Dictionary<string, string>()
                {
                    {"STATION_NAME",settings.STATION_NAME},
                    {"FOOTER",settings.FOOTER},
                    {"BANNER_NOTICE",settings.BANNER_NOTICE}
                };
                    List<ConfigModel> configs = new();
                    foreach (var item in dic)
                    {
                        var config = new ConfigModel { key = item.Key, value = item.Value };
                        configs.Add(config);
                        con.UpdateAsync(config);
                    }
                    GlobalParameter.Config = configs;

                    return Json(new { success = true, message = "อัพเดตการตั้งค่าสำเร็จ!!" });

                }
            }
            catch(Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        //[ActionName("ThemeMode")]
        //public IActionResult ChangeTheme()
        //{
        //    using (var con = _context.CreateConnection())
        //    {
        //        string Mode = con.GetList<ConfigModel>().Where(w=>w.key == "DARKMODE").FirstOrDefault().value;

        //        if (Mode != null)
        //        {
        //            if(Mode == "1")
        //            {
        //                var config = new ConfigModel { key = "DARKMODE", value = "0" };
        //                con.UpdateAsync(config);
        //                return Redirect(Request.Headers["Referer"].ToString());
        //            }
        //            else
        //            {
        //                var config = new ConfigModel {  key = "DARKMODE", value = "1" };
        //                con.UpdateAsync(config);
        //                return Redirect(Request.Headers["Referer"].ToString());
        //            }
                    
        //        }
        //        else
        //        {
        //            return Redirect(Request.Headers["Referer"].ToString());
        //        }

        //    }
         
        //}

    }
}
