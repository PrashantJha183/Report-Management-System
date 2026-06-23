using Report.Models;
using Report.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Report.Controllers
{
    public class ReportController : Controller
    {
        private readonly ReportService _reportService;

        public ReportController()
        {
            _reportService = new ReportService();
        }

        //[HttpGet]
        //public ActionResult Index(ReportGridViewModel model)
        //{
        //    var result = _reportService.GetReports(model);
        //    return View(result);
        //}

        [HttpGet]
        public async Task<ActionResult> Index(ReportGridViewModel model)
        {
            var result = await _reportService.GetReportsAsync(model);

            // Load link items for dropdown
            var dt = await _reportService.GetLinkItemsAsync();
            var items = new List<SelectListItem>();
            foreach (DataRow row in dt.Rows)
            {
                items.Add(new SelectListItem
                {
                    Value = row["LinkItemId"].ToString(),
                    Text = row["LinkItemName"].ToString()
                });
            }
            ViewBag.LinkItems = items;
            ViewBag.NextIdentity = await _reportService.GetNextIdentityAsync();
            ViewBag.TableName = ReportConfig.ReportTable;
            ViewBag.AuditTableName = ReportConfig.ReportTable;
            ViewBag.RecordIdLabel = "Report ID";
            ViewBag.Colspan = 4;

            return View(result);
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //[HttpGet]
        //public ActionResult Index(ReportGridViewModel model)
        //{
        //    var result = _reportService.GetReports(model);

        //    // Load link items for dropdown
        //    var dt = _reportService.GetLinkItems();
        //    var items = new List<SelectListItem>();
        //    foreach (DataRow row in dt.Rows)
        //    {
        //        items.Add(new SelectListItem
        //        {
        //            Value = row["LinkItemId"].ToString(),
        //            Text = row["LinkItemName"].ToString()
        //        });
        //    }
        //    ViewBag.LinkItems = items;
        //    ViewBag.NextIdentity = _reportService.GetNextIdentity();

        //    return View(result);
        //}
        #endregion

        [HttpPost]
        public async Task<JsonResult> SaveChanges(SaveChangesRequest request)
        {
            try
            {
                var result = await _reportService.SaveChangesAsync(request.Changes, request.Mode);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new SaveChangesResult { Status = "ERROR:" + ex.Message });
            }
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //[HttpPost]
        //public JsonResult SaveChanges(SaveChangesRequest request)
        //{
        //    try
        //    {
        //        var result = _reportService.SaveChanges(request.Changes, request.Mode);
        //        return Json(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new SaveChangesResult { Status = "ERROR:" + ex.Message });
        //    }
        //}
        #endregion

        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            var status = await _reportService.DeleteReportAsync(id);
            if (status == "DELETED")
                return Json(new { Status = "DELETED" });
            return Json(new { Status = status });
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //[HttpPost]
        //public JsonResult Delete(int id)
        //{
        //    var status = _reportService.DeleteReport(id);
        //    if (status == "DELETED")
        //        return Json(new { Status = "DELETED" });
        //    return Json(new { Status = status });
        //}
        #endregion

    }
}