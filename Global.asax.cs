using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Report.Services;

namespace Report
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            ControllerBuilder.Current.DefaultNamespaces.Add("Report.Controllers");
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        } 

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            // ═══ Dynamic code EXECUTION is now handled by the Sumeet (ERP) project. ═══
            // Execution wiring below is intentionally disabled (commented out) and kept
            // only for reference. Report remains the authoring tool (save + compile).
            //
            //try
            //{
            //    if (HttpContext.Current?.Request?.Url == null) return;
            //    if (HttpContext.Current.Request.Url.IsFile) return;
            //
            //    string url = HttpContext.Current.Request.Url.ToString();
            //    if (DynamicActionService.IsDynamicActionPresent(url))
            //    {
            //        StaticLogger.Log($"BeginRequest rewrite: {url}");
            //        HttpContext.Current.RewritePath("~/DynamicCallBack/Execute?Url=" + HttpContext.Current.Server.UrlEncode(url));
            //    }
            //}
            //catch (Exception ex)
            //{
            //    StaticLogger.LogError(ex, "BeginRequest.Rewrite");
            //}
        }
    }
}
