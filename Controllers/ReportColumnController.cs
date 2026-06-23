using Report.Models;
using Report.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Report.Controllers
{
    public class ReportColumnController : Controller
    {
        private readonly ReportColumnService _reportColumnService;

        public ReportColumnController()
        {
            _reportColumnService = new ReportColumnService();
        }

        [HttpGet]
        public async Task<ActionResult> Index(Models.ReportColumnGridViewModel model)
        {
            var result = await _reportColumnService.GetReportColumnsAsync(model);

            var datatypes = new List<SelectListItem>
{
    new SelectListItem { Value = "string", Text = "string" },
    new SelectListItem { Value = "int", Text = "int" },
    new SelectListItem { Value = "datetime", Text = "datetime" },
    new SelectListItem { Value = "decimal", Text = "decimal" }
};
            ViewBag.DatatypeOptions = datatypes;
            ViewBag.NextIdentity = await _reportColumnService.GetNextIdentityAsync();
            ViewBag.TableName = ReportConfig.ReportColumnTable;
            ViewBag.AuditTableName = ReportConfig.ReportColumnTable;
            ViewBag.RecordIdLabel = "Report Column ID";
            ViewBag.Colspan = 5;

            return View(result);
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //[HttpGet]
        //public ActionResult Index(Models.ReportColumnGridViewModel model)
        //{
        //    var result = _reportColumnService.GetReportColumns(model);
        //    var datatypes = new List<SelectListItem>
        //{
        //    new SelectListItem { Value = "string", Text = "string" },
        //    new SelectListItem { Value = "int", Text = "int" },
        //    new SelectListItem { Value = "datetime", Text = "datetime" },
        //    new SelectListItem { Value = "decimal", Text = "decimal" }
        //};
        //    ViewBag.DatatypeOptions = datatypes;
        //    ViewBag.NextIdentity = _reportColumnService.GetNextIdentity();
        //    return View(result);
        //}
        #endregion

        [HttpPost]
        public async Task<JsonResult> SaveChanges(SaveColumnChangesRequest request)
        {
            try
            {
                var result = await _reportColumnService.SaveChangesAsync(request.Changes, request.Mode);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new SaveColumnChangesResult { Status = "ERROR:" + ex.Message });
            }
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //[HttpPost]
        //public JsonResult SaveChanges(SaveColumnChangesRequest request)
        //{
        //    try
        //    {
        //        var result = _reportColumnService.SaveChanges(request.Changes, request.Mode);
        //        return Json(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new SaveColumnChangesResult { Status = "ERROR:" + ex.Message });
        //    }
        //}
        #endregion

        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            var result = await _reportColumnService.DeleteReportColumnAsync(id);
            return Json(new { Status = result });
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //[HttpPost]
        //public JsonResult Delete(int id)
        //{
        //    var result = _reportColumnService.DeleteReportColumn(id);
        //    return Json(new { Status = result });
        //}
        #endregion
    }
}
