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
    public class AuthPassed : IMiddleware
    {
        private readonly RequestDelegate _next;
        private const string CustomHeaderName = "X-Custom-Auth";
        private const string CustomHeaderValue = "25c1e9bf-5136-4d61-ac93-651723bdf291"; // กำหนดค่า Secret ที่ต้องการ
        public AuthPassed()
        {
            
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {

            if (context.Request.Path.StartsWithSegments("/rtcmcu", StringComparison.OrdinalIgnoreCase))
            {
                if (!context.Request.Headers.TryGetValue(CustomHeaderName, out var headerValue) || headerValue != CustomHeaderValue)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }

            await next(context);
            
        }
    }
}
