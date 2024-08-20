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

namespace SJPCORE.Controllers
{
    [AuthorizationHeader]
    public class StatusController : Controller
    {
        private readonly ILogger<DashBoardController> _logger;
        private readonly DapperContext _context;

        public StatusController(ILogger<DashBoardController> logger, DapperContext context)
        {
            _logger = logger;
            _context = context;
        }
        public IActionResult Server()
        {
            return View("status-server");
        }



    }
}
