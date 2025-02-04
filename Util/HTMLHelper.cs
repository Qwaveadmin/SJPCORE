using Microsoft.AspNetCore.Http;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace SJPCORE.Util
{
    public static class HTMLHelper
    {
        public static string IsDarkMode()
        {
            if (int.Parse(@GlobalParameter.Config.Where(w => w.key == "DARKMODE").FirstOrDefault().value) == 1)
            {
                return "dark";
            }
            else
            {
                return "light";
            }
        }

        public static bool IsAdmin(HttpContext context)
        {
            var authorizationService = context.RequestServices.GetRequiredService<IAuthorizationService>();

            if (authorizationService.AuthorizeAsync(context.User, "RequireAdmin").Result.Succeeded)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public static string GetRole(HttpContext context)
        {
            // ดึงจาก Claims Identity
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                // ดึง IAuthorizationService จาก DI
                var authorizationService = context.RequestServices.GetRequiredService<IAuthorizationService>();
                
                // ใช้ .Result แทน await
                if (authorizationService.AuthorizeAsync(context.User, "RequireAdmin").Result.Succeeded)
                {
                    return "Admin";
                }
                else
                {
                    return "User"; 
                }
            }
            return "Guest";
        }

        public static string GetUsername(HttpContext context)
        {
            // ดึงจาก Claims Identity
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                return context.User.Identity.Name;
            }
            
            // หรือดึงจาก Cookie โดยตรง
            if (context.Request.Cookies.TryGetValue("U", out string username))
            {
                return username;
            }

            return "Guest";
        }
    }
}
