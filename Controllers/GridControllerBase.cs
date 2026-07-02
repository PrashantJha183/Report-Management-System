using Report.Models;
using Report.Services;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Report.Controllers
{
    /// <summary>
    /// Generic base controller for grid CRUD operations.
    /// Provides Index (GET), SaveChanges (POST), and Delete (POST) actions
    /// that delegate to a typed service.
    /// </summary>
    /// <typeparam name="TService">The service type (e.g. AlertConfigService)</typeparam>
    /// <typeparam name="TGridViewModel">The grid view model type</typeparam>
    /// <typeparam name="TChange">The change item type</typeparam>
    /// <typeparam name="TRequest">The save changes request type</typeparam>
    /// <typeparam name="TResult">The save changes result type</typeparam>
    public abstract class GridControllerBase<TService, TGridViewModel, TChange, TRequest, TResult> : Controller
        where TService : class
        where TGridViewModel : GridViewModelBase, new()
        where TRequest : class, new()
        where TResult : class, new()
    {
        protected readonly TService _service;

        protected GridControllerBase(TService service)
        {
            _service = service;
        }

        /// <summary>
        /// The name of the table for audit/ViewBag.
        /// </summary>
        protected abstract string TableName { get; }

        /// <summary>
        /// The audit table name (usually same as TableName).
        /// </summary>
        protected abstract string AuditTableName { get; }

        /// <summary>
        /// Label for the record ID in audit view.
        /// </summary>
        protected abstract string RecordIdLabel { get; }

        /// <summary>
        /// Column span for the table in the view.
        /// </summary>
        protected abstract int Colspan { get; }

        /// <summary>
        /// Whether the view should show the Deleted Records button.
        /// </summary>
        protected virtual bool ShowDeletedRecords => true;

        /// <summary>
        /// GET: Index — fetch paged data and render the grid view.
        /// Override in derived controller to set additional ViewBag items.
        /// </summary>
        [HttpGet]
        public virtual async Task<ActionResult> Index(TGridViewModel model)
        {
            var result = await GetPagedDataAsync(model);
            ViewBag.NextIdentity = await GetNextIdentityAsync();
            ViewBag.TableName = TableName;
            ViewBag.AuditTableName = AuditTableName;
            ViewBag.RecordIdLabel = RecordIdLabel;
            ViewBag.Colspan = Colspan;
            ViewBag.ShowDeletedRecords = ShowDeletedRecords;
            return View(result);
        }

        /// <summary>
        /// Fetch paged data. Override if the service method name differs.
        /// </summary>
        protected virtual Task<TGridViewModel> GetPagedDataAsync(TGridViewModel model)
        {
            // Use reflection to find the service method — derived classes
            // typically override this to call the specific service method.
            throw new NotImplementedException("Override GetPagedDataAsync in the derived controller.");
        }

        /// <summary>
        /// Get the next identity. Override if needed.
        /// </summary>
        protected virtual Task<int> GetNextIdentityAsync()
        {
            return Task.FromResult(1);
        }

        /// <summary>
        /// POST: SaveChanges — accepts changes and persists them.
        /// </summary>
        [HttpPost]
        public virtual async Task<JsonResult> SaveChanges(TRequest request)
        {
            try
            {
                var result = await SaveChangesAsync(request);
                return Json(result);
            }
            catch (Exception ex)
            {
                var errorResult = new TResult();
                var resultType = errorResult.GetType();
                resultType.GetProperty("Status")?.SetValue(errorResult, "ERROR:" + ex.Message);
                return Json(errorResult);
            }
        }

        /// <summary>
        /// Persist changes. Override to call the specific service method.
        /// </summary>
        protected virtual Task<TResult> SaveChangesAsync(TRequest request)
        {
            throw new NotImplementedException("Override SaveChangesAsync in the derived controller.");
        }

        /// <summary>
        /// POST: Delete — deletes a record by ID.
        /// </summary>
        [HttpPost]
        public virtual async Task<JsonResult> Delete(int id)
        {
            var status = await DeleteAsync(id);
            if (status == "DELETED")
                return Json(new { Status = "DELETED" });
            return Json(new { Status = status });
        }

        /// <summary>
        /// Delete a record. Override to call the specific service method.
        /// </summary>
        protected virtual Task<string> DeleteAsync(int id)
        {
            throw new NotImplementedException("Override DeleteAsync in the derived controller.");
        }
    }
}
