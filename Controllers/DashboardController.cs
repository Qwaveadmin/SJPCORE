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
using SJPCORE.Util;
using SJPCORE.Models.Attribute;

namespace SJPCORE.Controllers
{
    [AuthorizationHeader]
    public class DashBoardController : Controller
    {
        private readonly ILogger<DashBoardController> _logger;
        private readonly DapperContext _context;

       

        public DashBoardController(ILogger<DashBoardController> logger, DapperContext context)
        {
            _logger = logger;
            _context = context;
        }
        [ActionName("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var dic = new Dictionary<string, string>();

            using (var con = _context.CreateConnection())
            {
                dic.Add("sound_cnt",(await con.GetListAsync<StorageModel>()).Count().ToString());
                dic.Add("yt_cnt", (await con.GetListAsync<YtModel>()).Count().ToString());
                dic.Add("fm_cnt", (await con.GetListAsync<RadioModel>()).Count().ToString());
            }

            return View(dic);
        }
      



    }
}
