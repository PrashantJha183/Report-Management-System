using Report.Models;
using Report.Services;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Report.Controllers
{
    public class AlertScheduleController : Controller
    {
        private readonly AlertScheduleService _alertScheduleService;

        public AlertScheduleController()
        {
            _alertScheduleService = new AlertScheduleService();
        }

        [HttpGet]
        public async Task<ActionResult> Index(AlertScheduleGridViewModel model)
        {
            var result = await _alertScheduleService.GetAlertSchedulesAsync(model);
            ViewBag.NextIdentity = await _alertScheduleService.GetNextIdentityAsync();
            ViewBag.TableName = ReportConfig.AlertScheduleTable;
            ViewBag.AuditTableName = ReportConfig.AlertConfigTable;
            ViewBag.RecordIdLabel = "Alert Config ID";
            ViewBag.Colspan = 8;
            return View(result);
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //[HttpGet]
        //public ActionResult Index(AlertScheduleGridViewModel model)
        //{
        //    var result = _alertScheduleService.GetAlertSchedules(model);
        //    ViewBag.NextIdentity = _alertScheduleService.GetNextIdentity();
        //    return View(result);
        //}
        #endregion

        [HttpPost]
        public async Task<JsonResult> SaveChanges(SaveAlertScheduleChangesRequest request)
        {
            try
            {
                var result = await _alertScheduleService.SaveChangesAsync(request.Changes, request.Mode);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new SaveAlertScheduleChangesResult { Status = "ERROR:" + ex.Message });
            }
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //[HttpPost]
        //public JsonResult SaveChanges(SaveAlertScheduleChangesRequest request)
        //{
        //    try
        //    {
        //        var result = _alertScheduleService.SaveChanges(request.Changes, request.Mode);
        //        return Json(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new SaveAlertScheduleChangesResult { Status = "ERROR:" + ex.Message });
        //    }
        //}
        #endregion

        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            var status = await _alertScheduleService.DeleteAlertScheduleAsync(id);
            if (status == "DELETED")
                return Json(new { Status = "DELETED" });
            return Json(new { Status = status });
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //[HttpPost]
        //public JsonResult Delete(int id)
        //{
        //    var status = _alertScheduleService.DeleteAlertSchedule(id);
        //    if (status == "DELETED")
        //        return Json(new { Status = "DELETED" });
        //    return Json(new { Status = status });
        //}
        #endregion
    }
}
