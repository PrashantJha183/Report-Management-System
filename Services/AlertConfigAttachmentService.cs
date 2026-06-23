using Report.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Report.Services
{
    public class AlertConfigAttachmentService
    {
        private readonly IDal _dal;
        private readonly AuditLogService _auditLog;

        private static readonly HashSet<string> ColumnWhitelist = new HashSet<string>
        {
            "AlertConfigId", "AttachmentFileTypeId", "EmailAttachmentUrl"
        };

        public AlertConfigAttachmentService() : this(new Dal())
        {
        }

        public AlertConfigAttachmentService(IDal dal)
        {
            _dal = dal;
            _auditLog = new AuditLogService(dal);
        }

        #region OLD_SYNC
        //public AlertConfigAttachmentGridViewModel GetAlertConfigAttachments(AlertConfigAttachmentGridViewModel filter)
        //{
        //    if (filter == null)
        //        filter = new AlertConfigAttachmentGridViewModel();

        //    var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        //    var pageSize = 7;
        //    if (pageSize < 10) pageSize = 7;
        //    if (pageSize > 100) pageSize = 100;

        //    var parameters = new List<DbParameter>();
        //    var where = new StringBuilder(" WHERE 1=1");

        //    if (!string.IsNullOrWhiteSpace(filter.SearchText))
        //    {
        //        string search = filter.SearchText.Trim();
        //        where.Append(" AND EmailAttachmentUrl LIKE @search");
        //        parameters.Add(_dal.CreateParameter("@search", $"%{search}%"));
        //    }

        //    if (filter.FilterAlertConfigId > 0)
        //    {
        //        where.Append(" AND AlertConfigId = @filterAlertConfigId");
        //        parameters.Add(_dal.CreateParameter("@filterAlertConfigId", filter.FilterAlertConfigId));
        //    }

        //    string countSql = $"SELECT COUNT(*) FROM com_mst_alertconfigattachment{where}";
        //    filter.TotalRecords = Convert.ToInt32(_dal.ExecuteScalar(countSql, parameters.ToArray()));

        //    string sortColumn = GetSortColumnExpression(filter.SortColumn);
        //    string sortDir = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        //    int offset = (pageNumber - 1) * pageSize;

        //    var dataParams = parameters.Select(p => _dal.CreateParameter(p.ParameterName, p.Value)).ToList<DbParameter>();
        //    string sql = $@"
        //        SELECT AlertConfigAttachmentId, AlertConfigId, AttachmentFileTypeId, EmailAttachmentUrl
        //        FROM com_mst_alertconfigattachment{where}
        //        ORDER BY {sortColumn} {sortDir}
        //        {_dal.GetPaginationClause(offset, pageSize)}";

        //    DataTable dt = _dal.ExecuteQuery(sql, dataParams.ToArray());

        //    var items = new List<AlertConfigAttachmentGridItemViewModel>();
        //    foreach (DataRow row in dt.Rows)
        //    {
        //        var item = new AlertConfigAttachmentGridItemViewModel
        //        {
        //            AlertConfigAttachmentId = row["AlertConfigAttachmentId"] == DBNull.Value ? 0 : Convert.ToInt32(row["AlertConfigAttachmentId"]),
        //            AlertConfigId = row["AlertConfigId"] == DBNull.Value ? 0 : Convert.ToInt32(row["AlertConfigId"]),
        //            AttachmentFileTypeId = row["AttachmentFileTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(row["AttachmentFileTypeId"]),
        //            EmailAttachmentUrl = row["EmailAttachmentUrl"] == DBNull.Value ? "" : row["EmailAttachmentUrl"].ToString()
        //        };
        //        items.Add(item);
        //    }

        //    filter.Items = items;
        //    filter.PageNumber = pageNumber;
        //    filter.PageSize = pageSize;
        //    return filter;
        //}
        #endregion

        public async Task<AlertConfigAttachmentGridViewModel> GetAlertConfigAttachmentsAsync(AlertConfigAttachmentGridViewModel filter)
        {
            if (filter == null)
                filter = new AlertConfigAttachmentGridViewModel();

            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = ReportConfig.AlertConfigAttachmentPageSize;
            if (pageSize > ReportConfig.MaxPageSize) pageSize = ReportConfig.MaxPageSize;

            var parameters = new List<DbParameter>();
            var where = new StringBuilder(" WHERE 1=1");

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                string search = filter.SearchText.Trim();
                where.Append(" AND EmailAttachmentUrl LIKE @search");
                parameters.Add(_dal.CreateParameter("@search", $"%{search}%"));
            }

            if (filter.FilterAlertConfigId > 0)
            {
                where.Append(" AND AlertConfigId = @filterAlertConfigId");
                parameters.Add(_dal.CreateParameter("@filterAlertConfigId", filter.FilterAlertConfigId));
            }

            string sortColumn = GetSortColumnExpression(filter.SortColumn);
            string sortDir = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            int offset = (pageNumber - 1) * pageSize;

            string sql = $@"
                SELECT COUNT(*) OVER() AS TotalRecords, AlertConfigAttachmentId, AlertConfigId, AttachmentFileTypeId, EmailAttachmentUrl
                FROM {ReportConfig.AlertConfigAttachmentTable}{where}
                ORDER BY {sortColumn} {sortDir}
                {_dal.GetPaginationClause(offset, pageSize)}";

            DataTable dt = await _dal.ExecuteQueryAsync(sql, parameters.ToArray());

            var items = new List<AlertConfigAttachmentGridItemViewModel>();
            foreach (DataRow row in dt.Rows)
            {
                var item = new AlertConfigAttachmentGridItemViewModel
                {
                    AlertConfigAttachmentId = row["AlertConfigAttachmentId"] == DBNull.Value ? 0 : Convert.ToInt32(row["AlertConfigAttachmentId"]),
                    AlertConfigId = row["AlertConfigId"] == DBNull.Value ? 0 : Convert.ToInt32(row["AlertConfigId"]),
                    AttachmentFileTypeId = row["AttachmentFileTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(row["AttachmentFileTypeId"]),
                    EmailAttachmentUrl = row["EmailAttachmentUrl"] == DBNull.Value ? "" : row["EmailAttachmentUrl"].ToString()
                };
                items.Add(item);
            }

            filter.Items = items;
            filter.TotalRecords = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["TotalRecords"]) : 0;
            filter.PageNumber = pageNumber;
            filter.PageSize = pageSize;

            if (filter.FilterAlertConfigId > 0)
            {
                var dtName = await _dal.ExecuteQueryAsync(
                    $"SELECT AlertName FROM {ReportConfig.AlertConfigTable} WHERE AlertConfigId = @id",
                    _dal.CreateParameter("@id", filter.FilterAlertConfigId));
                if (dtName.Rows.Count > 0)
                    filter.AlertName = dtName.Rows[0]["AlertName"]?.ToString() ?? "";
            }

            return filter;
        }

        #region OLD_SYNC
        //public SaveAlertConfigAttachmentChangesResult SaveChanges(List<AlertConfigAttachmentChange> changes, string mode)
        //{
        //    var result = new SaveAlertConfigAttachmentChangesResult { Status = "SAVED", IdMappings = new Dictionary<string, int>() };

        //    if (mode == "CANCEL")
        //    {
        //        result.Status = "CANCELLED";
        //        return result;
        //    }

        //    if (changes == null || changes.Count == 0)
        //    {
        //        result.Status = "NO_CHANGES";
        //        return result;
        //    }

        //    var columnWhitelist = new Dictionary<string, string>
        //    {
        //        { "AlertConfigId", "AlertConfigId" },
        //        { "AttachmentFileTypeId", "AttachmentFileTypeId" },
        //        { "EmailAttachmentUrl", "EmailAttachmentUrl" }
        //    };

        //    var rows = changes.GroupBy(c => c.AlertConfigAttachmentId);

        //    var sql = new StringBuilder();
        //    var parameters = new List<DbParameter>();
        //    var paramIndex = 0;

        //    foreach (var rowGroup in rows)
        //    {
        //        var alertConfigAttachmentId = rowGroup.Key;

        //        if (alertConfigAttachmentId < 0)
        //        {
        //            var cols = new List<string>();
        //            var vals = new List<string>();

        //            foreach (var change in rowGroup)
        //            {
        //                if (!ColumnWhitelist.Contains(change.Column))
        //                    continue;

        //                var valParam = $"@val{paramIndex}";
        //                cols.Add(change.Column);
        //                vals.Add(valParam);

        //                object paramValue = string.IsNullOrEmpty(change.NewValue) ? DBNull.Value : (object)change.NewValue;
        //                parameters.Add(_dal.CreateParameter(valParam, paramValue));
        //                paramIndex++;
        //            }

        //            if (cols.Count == 0) continue;

        //            sql.AppendLine($"    INSERT INTO com_mst_alertconfigattachment ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)});");
        //            sql.AppendLine($"    SELECT CAST({_dal.GetIdentityFunction()} AS {_dal.GetIdentityCastType()}) AS NewId, @tempId{paramIndex} AS TempId, NULL AS ErrorMessage;");
        //            parameters.Add(_dal.CreateParameter($"@tempId{paramIndex}", alertConfigAttachmentId));
        //            paramIndex++;
        //        }
        //        else
        //        {
        //            foreach (var change in rowGroup)
        //            {
        //                if (!ColumnWhitelist.Contains(change.Column))
        //                    continue;

        //                var valParam = $"@val{paramIndex}";
        //                var pkParam = $"@pk{paramIndex}";

        //                sql.AppendLine($"    UPDATE com_mst_alertconfigattachment SET {change.Column} = {valParam} WHERE AlertConfigAttachmentId = {pkParam};");

        //                object paramValue = string.IsNullOrEmpty(change.NewValue) ? DBNull.Value : (object)change.NewValue;
        //                parameters.Add(_dal.CreateParameter(valParam, paramValue));
        //                parameters.Add(_dal.CreateParameter(pkParam, alertConfigAttachmentId));
        //                paramIndex++;
        //            }
        //        }
        //    }

        //    try
        //    {
        //        _dal.BeginTransaction();
        //        var dt = _dal.ExecuteQuery(sql.ToString(), parameters.ToArray());
        //        _dal.Commit();

        //        foreach (DataRow row in dt.Rows)
        //        {
        //            if (dt.Columns.Contains("TempId") && row["TempId"] != DBNull.Value &&
        //                dt.Columns.Contains("NewId") && row["NewId"] != DBNull.Value)
        //            {
        //                var tempId = row["TempId"].ToString();
        //                var newId = Convert.ToInt32(row["NewId"]);
        //                if (int.TryParse(tempId, out _))
        //                {
        //                    result.IdMappings[tempId] = newId;
        //                }
        //            }
        //        }

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        _dal.Rollback();
        //        result.Status = "ERROR:" + ex.Message;
        //        return result;
        //    }
        //}
        #endregion

        public async Task<SaveAlertConfigAttachmentChangesResult> SaveChangesAsync(List<AlertConfigAttachmentChange> changes, string mode)
        {
            var result = new SaveAlertConfigAttachmentChangesResult { Status = "SAVED", IdMappings = new Dictionary<string, int>() };

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

            // Whitelist enforced via ColumnWhitelist static HashSet

            var rows = changes.GroupBy(c => c.AlertConfigAttachmentId);

            var sql = new StringBuilder();
            var parameters = new List<DbParameter>();
            var paramIndex = 0;

            foreach (var rowGroup in rows)
            {
                var alertConfigAttachmentId = rowGroup.Key;

                if (alertConfigAttachmentId < 0)
                {
                    var cols = new List<string>();
                    var vals = new List<string>();

                    foreach (var change in rowGroup)
                    {
                        if (!ColumnWhitelist.Contains(change.Column))
                            continue;

                        var valParam = $"@val{paramIndex}";
                        cols.Add(change.Column);
                        vals.Add(valParam);

                        object paramValue = string.IsNullOrEmpty(change.NewValue) ? DBNull.Value : (object)change.NewValue;
                        parameters.Add(_dal.CreateParameter(valParam, paramValue));
                        paramIndex++;
                    }

                    if (cols.Count == 0) continue;

                    sql.AppendLine($"    INSERT INTO {ReportConfig.AlertConfigAttachmentTable} ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)});");
                    sql.AppendLine($"    SELECT CAST({_dal.GetIdentityFunction()} AS {_dal.GetIdentityCastType()}) AS NewId, @tempId{paramIndex} AS TempId, NULL AS ErrorMessage;");
                    parameters.Add(_dal.CreateParameter($"@tempId{paramIndex}", alertConfigAttachmentId));
                    paramIndex++;
                }
                else
                {
                    var setClauses = new List<string>();
                    foreach (var change in rowGroup)
                    {
                        if (!ColumnWhitelist.Contains(change.Column))
                            continue;

                        var valParam = $"@val{paramIndex}";
                        setClauses.Add($"{change.Column} = {valParam}");

                        object paramValue = string.IsNullOrEmpty(change.NewValue) ? DBNull.Value : (object)change.NewValue;
                        parameters.Add(_dal.CreateParameter(valParam, paramValue));
                        paramIndex++;
                    }

                    if (setClauses.Count == 0) continue;

                    var pkParam = $"@pk{paramIndex}";
                    sql.AppendLine($"    UPDATE {ReportConfig.AlertConfigAttachmentTable} SET {string.Join(", ", setClauses)} WHERE AlertConfigAttachmentId = {pkParam};");
                    parameters.Add(_dal.CreateParameter(pkParam, alertConfigAttachmentId));
                    paramIndex++;
                }
            }

            var auditEntries = new List<(int RecordId, string OldValues)>();
            foreach (var rowGroup in rows)
            {
                if (rowGroup.Key < 0) continue;
                var oldJson = await _dal.GetRowAsJsonAsync(ReportConfig.AlertConfigAttachmentTable, "AlertConfigAttachmentId", rowGroup.Key);
                if (oldJson != null)
                    auditEntries.Add((rowGroup.Key, oldJson));
            }

            try
            {
                _dal.BeginTransaction();
                var dt = await _dal.ExecuteQueryAsync(sql.ToString(), parameters.ToArray());
                _dal.Commit();

                foreach (DataRow row in dt.Rows)
                {
                    if (dt.Columns.Contains("TempId") && row["TempId"] != DBNull.Value &&
                        dt.Columns.Contains("NewId") && row["NewId"] != DBNull.Value)
                    {
                        var tempId = row["TempId"].ToString();
                        var newId = Convert.ToInt32(row["NewId"]);
                        if (int.TryParse(tempId, out _))
                        {
                            result.IdMappings[tempId] = newId;
                        }
                    }
                }

                foreach (var entry in auditEntries)
                    await _auditLog.LogChangeAsync(ReportConfig.AlertConfigAttachmentTable, entry.RecordId, "UPDATE", entry.OldValues);

                return result;
            }
            catch (Exception ex)
            {
                _dal.Rollback();
                result.Status = "ERROR:" + ex.Message;
                return result;
            }
        }

        #region OLD_SYNC
        //public string DeleteAlertConfigAttachment(int alertConfigAttachmentId)
        //{
        //    var rowsAffected = _dal.ExecuteNonQuery(
        //        "DELETE FROM com_mst_alertconfigattachment WHERE AlertConfigAttachmentId = @id",
        //        _dal.CreateParameter("@id", alertConfigAttachmentId));
        //    return rowsAffected > 0 ? "DELETED" : "NOT_FOUND";
        //}
        #endregion

        public async Task<string> DeleteAlertConfigAttachmentAsync(int alertConfigAttachmentId)
        {
            var oldJson = await _dal.GetRowAsJsonAsync(ReportConfig.AlertConfigAttachmentTable, "AlertConfigAttachmentId", alertConfigAttachmentId);
            var rowsAffected = await _dal.ExecuteNonQueryAsync(
                $"DELETE FROM {ReportConfig.AlertConfigAttachmentTable} WHERE AlertConfigAttachmentId = @id",
                _dal.CreateParameter("@id", alertConfigAttachmentId));
            if (rowsAffected > 0)
                await _auditLog.LogChangeAsync(ReportConfig.AlertConfigAttachmentTable, alertConfigAttachmentId, "DELETE", oldJson);
            return rowsAffected > 0 ? "DELETED" : "NOT_FOUND";
        }

        private static string GetSortColumnExpression(string sortColumn)
        {
            switch (sortColumn)
            {
                case "AlertConfigId": return "AlertConfigId";
                case "AttachmentFileTypeId": return "AttachmentFileTypeId";
                case "EmailAttachmentUrl": return "EmailAttachmentUrl";
                default: return "AlertConfigAttachmentId";
            }
        }
    }
}
