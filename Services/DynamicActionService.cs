//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Report.Services
//{
//    public static class DynamicActionService
//    {
//        // DynamicCode controller actions that must NOT be intercepted (real MVC pages / AJAX endpoints)
//        private static readonly HashSet<string> ReservedActions =
//            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
//            {
//                "Index", "IDE", "GetCode", "SaveCode", "SaveChanges", "Delete", "LoadClassesFromNamespaces"
//            };

//        // ── Decide whether the incoming URL should be rewritten to the dynamic call back ──
//        public static bool IsDynamicActionPresent(string url)
//        {
//            if (string.IsNullOrWhiteSpace(url)) return false;
//            if (url.IndexOf('.') > url.LastIndexOf('/')) return false; // has a file extension (css/js/images)

//            var (controller, action) = IDEHelperService.ExtractControllerAction(url);
//            if (string.IsNullOrWhiteSpace(controller)) return false;
//            if (string.Equals(controller, "DynamicCallBack", StringComparison.OrdinalIgnoreCase)) return false;

//            // Protect the DynamicCode IDE pages / AJAX endpoints (real MVC pages)
//            if (string.Equals(controller, "DynamicCode", StringComparison.OrdinalIgnoreCase)
//                && ReservedActions.Contains(string.IsNullOrWhiteSpace(action) ? "Index" : action))
//                return false;

//            string effectiveAction = string.IsNullOrWhiteSpace(action) ? "Index" : action;

//            return DynamicRouteCache.Contains(controller, effectiveAction);
//        }

//        // ── Load saved code, compile, execute, return the model produced by the code ── 
//        public static async Task<object> ExecuteCallBack(string url)
//        {
//            var (controller, action) = IDEHelperService.ExtractControllerAction(url);
//            if (controller == null) return null;

//            string effectiveAction = string.IsNullOrWhiteSpace(action) ? "Index" : action;
//            string keyName = controller + ":" + effectiveAction;

//            var service = new DynamicCodeService();
//            var model = await service.GetByControllerActionAsync(controller, effectiveAction);
//            if (model == null)
//            {
//                // Execution logging disabled (execution runs in Sumeet/ERP).
//                //StaticLogger.Log($"ExecuteCallBack: no record for {keyName}");
//                return null;
//            }

//            var namespaceList = IDEHelperService.GetNamespaces(model.Namespaces);
//            string namespaceString = IDEHelperService.PrepareUsingNamespace(namespaceList);
//            var privateAssemblies = IDEHelperService.GetDllNamesFromNamespaces(namespaceList);
//            var gacAssemblies = new List<string> { "System.Web.dll", "Microsoft.CSharp.dll" };

//            var executor = new DynamicCodeExecutor();
//            var result = executor.ExecuteCode(
//                keyName,
//                model.Code,
//                new object[0],
//                gacAssemblies,
//                privateAssemblies,
//                namespaceString);

//            if (!result.Success)
//            {
//                // Execution logging disabled (execution runs in Sumeet/ERP).
//                //StaticLogger.Log($"ExecuteCallBack FAILED {keyName}: {string.Join("; ", result.Errors)}");
//                return new DynamicRuntimeError { Errors = result.Errors };
//            }

//            return result.ReturnVal;
//        }
//    }

//    public class DynamicRuntimeError
//    {
//        public List<string> Errors { get; set; } = new List<string>();
//    }
//}