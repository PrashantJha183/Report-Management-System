using Report.Models;
using Report.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Report.Controllers
{
    public class ReportFilteringColumnController : Controller
    {
        private readonly ReportFilteringColumnService _reportFilteringColumnService;

        public ReportFilteringColumnController()
        {
            _reportFilteringColumnService = new ReportFilteringColumnService();
        }

        [HttpGet]
        public async Task<ActionResult> Index(Models.ReportFilteringColumnGridViewModel model)
        {
            var result = await _reportFilteringColumnService.GetReportFilteringColumnsAsync(model);

            var datatypes = new List<SelectListItem>
    {
        new SelectListItem { Value = "string", Text = "string" },
        new SelectListItem { Value = "int", Text = "int" },
        new SelectListItem { Value = "datetime", Text = "datetime" },
        new SelectListItem { Value = "decimal", Text = "decimal" }
    };
            ViewBag.DatatypeOptions = datatypes;
            ViewBag.NextIdentity = await _reportFilteringColumnService.GetNextIdentityAsync();
            ViewBag.TableName = ReportConfig.ReportFilteringColumnTable;
            ViewBag.AuditTableName = ReportConfig.ReportFilteringColumnTable;
            ViewBag.RecordIdLabel = "Report Filtering Column ID";
            ViewBag.Colspan = 10;

            return View(result);
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //[HttpGet]
        //public ActionResult Index(Models.ReportFilteringColumnGridViewModel model)
        //{
        //    var result = _reportFilteringColumnService.GetReportFilteringColumns(model);
        //    var datatypes = new List<SelectListItem>
        //    {
        //        new SelectListItem { Value = "string", Text = "string" },
        //        new SelectListItem { Value = "int", Text = "int" },
        //        new SelectListItem { Value = "datetime", Text = "datetime" },
        //        new SelectListItem { Value = "decimal", Text = "decimal" }
        //    };
        //    ViewBag.DatatypeOptions = datatypes;
        //    ViewBag.NextIdentity = _reportFilteringColumnService.GetNextIdentity();
        //    return View(result);
        //}
        #endregion

        [HttpPost]
        public async Task<JsonResult> SaveChanges(SaveFilteringColumnChangesRequest request)
        {
            try
            {
                var result = await _reportFilteringColumnService.SaveChangesAsync(request.Changes, request.Mode);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new SaveFilteringColumnChangesResult { Status = "ERROR:" + ex.Message });
            }
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //[HttpPost]
        //public JsonResult SaveChanges(SaveFilteringColumnChangesRequest request)
        //{
        //    try
        //    {
        //        var result = _reportFilteringColumnService.SaveChanges(request.Changes, request.Mode);
        //        return Json(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new SaveFilteringColumnChangesResult { Status = "ERROR:" + ex.Message });
        //    }
        //}
        #endregion

        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            var status = await _reportFilteringColumnService.DeleteReportFilteringColumnAsync(id);
            if (status == "DELETED")
                return Json(new { Status = "DELETED" });
            return Json(new { Status = status });
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //[HttpPost]
        //public JsonResult Delete(int id)
        //{
        //    var status = _reportFilteringColumnService.DeleteReportFilteringColumn(id);
        //    if (status == "DELETED")
        //        return Json(new { Status = "DELETED" });
        //    return Json(new { Status = status });
        //}
        #endregion
    }
}
