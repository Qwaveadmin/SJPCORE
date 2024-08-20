
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
            if (!context.HttpContext.Request.Cookies.ContainsKey("Authorization"))
            {
                context.Result = new RedirectToActionResult("SignIn", "Account", null);
            }

            base.OnActionExecuting(context);
        }
    }
}
