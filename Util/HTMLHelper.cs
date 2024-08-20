using Microsoft.AspNetCore.Http;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Linq;

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
            if (context.Request.Cookies.ContainsKey("Authorization"))
            {
                string value = context.Request.Cookies["Authorization"];


                if(value == "sutha")
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
            else
            {
                return false;
            }

        }

        public static string GetRole(HttpContext context)
        {
            return IsAdmin(context) ? "ผู้ดูแลระบบ" : "ผู้ใช้งาน";
        }

        public static string GetUsername(HttpContext context)
        {
            if (context.Request.Cookies.ContainsKey("U"))
            {
                string value = context.Request.Cookies["U"];

                return value;

            }
            else
            {
                return "error";
            }
        }
    }
}
