using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Report.Models;

namespace Report.Services
{
    public class DynamicCodeService
    {
        private readonly CustomCodeDAL _dal;
        private readonly AuditLogService _auditLog;

        public DynamicCodeService()
        {
            _dal = new CustomCodeDAL();
            _auditLog = new AuditLogService();
        }

        public async Task<DynamicCodeGridViewModel> GetAllAsync(DynamicCodeGridViewModel filter)
        {
            if (filter == null)
                filter = new DynamicCodeGridViewModel();

            var allItems = await _dal.GetAllAsync(filter.SearchText);

            // Sort
            allItems = SortItems(allItems, filter.SortColumn, filter.SortDirection);

            // Pagination
            filter.TotalRecords = allItems.Count;
            var pagedItems = allItems
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            filter.Items = pagedItems;
            return filter;
        }

        public async Task<SaveDynamicCodeChangesResult> SaveChangesAsync(
            List<DynamicCodeChange> changes, string mode)
        {
            var result = new SaveDynamicCodeChangesResult { IdMappings = new Dictionary<string, int>() };

            if (mode == "CANCEL")
            {
                result.Status = "CANCELLED";
                return result;
            }

            if (changes == null || changes.Count == 0)
            {
                result.Status = "NO_CHANGES";
                return result;
            }

            var rows = changes.GroupBy(c => c.DynamicCodeId);

            foreach (var rowGroup in rows)
            {
                var entityId = rowGroup.Key;

                if (entityId < 0) // NEW row — INSERT
                {
                    var model = new IDEViewModel();
                    foreach (var change in rowGroup)
                    {
                        ApplyChangeToModel(model, change.Column, change.NewValue);
                    }

                    int newId = await _dal.AddAsync(model);
                    result.IdMappings[entityId.ToString()] = newId;
                }
                else // EXISTING row — UPDATE
                {
                    var existing = await _dal.GetByIdAsync(entityId);
                    if (existing == null) continue;

                    var oldJson = await GetRowAsJsonAsync(entityId);

                    foreach (var change in rowGroup)
                    {
                        ApplyChangeToModel(existing, change.Column, change.NewValue);
                    }

                    existing.CodeID = entityId;
                    await _dal.UpdateAsync(existing);

                    if (oldJson != null)
                        await _auditLog.LogChangeAsync(ReportConfig.DynamicCodeTable, entityId, "UPDATE", oldJson);
                }
            }

            result.Status = "OK";
            return result;
        }

        public async Task<string> DeleteAsync(int id)
        {
            try
            {
                var oldJson = await GetRowAsJsonAsync(id);

                await _dal.DeleteAsync(id);

                if (oldJson != null)
                {
                    try
                    {
                        await _auditLog.LogChangeAsync(ReportConfig.DynamicCodeTable, id, "DELETE", oldJson);
                    }
                    catch
                    {
                        // Audit failure must not fail a completed delete.
                    }
                }

                return "DELETED";
            }
            catch (Exception ex)
            {
                return "ERROR:" + ex.Message;
            }
        }

        public async Task<int> GetNextIdentityAsync()
        {
            return await _dal.GetNextIdAsync();
        }

        public async Task<IDEViewModel> GetCodeByIdAsync(int id)
        {
            return await _dal.GetByIdAsync(id);
        }

        public async Task<IDEViewModel> GetByControllerActionAsync(string controller, string action)
        {
            return await _dal.GetByControllerActionAsync(controller, action);
        }

        public async Task<int> SaveCodeFromIDEAsync(IDEViewModel model)
        {
            if (model.CodeID > 0)
            {
                var existing = await _dal.GetByIdAsync(model.CodeID);
                if (existing == null)
                    return 0;

                var oldJson = await GetRowAsJsonAsync(model.CodeID);

                MergeForUpdate(existing, model);

                await _dal.UpdateAsync(existing);

                if (oldJson != null)
                    await _auditLog.LogChangeAsync(ReportConfig.DynamicCodeTable, existing.CodeID, "UPDATE", oldJson);

                return existing.CodeID;
            }
            else
            {
                return await _dal.AddAsync(model);
            }
        }

        // ── Only overwrite a field when the user actually provided/edited it; otherwise keep the old value ──
        private void MergeForUpdate(IDEViewModel existing, IDEViewModel incoming)
        {
            if (!string.IsNullOrWhiteSpace(incoming.ControllerName))
                existing.ControllerName = incoming.ControllerName;
            if (!string.IsNullOrWhiteSpace(incoming.ActionName))
                existing.ActionName = incoming.ActionName;
            if (!string.IsNullOrWhiteSpace(incoming.Namespaces))
                existing.Namespaces = incoming.Namespaces;
            if (!string.IsNullOrWhiteSpace(incoming.Code))
                existing.Code = incoming.Code;
            if (!string.IsNullOrWhiteSpace(incoming.Description))
                existing.Description = incoming.Description;
        }

        // ── Private helpers ──

        private async Task<string> GetRowAsJsonAsync(int id)
        {
            return await new Dal().GetRowAsJsonAsync(ReportConfig.DynamicCodeTable, "CustomCodeId", id);
        }

        private void ApplyChangeToModel(IDEViewModel model, string column, string value)
        {
            switch (column)
            {
                case "ControllerName":
                    model.ControllerName = value;
                    break;
                case "ActionName":
                    model.ActionName = value;
                    break;
                case "Description":
                    model.Description = value;
                    break;
            }
        }

        private List<DynamicCodeGridItem> SortItems(
            List<DynamicCodeGridItem> items, string sortColumn, string sortDirection)
        {
            bool asc = string.IsNullOrEmpty(sortDirection) || sortDirection.ToLower() == "asc";

            switch (sortColumn)
            {
                case "ControllerName":
                    return asc ? items.OrderBy(x => x.ControllerName).ToList()
                               : items.OrderByDescending(x => x.ControllerName).ToList();
                case "ActionName":
                    return asc ? items.OrderBy(x => x.ActionName).ToList()
                               : items.OrderByDescending(x => x.ActionName).ToList();
                case "Description":
                    return asc ? items.OrderBy(x => x.Description).ToList()
                               : items.OrderByDescending(x => x.Description).ToList();
                default: // CustomCodeId
                    return asc ? items.OrderBy(x => x.DynamicCodeId).ToList()
                               : items.OrderByDescending(x => x.DynamicCodeId).ToList();
            }
        }
    }
}