using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace SJPCORE.Models.Attribute
{
    public class InternalOnlyAttribute : TypeFilterAttribute  
    {
        public InternalOnlyAttribute() : base(typeof(InternalOnlyFilter))
        {
        }
    }

    

    public static class InternalRequestFlag
    {
        public const string INTERNAL_REQUEST_KEY = "X-Internal-Request";
        public const string INTERNAL_REQUEST_VALUE = "SJPCORE_INTERNAL_asjdoijmhotry9cjkzxgvirslujgvujipsedju89wiejfvusd90ok0uow9033289htgbjhdshk";
    }

    public class InternalOnlyFilter : IAuthorizationFilter
    {
        private static readonly string ProcessKey;

        static InternalOnlyFilter()
        {
            // สร้าง unique key สำหรับ process นี้
            ProcessKey = $"SJPCORE_{Process.GetCurrentProcess().Id}_{DateTime.UtcNow.Ticks}";
            Environment.SetEnvironmentVariable("SJPCORE_PROCESS", ProcessKey);
        }
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            try 
            {
                var processEnvKey = Environment.GetEnvironmentVariable("SJPCORE_PROCESS");
                var internalFlag = context.HttpContext.Request.Headers[InternalRequestFlag.INTERNAL_REQUEST_KEY].FirstOrDefault();
                Console.WriteLine($"Current: {ProcessKey}, Env: {processEnvKey}, Header: {internalFlag}");

                if (processEnvKey == ProcessKey && internalFlag == InternalRequestFlag.INTERNAL_REQUEST_VALUE)
                {
                    // ถ้า key ตรงกัน = รันอยู่ใน process เดียวกัน
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking process: {ex.Message}");
            }
            
            context.Result = new UnauthorizedResult();
            return;
        }
    }
}