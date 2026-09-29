using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Report.Models;
using Report.Services;


using RSuite.Infrastructure.Core.DynamicExecution;

namespace Report.Controllers
{
    public class DynamicCodeController : Controller
    {
        private readonly DynamicCodeService _service;

        public DynamicCodeController()
        {
            _service = new DynamicCodeService();
        }

        // ── Grid Listing ──

        [HttpGet]
        public async Task<ActionResult> Index(DynamicCodeGridViewModel model)
        {
            if (model == null)
                model = new DynamicCodeGridViewModel();

            var result = await _service.GetAllAsync(model);

            ViewBag.NextIdentity = await _service.GetNextIdentityAsync();
            ViewBag.TableName = ReportConfig.DynamicCodeTable;
            ViewBag.AuditTableName = ReportConfig.DynamicCodeTable;
            ViewBag.RecordIdLabel = "Code ID";
            ViewBag.Colspan = 5;

            return View(result);
        }

        // ── Grid Save ──

        [HttpPost]
        public async Task<JsonResult> SaveChanges(SaveDynamicCodeChangesRequest request)
        {
            try
            {
                var result = await _service.SaveChangesAsync(request.Changes, request.Mode);
                // Execution now runs in Sumeet (ERP); route cache is not maintained here.
                //if (result.Status == "OK")
                //    await DynamicRouteCache.RefreshAsync();
                return Json(result);
            }
            catch (Exception ex)
            {
                StaticLogger.LogError(ex, "DynamicCode.SaveChanges");
                return Json(new SaveDynamicCodeChangesResult
                {
                    Status = "ERROR:" + ex.Message
                });
            }
        }

        // ── Grid Delete ──

        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            try
            {
                var status = await _service.DeleteAsync(id);
                StaticLogger.Log($"Delete: id={id}, status={status}");
                // Execution now runs in Sumeet (ERP); route cache is not maintained here.
                //if (status == "DELETED")
                //    await DynamicRouteCache.RefreshAsync();
                return Json(new { Status = status });
            }
            catch (Exception ex)
            {
                StaticLogger.LogError(ex, "DynamicCode.Delete");
                return Json(new { Status = "ERROR:" + ex.Message });
            }
        }

        // ── IDE Page (standalone fallback) ──

        [HttpGet]
        public async Task<ActionResult> IDE(int? id)
        {
            IDEViewModel model;

            if (id.HasValue && id.Value > 0)
            {
                model = await _service.GetCodeByIdAsync(id.Value);
                if (model == null)
                    model = new IDEViewModel();
            }
            else
            {
                model = new IDEViewModel();
            }

            return View(model);
        }

        // ── Get code by ID (JSON for inline IDE) ──

        [HttpGet]
        public async Task<JsonResult> GetCode(int id)
        {
            var model = await _service.GetCodeByIdAsync(id);
            if (model == null)
                return Json(new { }, JsonRequestBehavior.AllowGet);

            return Json(new
            {
                codeID = model.CodeID,
                controllerName = model.ControllerName ?? "",
                actionName = model.ActionName ?? "",
                description = model.Description ?? "",
                namespaces = model.Namespaces ?? "",
                code = model.Code ?? ""
            }, JsonRequestBehavior.AllowGet);
        }

        // ── IDE Save (compile + save) ──

        [HttpPost]
        public async Task<JsonResult> SaveCode(IDEViewModel model)
        {
            try
            {
                if (model == null)
                    return Json(new { success = false, errors = new List<string> { "No data received." } });

                StaticLogger.Log($"SaveCode: CodeID={model.CodeID}, ControllerName={model.ControllerName}, ActionName={model.ActionName}");

                // Build namespace string
                var namespaceList = IDEHelperService.GetNamespaces(model.Namespaces);
                // RSuite's compiler prepends its own header that already includes this using,
                // so dropping it from our string avoids a harmless duplicate-using warning.
                var saveNamespaceList = namespaceList
                    .Where(ns => !string.Equals(ns, "System.Collections.Generic", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                string namespaceString = IDEHelperService.PrepareUsingNamespace(saveNamespaceList);

                // Namespaces that exist only in Report's own app cannot be resolved by the runtime
                // (Sumeet) process, so they must not be persisted/executed there.
                var runtimeNamespaceList = namespaceList
                    .Where(ns => !ns.StartsWith("Report.", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // ── OLD (Report's own compiler) — kept for reference, NOT used ──
                // Resolve DLL references from namespaces
                //var gacAssemblies = new List<string>();
                //var privateAssemblies = IDEHelperService.GetDllNamesFromNamespaces(namespaceList);

                // Compile
                //var executor = new DynamicCodeExecutor();
                //var compileResult = executor.CompileCode(
                //    model.ControllerName,
                //    model.Code,
                //    gacAssemblies,
                //    privateAssemblies,
                //    namespaceString
                //);

                //if (!compileResult.Success)
                //{
                //    StaticLogger.Log($"SaveCode compile failed: {string.Join("; ", compileResult.Errors)}");
                //    return Json(new { success = false, errors = compileResult.Errors });
                //}

                // ── NEW (ERP engine — mirrors RSuiteTools CodeBuilderController.Save) ──
                var InputParamList = new List<InputParam>();
                var gacList = new List<string> { "System.Web.dll", "Microsoft.CSharp.dll" };
                var privateAsmList = IDEHelperService.GetDllNamesFromNamespaces(namespaceList);

                var executor = new RSuite.Infrastructure.Core.DynamicExecution.DynamicCodeExecutor();
                AssemblyManager.RemoveAsseblyModel(model.ControllerName);
                var rm = executor.CompileCode(
                    model.ControllerName,
                    model.Code,
                    InputParamList,
                    gacList,
                    privateAsmList,
                    namespaceString
                );

                // Save to DB (persist only namespaces that exist in the runtime app, so Sumeet compiles).
                model.Namespaces = string.Join(", ", runtimeNamespaceList);
                int savedId = await _service.SaveCodeFromIDEAsync(model);

                StaticLogger.Log($"SaveCode saved: id={savedId}");
                // Execution now runs in Sumeet (ERP); route cache is not maintained here.
                //if (savedId > 0)
                //    await DynamicRouteCache.RefreshAsync();

                return Json(new { success = true, id = savedId });
            }
            catch (Exception ex)
            {
                StaticLogger.LogError(ex, "DynamicCode.SaveCode");
                return Json(new { success = false, errors = new List<string> { ex.Message } });
            }
        }

        // ── Load class names for keyword autocomplete ──

        [HttpPost]
        public JsonResult LoadClassesFromNamespaces(string Namespaces)
        {
            StaticLogger.Log($"LoadClassesFromNamespaces: Namespaces={Namespaces}");
            var namespaceList = IDEHelperService.GetNamespaces(Namespaces);
            var classList = IntellisenseController.LoadAssemblyFromNamespaces(namespaceList);

            StaticLogger.Log($"LoadClassesFromNamespaces: found {classList.Count} classes");
            return Json(classList);
        }
    }
}
