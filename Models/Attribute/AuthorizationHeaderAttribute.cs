
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SJPCORE.Models.Attribute
{

    public class AuthorizationHeaderAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.Request.Path.Value.Contains("soundfile"))
            {
                base.OnActionExecuting(context);
                return;
            }
            // if (!context.HttpContext.Request.Cookies.ContainsKey("Authorization"))
            // {
            //     context.Result = new RedirectToActionResult("SignIn", "Account", null);
            // }
            //Check Configuration
            //Site ID
            //Host URL
            //EMQX
            //MQTT
            var config = new DapperContext().GetConfig();
            GlobalParameter.Config = config;
            var siteId = config.Find(x => x.key == "SITE_ID");
            var hostUrl = config.Find(x => x.key == "HOST_URL");
            var emqxIp = config.Find(x => x.key == "EMQX_IP");
            var emqxPort = config.Find(x => x.key == "EMQX_PORT");
            var emqxUser = config.Find(x => x.key == "EMQX_USER");
            var emqxPass = config.Find(x => x.key == "EMQX_PASS");
            var mqttPort = config.Find(x => x.key == "CONFIG_MQTT_PORT");
            var mqttUser = config.Find(x => x.key == "CONFIG_MQTT_USER");
            var mqttPass = config.Find(x => x.key == "CONFIG_MQTT_PASS");
            if (string.IsNullOrEmpty(siteId.value) || string.IsNullOrEmpty(hostUrl.value) || string.IsNullOrEmpty(emqxIp.value) || string.IsNullOrEmpty(emqxPort.value) || string.IsNullOrEmpty(emqxUser.value) || string.IsNullOrEmpty(emqxPass.value) || string.IsNullOrEmpty(mqttPort.value) || string.IsNullOrEmpty(mqttUser.value) || string.IsNullOrEmpty(mqttPass.value))
            {
                context.Result = new RedirectToActionResult("Config", "Account", null);
            }
            base.OnActionExecuting(context);
        }
    }
}
