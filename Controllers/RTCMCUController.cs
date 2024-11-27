using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SJPCORE.Models;

namespace SJPCORE.Controllers
{
    [Route("[controller]")]
    public class RTCMCUController : Controller
    {
        private readonly ILogger<RTCMCUController> _logger;
        private readonly DapperContext _context;    
        public RTCMCUController(ILogger<RTCMCUController> logger, DapperContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            Console.WriteLine("RTC MCU Controller");
            using (var con = _context.CreateConnection())
            {
                var site = con.Get<ConfigModel>("SITE_ID");
                return View("rtc-mcu", site);
            }
        }

        [HttpGet("error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
