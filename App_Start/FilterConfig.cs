using System.Web;
using System.Web.Mvc;
using Report.Services;

namespace Report
{
    public class LoginAuthorizeAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            string controller = filterContext.RouteData.Values["controller"]?.ToString();
            string action = filterContext.RouteData.Values["action"]?.ToString();

            if (controller == "Home")
                return;

            if (filterContext.HttpContext.Session["IsLoggedIn"] == null ||
                !(bool)filterContext.HttpContext.Session["IsLoggedIn"])
            {
                filterContext.Result = new RedirectResult("~/Home/Index");
                return;
            }

            var role = filterContext.HttpContext.Session["Role"]?.ToString();
            if (string.IsNullOrEmpty(role)) return;

            var roleService = new RoleService();
            if (!roleService.HasPermission(role, controller, action))
            {
                filterContext.Result = new RedirectResult("~/Home/Index");
            }
        }
    }

    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
            filters.Add(new LoginAuthorizeAttribute());
        }
    }
}
