//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Web.Mvc;
//using Report.Services;

//namespace Report.Controllers
//{
//    public class DynamicCallBackController : Controller
//    {
//        // GET: DynamicCallBack/Execute?Url=...
//        public async Task<ActionResult> Execute(string Url)
//        {
//            try
//            {
//                var (controller, action) = IDEHelperService.ExtractControllerAction(Url);
//                if (string.IsNullOrWhiteSpace(controller))
//                    return HttpNotFound();

//                object payload = await DynamicActionService.ExecuteCallBack(Url);

//                if (payload is DynamicRuntimeError runtimeError)
//                {
//                    ViewBag.RuntimeErrors = runtimeError.Errors;
//                    return View("_RuntimeError");
//                }

//                if (payload == null)
//                    return HttpNotFound();

//                if (payload is ActionResult actionResult)
//                    return actionResult;

//                string effectiveAction = string.IsNullOrWhiteSpace(action) ? "Index" : action;
//                return View("~/Views/" + controller + "/" + effectiveAction + ".cshtml", payload);
//            }
//            catch (Exception ex)
//            {
//                // Execution logging disabled (execution runs in Sumeet/ERP).
//                //StaticLogger.LogError(ex, "DynamicCallBack.Execute");
//                ViewBag.RuntimeErrors = new List<string> { ex.Message };
//                return View("_RuntimeError");
//            }
//        }
//    }
//}