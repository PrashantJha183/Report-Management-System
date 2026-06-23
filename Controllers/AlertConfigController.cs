using Report.Models;
using Report.Services;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Report.Controllers
{
    public class AlertConfigController : Controller
    {
        private readonly AlertConfigService _alertConfigService;

        public AlertConfigController()
        {
            _alertConfigService = new AlertConfigService();
        }

        [HttpGet]
        public async Task<ActionResult> Index(AlertConfigGridViewModel model)
        {
            var result = await _alertConfigService.GetAlertConfigsAsync(model);
            ViewBag.NextIdentity = await _alertConfigService.GetNextIdentityAsync();
            ViewBag.TableName = ReportConfig.AlertConfigTable;
            ViewBag.AuditTableName = ReportConfig.AlertConfigTable;
            ViewBag.RecordIdLabel = "Alert Config ID";
            ViewBag.Colspan = 8;
            return View(result);
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //public ActionResult Index(AlertConfigGridViewModel model)
        //{
        //    var result = _alertConfigService.GetAlertConfigs(model);
        //    ViewBag.NextIdentity = _alertConfigService.GetNextIdentity();
        //    return View(result);
        //}
        #endregion

        [HttpPost]
        public async Task<JsonResult> SaveChanges(SaveAlertConfigChangesRequest request)
        {
            try
            {
                var result = await _alertConfigService.SaveChangesAsync(request.Changes, request.Mode);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new SaveAlertConfigChangesResult { Status = "ERROR:" + ex.Message });
            }
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //[HttpPost]
        //public JsonResult SaveChanges(SaveAlertConfigChangesRequest request)
        //{
        //    try
        //    {
        //        var result = _alertConfigService.SaveChanges(request.Changes, request.Mode);
        //        return Json(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new SaveAlertConfigChangesResult { Status = "ERROR:" + ex.Message });
        //    }
        //}
        #endregion

        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            var status = await _alertConfigService.DeleteAlertConfigAsync(id);
            if (status == "DELETED")
                return Json(new { Status = "DELETED" });
            return Json(new { Status = status });
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //[HttpPost]
        //public JsonResult Delete(int id)
        //{
        //    var status = _alertConfigService.DeleteAlertConfig(id);
        //    if (status == "DELETED")
        //        return Json(new { Status = "DELETED" });
        //    return Json(new { Status = status });
        //}
        #endregion
    }
}