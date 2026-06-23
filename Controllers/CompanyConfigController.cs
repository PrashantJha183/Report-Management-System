using Report.Models;
using Report.Services;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Report.Controllers
{
    public class CompanyConfigController : Controller
    {
        private readonly CompanyConfigService _companyConfigService;

        public CompanyConfigController()
        {
            _companyConfigService = new CompanyConfigService();
        }

        [HttpGet]
        public async Task<ActionResult> Index(CompanyConfigGridViewModel model)
        {
            var result = await _companyConfigService.GetCompanyConfigsAsync(model);
            ViewBag.NextIdentity = await _companyConfigService.GetNextIdentityAsync();
            ViewBag.TableName = ReportConfig.CompanyConfigTable;
            ViewBag.AuditTableName = ReportConfig.CompanyConfigTable;
            ViewBag.RecordIdLabel = "Company Config ID";
            ViewBag.Colspan = 6;
            return View(result);
        }

        [HttpPost]
        public async Task<JsonResult> SaveChanges(SaveCompanyConfigChangesRequest request)
        {
            try
            {
                var result = await _companyConfigService.SaveChangesAsync(request.Changes, request.Mode);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new SaveCompanyConfigChangesResult { Status = "ERROR:" + ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            var status = await _companyConfigService.DeleteCompanyConfigAsync(id);
            if (status == "DELETED")
                return Json(new { Status = "DELETED" });
            return Json(new { Status = status });
        }
    }
}
