using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SJPCORE.Controllers
{
    [Route("[controller]")]
    public class RTCMCUController : Controller
    {
        private readonly ILogger<RTCMCUController> _logger;

        public RTCMCUController(ILogger<RTCMCUController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            Console.WriteLine("RTC MCU Controller");
            var site = GlobalParameter.Config.Where(x => x.key == "SITE_ID").FirstOrDefault();
            return View("rtc-mcu", site);
        }

        [HttpGet("error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
