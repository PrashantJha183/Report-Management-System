using System.Web;
using System.Web.Mvc;

namespace Report
{
    public class LoginAuthorizeAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            string controller = filterContext.RouteData.Values["controller"]?.ToString();
            if (controller == "Home")
                return;

            if (filterContext.HttpContext.Session["IsLoggedIn"] == null ||
                !(bool)filterContext.HttpContext.Session["IsLoggedIn"])
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
