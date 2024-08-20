using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SJPCORE.Models;
using System.Net;
using System.Threading.Tasks;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using System;
using static System.Net.Mime.MediaTypeNames;

namespace SJPCORE.Middleware
{
    public class PassParam : IMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly DapperContext _context;        
        public PassParam(DapperContext context)
        {
            _context = context;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
                await next(context);
        }
    }
}
