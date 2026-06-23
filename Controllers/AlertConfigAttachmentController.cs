using Report.Models;
using Report.Services;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Report.Controllers
{
    public class AlertConfigAttachmentController : Controller
    {
        private readonly AlertConfigAttachmentService _alertConfigAttachmentService;

        public AlertConfigAttachmentController()
        {
            _alertConfigAttachmentService = new AlertConfigAttachmentService();
        }

        [HttpGet]
        public async Task<ActionResult> Index(AlertConfigAttachmentGridViewModel model)
        {
            var result = await _alertConfigAttachmentService.GetAlertConfigAttachmentsAsync(model);
            ViewBag.TableName = ReportConfig.AlertConfigAttachmentTable;
            ViewBag.AuditTableName = ReportConfig.AlertConfigTable;
            ViewBag.RecordIdLabel = "Alert Config ID";
            ViewBag.Colspan = 5;
            return View(result);
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //[HttpGet]
        //public ActionResult Index(AlertConfigAttachmentGridViewModel model)
        //{
        //    var result = _alertConfigAttachmentService.GetAlertConfigAttachments(model);
        //    return View(result);
        //}
        #endregion

        [HttpPost]
        public async Task<JsonResult> SaveChanges(SaveAlertConfigAttachmentChangesRequest request)
        {
            try
            {
                var result = await _alertConfigAttachmentService.SaveChangesAsync(request.Changes, request.Mode);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new SaveAlertConfigAttachmentChangesResult { Status = "ERROR:" + ex.Message });
            }
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //[HttpPost]
        //public JsonResult SaveChanges(SaveAlertConfigAttachmentChangesRequest request)
        //{
        //    try
        //    {
        //        var result = _alertConfigAttachmentService.SaveChanges(request.Changes, request.Mode);
        //        return Json(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new SaveAlertConfigAttachmentChangesResult { Status = "ERROR:" + ex.Message });
        //    }
        //}
        #endregion

        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            var status = await _alertConfigAttachmentService.DeleteAlertConfigAttachmentAsync(id);
            if (status == "DELETED")
                return Json(new { Status = "DELETED" });
            return Json(new { Status = status });
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //[HttpPost]
        //public JsonResult Delete(int id)
        //{
        //    var status = _alertConfigAttachmentService.DeleteAlertConfigAttachment(id);
        //    if (status == "DELETED")
        //        return Json(new { Status = "DELETED" });
        //    return Json(new { Status = status });
        //}
        #endregion
    }
}