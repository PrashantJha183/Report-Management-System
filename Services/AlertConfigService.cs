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
    public class AlertConfigService
    {
        private readonly IDal _dal;
        private readonly AuditLogService _auditLog;

        public AlertConfigService() : this(new Dal())
        {
        }

        public AlertConfigService(IDal dal)
        {
            _dal = dal;
            _auditLog = new AuditLogService(dal);
        }

        private static readonly HashSet<string> ColumnWhitelist = new HashSet<string>
        {
            "AlertName", "ReportId", "IsEmailNotify", "EmailSubject",
            "EmailContentHeader", "EmailContentUrl", "AttachmentFileTypeId",
            "EmailAttachmentUrl", "StatusId", "AlertTypeId", "IsMultiAlert",
            "MultiAlertQuery", "EmailContentFooter"
        };

        #region OLD_SYNC (retained for reference — all callers use async)
        //public AlertConfigGridViewModel GetAlertConfigs(AlertConfigGridViewModel filter)
        //{
        //    if (filter == null)
        //        filter = new AlertConfigGridViewModel();
        //
        //    var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        //    var pageSize = 5;
        //    if (pageSize < 10) pageSize = 5;
        //    if (pageSize > 100) pageSize = 100;
        //
        //    var parameters = new List<DbParameter>();
        //    var where = new StringBuilder(" WHERE 1=1");
        //
        //    if (!string.IsNullOrWhiteSpace(filter.SearchText))
        //    {
        //        string search = filter.SearchText.Trim();
        //        where.Append(" AND AlertName LIKE @search");
        //        parameters.Add(_dal.CreateParameter("@search", $"%{search}%"));
        //    }
        //
        //    string countSql = $"SELECT COUNT(*) FROM com_mst_alertconfig{where}";
        //    filter.TotalRecords = Convert.ToInt32(_dal.ExecuteScalar(countSql, parameters.ToArray()));
        //
        //    string sortColumn = GetSortColumnExpression(filter.SortColumn);
        //    string sortDir = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        //    int offset = (pageNumber - 1) * pageSize;
        //
        //    var dataParams = parameters.Select(p => _dal.CreateParameter(p.ParameterName, p.Value)).ToList<DbParameter>();
        //    string sql = $@"
        //        SELECT AlertConfigId, AlertName, ReportId, IsEmailNotify, EmailSubject,
        //               EmailContentHeader, EmailContentUrl, AttachmentFileTypeId, EmailAttachmentUrl,
        //               StatusId, AlertTypeId, IsMultiAlert, MultiAlertQuery, EmailContentFooter,
        //               CreatedBy, CreatedOn, UpdatedBy, UpdatedOn
        //        FROM com_mst_alertconfig{where}
        //        ORDER BY {sortColumn} {sortDir}
        //        {_dal.GetPaginationClause(offset, pageSize)}";
        //
        //    DataTable dt = _dal.ExecuteQuery(sql, dataParams.ToArray());
        //
        //    var items = new List<AlertConfigGridItemViewModel>();
        //    foreach (DataRow row in dt.Rows)
        //    {
        //        var item = new AlertConfigGridItemViewModel
        //        {
        //            AlertConfigId = row["AlertConfigId"] == DBNull.Value ? 0 : Convert.ToInt32(row["AlertConfigId"]),
        //            AlertName = row["AlertName"] == DBNull.Value ? "" : row["AlertName"].ToString(),
        //            ReportId = row["ReportId"] == DBNull.Value ? 0 : Convert.ToInt32(row["ReportId"]),
        //            IsEmailNotify = row["IsEmailNotify"] == DBNull.Value ? false : Convert.ToBoolean(row["IsEmailNotify"]),
        //            EmailSubject = row["EmailSubject"] == DBNull.Value ? "" : row["EmailSubject"].ToString(),
        //            EmailContentHeader = row["EmailContentHeader"] == DBNull.Value ? "" : row["EmailContentHeader"].ToString(),
        //            EmailContentUrl = row["EmailContentUrl"] == DBNull.Value ? "" : row["EmailContentUrl"].ToString(),
        //            AttachmentFileTypeId = row["AttachmentFileTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(row["AttachmentFileTypeId"]),
        //            EmailAttachmentUrl = row["EmailAttachmentUrl"] == DBNull.Value ? "" : row["EmailAttachmentUrl"].ToString(),
        //            StatusId = row["StatusId"] == DBNull.Value ? 0 : Convert.ToInt32(row["StatusId"]),
        //            AlertTypeId = row["AlertTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(row["AlertTypeId"]),
        //            IsMultiAlert = row["IsMultiAlert"] == DBNull.Value ? false : Convert.ToBoolean(row["IsMultiAlert"]),
        //            MultiAlertQuery = row["MultiAlertQuery"] == DBNull.Value ? "" : row["MultiAlertQuery"].ToString(),
        //            EmailContentFooter = row["EmailContentFooter"] == DBNull.Value ? "" : row["EmailContentFooter"].ToString(),
        //            CreatedBy = row["CreatedBy"] == DBNull.Value ? 0 : Convert.ToInt32(row["CreatedBy"]),
        //            CreatedOn = row["CreatedOn"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedOn"]),
        //            UpdatedBy = row["UpdatedBy"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["UpdatedBy"]),
        //            UpdatedOn = row["UpdatedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["UpdatedOn"])
        //        };
        //        items.Add(item);
        //    }
        //
        //    filter.Items = items;
        //    filter.PageNumber = pageNumber;
        //    filter.PageSize = pageSize;
        //    return filter;
        //}
        #endregion

        public async Task<AlertConfigGridViewModel> GetAlertConfigsAsync(AlertConfigGridViewModel filter)
        {
            if (filter == null)
                filter = new AlertConfigGridViewModel();

            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = ReportConfig.AlertConfigPageSize;
            if (pageSize < ReportConfig.AlertConfigPageSize) pageSize = ReportConfig.AlertConfigPageSize;
            if (pageSize > ReportConfig.MaxPageSize) pageSize = ReportConfig.MaxPageSize;

            var parameters = new List<DbParameter>();
            var where = new StringBuilder(" WHERE 1=1");

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                string search = filter.SearchText.Trim();
                where.Append(" AND AlertName LIKE @search");
                parameters.Add(_dal.CreateParameter("@search", $"%{search}%"));
            }

            // OLD: Separate COUNT + SELECT (2 round trips)
            //string countSql = $"SELECT COUNT(*) FROM com_mst_alertconfig{where}";
            //filter.TotalRecords = Convert.ToInt32(await _dal.ExecuteScalarAsync(countSql, parameters.ToArray()));
            //var dataParams = parameters.Select(p => _dal.CreateParameter(p.ParameterName, p.Value)).ToList<DbParameter>();

            string sortColumn = GetSortColumnExpression(filter.SortColumn);
            string sortDir = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            int offset = (pageNumber - 1) * pageSize;

            string sql = $@"
                SELECT AlertConfigId, AlertName, ReportId, IsEmailNotify, EmailSubject,
                       EmailContentHeader, EmailContentUrl, AttachmentFileTypeId, EmailAttachmentUrl,
                       StatusId, AlertTypeId, IsMultiAlert, MultiAlertQuery, EmailContentFooter,
                       CreatedBy, CreatedOn, UpdatedBy, UpdatedOn,
                       COUNT(*) OVER() AS TotalRecords
                FROM {ReportConfig.AlertConfigTable}{where}
                ORDER BY {sortColumn} {sortDir}
                {_dal.GetPaginationClause(offset, pageSize)}";

            DataTable dt = await _dal.ExecuteQueryAsync(sql, parameters.ToArray());
            filter.TotalRecords = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["TotalRecords"]) : 0;

            var items = new List<AlertConfigGridItemViewModel>();
            foreach (DataRow row in dt.Rows)
            {
                var item = new AlertConfigGridItemViewModel
                {
                    AlertConfigId = row["AlertConfigId"] == DBNull.Value ? 0 : Convert.ToInt32(row["AlertConfigId"]),
                    AlertName = row["AlertName"] == DBNull.Value ? "" : row["AlertName"].ToString(),
                    ReportId = row["ReportId"] == DBNull.Value ? 0 : Convert.ToInt32(row["ReportId"]),
                    IsEmailNotify = row["IsEmailNotify"] == DBNull.Value ? false : Convert.ToBoolean(row["IsEmailNotify"]),
                    EmailSubject = row["EmailSubject"] == DBNull.Value ? "" : row["EmailSubject"].ToString(),
                    EmailContentHeader = row["EmailContentHeader"] == DBNull.Value ? "" : row["EmailContentHeader"].ToString(),
                    EmailContentUrl = row["EmailContentUrl"] == DBNull.Value ? "" : row["EmailContentUrl"].ToString(),
                    AttachmentFileTypeId = row["AttachmentFileTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(row["AttachmentFileTypeId"]),
                    EmailAttachmentUrl = row["EmailAttachmentUrl"] == DBNull.Value ? "" : row["EmailAttachmentUrl"].ToString(),
                    StatusId = row["StatusId"] == DBNull.Value ? 0 : Convert.ToInt32(row["StatusId"]),
                    AlertTypeId = row["AlertTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(row["AlertTypeId"]),
                    IsMultiAlert = row["IsMultiAlert"] == DBNull.Value ? false : Convert.ToBoolean(row["IsMultiAlert"]),
                    MultiAlertQuery = row["MultiAlertQuery"] == DBNull.Value ? "" : row["MultiAlertQuery"].ToString(),
                    EmailContentFooter = row["EmailContentFooter"] == DBNull.Value ? "" : row["EmailContentFooter"].ToString(),
                    CreatedBy = row["CreatedBy"] == DBNull.Value ? 0 : Convert.ToInt32(row["CreatedBy"]),
                    CreatedOn = row["CreatedOn"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedOn"]),
                    UpdatedBy = row["UpdatedBy"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["UpdatedBy"]),
                    UpdatedOn = row["UpdatedOn"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["UpdatedOn"])
                };
                items.Add(item);
            }

            filter.Items = items;
            filter.PageNumber = pageNumber;
            filter.PageSize = pageSize;
            return filter;
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //public SaveAlertConfigChangesResult SaveChanges(List<AlertConfigChange> changes, string mode)
        //{
        //    var result = new SaveAlertConfigChangesResult { Status = "SAVED", IdMappings = new Dictionary<string, int>() };
        //
        //    if (mode == "CANCEL")
        //    {
        //        result.Status = "CANCELLED";
        //        return result;
        //    }
        //
        //    if (changes == null || changes.Count == 0)
        //    {
        //        result.Status = "NO_CHANGES";
        //        return result;
        //    }
        //
        //    var columnWhitelist = new Dictionary<string, string>
        //    {
        //        { "AlertName", "AlertName" },
        //        { "ReportId", "ReportId" },
        //        { "IsEmailNotify", "IsEmailNotify" },
        //        { "EmailSubject", "EmailSubject" },
        //        { "EmailContentHeader", "EmailContentHeader" },
        //        { "EmailContentUrl", "EmailContentUrl" },
        //        { "AttachmentFileTypeId", "AttachmentFileTypeId" },
        //        { "EmailAttachmentUrl", "EmailAttachmentUrl" },
        //        { "StatusId", "StatusId" },
        //        { "AlertTypeId", "AlertTypeId" },
        //        { "IsMultiAlert", "IsMultiAlert" },
        //        { "MultiAlertQuery", "MultiAlertQuery" },
        //        { "EmailContentFooter", "EmailContentFooter" }
        //    };
        //
        //    var rows = changes.GroupBy(c => c.AlertConfigId);
        //
        //    var sql = new StringBuilder();
        //
        //    var parameters = new List<DbParameter>();
        //    var paramIndex = 0;
        //
        //    var istNow = _dal.GetCurrentIstTime();
        //
        //    foreach (var rowGroup in rows)
        //    {
        //        var alertConfigId = rowGroup.Key;
        //
        //        if (alertConfigId < 0)
        //        {
        //            // generate next ID since DB column is not auto-increment
        //            var newId = GetNextIdentity();
        //            var cols = new List<string> { "AlertConfigId" };
        //            var vals = new List<string> { $"@pk{paramIndex}" };
        //            parameters.Add(_dal.CreateParameter($"@pk{paramIndex}", newId));
        //            paramIndex++;
        //
        //            foreach (var change in rowGroup)
        //            {
        //                if (!columnWhitelist.ContainsKey(change.Column))
        //                    continue;
        //
        //                var valParam = $"@val{paramIndex}";
        //                cols.Add(change.Column);
        //                vals.Add(valParam);
        //
        //                object paramValue;
        //                if (string.IsNullOrEmpty(change.NewValue))
        //                    paramValue = DBNull.Value;
        //                else if (change.Column == "IsEmailNotify" || change.Column == "IsMultiAlert")
        //                    paramValue = change.NewValue == "1" ? 1 : 0;
        //                else
        //                    paramValue = change.NewValue;
        //                parameters.Add(_dal.CreateParameter(valParam, paramValue));
        //                paramIndex++;
        //            }
        //
        //            // auto-set CreatedOn to current IST for INSERT; UpdatedOn stays NULL
        //            cols.Add("CreatedOn");
        //            vals.Add($"@istNow{paramIndex}");
        //            parameters.Add(_dal.CreateParameter($"@istNow{paramIndex}", istNow));
        //            paramIndex++;
        //
        //            if (cols.Count == 1) continue;
        //
        //            sql.AppendLine($"    INSERT INTO com_mst_alertconfig ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)});");
        //            // return the generated ID mapped to the client's temp ID
        //            sql.AppendLine($"    SELECT {newId} AS NewId, @tempId{paramIndex} AS TempId, NULL AS ErrorMessage;");
        //            parameters.Add(_dal.CreateParameter($"@tempId{paramIndex}", alertConfigId));
        //            paramIndex++;
        //        }
        //        else
        //        {
        //            foreach (var change in rowGroup)
        //            {
        //                if (!columnWhitelist.ContainsKey(change.Column))
        //                    continue;
        //
        //                var valParam = $"@val{paramIndex}";
        //                var pkParam = $"@pk{paramIndex}";
        //
        //                sql.AppendLine($"    UPDATE com_mst_alertconfig SET {change.Column} = {valParam} WHERE AlertConfigId = {pkParam};");
        //
        //                object paramValue;
        //                if (string.IsNullOrEmpty(change.NewValue))
        //                    paramValue = DBNull.Value;
        //                else if (change.Column == "IsEmailNotify" || change.Column == "IsMultiAlert")
        //                    paramValue = change.NewValue == "1" ? 1 : 0;
        //                else
        //                    paramValue = change.NewValue;
        //                parameters.Add(_dal.CreateParameter(valParam, paramValue));
        //                parameters.Add(_dal.CreateParameter(pkParam, alertConfigId));
        //                paramIndex++;
        //            }
        //
        //            // auto-set UpdatedOn for UPDATE after all column changes
        //            var pkUpd = $"@pkUpd{paramIndex}";
        //            sql.AppendLine($"    UPDATE com_mst_alertconfig SET UpdatedOn = @updatedOn{paramIndex} WHERE AlertConfigId = {pkUpd};");
        //            parameters.Add(_dal.CreateParameter($"@updatedOn{paramIndex}", istNow));
        //            parameters.Add(_dal.CreateParameter(pkUpd, alertConfigId));
        //            paramIndex++;
        //        }
        //    }
        //
        //    try
        //    {
        //        _dal.BeginTransaction();
        //        var dt = _dal.ExecuteQuery(sql.ToString(), parameters.ToArray());
        //        _dal.Commit();
        //
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
        //
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

        public async Task<SaveAlertConfigChangesResult> SaveChangesAsync(List<AlertConfigChange> changes, string mode)
        {
            var result = new SaveAlertConfigChangesResult { Status = "SAVED", IdMappings = new Dictionary<string, int>() };

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

            // OLD: Dictionary whitelist (now uses static HashSet)
            //var columnWhitelist = new Dictionary<string, string> { ... };

            var rows = changes.GroupBy(c => c.AlertConfigId);

            // Capture old values for all UPDATE rows BEFORE the transaction
            var auditEntries = new List<(int RecordId, string OldValues)>();
            foreach (var rowGroup in rows)
            {
                if (rowGroup.Key < 0) continue; // skip new rows
                var oldJson = await _dal.GetRowAsJsonAsync(ReportConfig.AlertConfigTable, "AlertConfigId", rowGroup.Key);
                if (oldJson != null)
                    auditEntries.Add((rowGroup.Key, oldJson));
            }

            var sql = new StringBuilder();
            var parameters = new List<DbParameter>();
            var paramIndex = 0;
            var istNow = _dal.GetCurrentIstTime();

            try
            {
                _dal.BeginTransaction();

                foreach (var rowGroup in rows)
                {
                    var alertConfigId = rowGroup.Key;

                    if (alertConfigId < 0)
                    {
                        // NEW: Generate ID inside the transaction with table lock to prevent race conditions
                        // OLD: var newId = await GetNextIdentityAsync(); (called outside transaction)
                        var lockSql = ReportConfig.WrapLockQuery(ReportConfig.AlertConfigTable, _dal.DatabaseType,
                            $"{_dal.GetNullFunction()}(MAX(AlertConfigId), 0) + 1");
                        var maxId = Convert.ToInt32(await _dal.ExecuteScalarAsync(lockSql));
                        var newId = maxId + 1;

                        var cols = new List<string> { "AlertConfigId" };
                        var vals = new List<string> { $"@pk{paramIndex}" };
                        parameters.Add(_dal.CreateParameter($"@pk{paramIndex}", newId));
                        paramIndex++;

                        foreach (var change in rowGroup)
                        {
                            if (!ColumnWhitelist.Contains(change.Column))
                                continue;

                            var valParam = $"@val{paramIndex}";
                            cols.Add(change.Column);
                            vals.Add(valParam);

                            object paramValue;
                            if (string.IsNullOrEmpty(change.NewValue))
                                paramValue = DBNull.Value;
                            else if (change.Column == "IsEmailNotify" || change.Column == "IsMultiAlert")
                                paramValue = change.NewValue == "1" ? 1 : 0;
                            else
                                paramValue = change.NewValue;
                            parameters.Add(_dal.CreateParameter(valParam, paramValue));
                            paramIndex++;
                        }

                        // auto-set CreatedOn to current IST for INSERT; UpdatedOn stays NULL
                        cols.Add("CreatedOn");
                        vals.Add($"@istNow{paramIndex}");
                        parameters.Add(_dal.CreateParameter($"@istNow{paramIndex}", istNow));
                        paramIndex++;

                        if (cols.Count == 1) continue;

                        sql.AppendLine($"    INSERT INTO {ReportConfig.AlertConfigTable} ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)});");
                        sql.AppendLine($"    SELECT {newId} AS NewId, @tempId{paramIndex} AS TempId, NULL AS ErrorMessage;");
                        parameters.Add(_dal.CreateParameter($"@tempId{paramIndex}", alertConfigId));
                        paramIndex++;
                    }
                    else
                    {
                        // OLD: Separate UPDATE per changed column
                        // NEW: Single UPDATE per row with all columns + UpdatedOn
                        var setClauses = new List<string>();
                        var pkParam = $"@pk{paramIndex}";

                        foreach (var change in rowGroup)
                        {
                            if (!ColumnWhitelist.Contains(change.Column))
                                continue;

                            var valParam = $"@val{paramIndex}";
                            setClauses.Add($"{change.Column} = {valParam}");

                            object paramValue;
                            if (string.IsNullOrEmpty(change.NewValue))
                                paramValue = DBNull.Value;
                            else if (change.Column == "IsEmailNotify" || change.Column == "IsMultiAlert")
                                paramValue = change.NewValue == "1" ? 1 : 0;
                            else
                                paramValue = change.NewValue;
                            parameters.Add(_dal.CreateParameter(valParam, paramValue));
                            paramIndex++;
                        }

                        // auto-set UpdatedOn in the same UPDATE
                        setClauses.Add($"UpdatedOn = @updatedOn{paramIndex}");
                        parameters.Add(_dal.CreateParameter($"@updatedOn{paramIndex}", istNow));
                        paramIndex++;

                        if (setClauses.Count > 0)
                        {
                            sql.AppendLine($"    UPDATE {ReportConfig.AlertConfigTable} SET {string.Join(", ", setClauses)} WHERE AlertConfigId = {pkParam};");
                            parameters.Add(_dal.CreateParameter(pkParam, alertConfigId));
                            paramIndex++;
                        }
                    }
                }

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

                // Log audit entries for all UPDATE rows
                foreach (var entry in auditEntries)
                    await _auditLog.LogChangeAsync(ReportConfig.AlertConfigTable, entry.RecordId, "UPDATE", entry.OldValues);

                return result;
            }
            catch (Exception ex)
            {
                _dal.Rollback();
                result.Status = "ERROR:" + ex.Message;
                return result;
            }
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //public int GetNextIdentity()
        //{
        //    var sql = $"SELECT {_dal.GetNullFunction()}(MAX(AlertConfigId), 0) + 1 FROM com_mst_alertconfig";
        //    return Convert.ToInt32(_dal.ExecuteScalar(sql));
        //}
        #endregion

        public async Task<int> GetNextIdentityAsync()
        {
            var sql = $"SELECT {_dal.GetNullFunction()}(MAX(AlertConfigId), 0) + 1 FROM {ReportConfig.AlertConfigTable}";
            return Convert.ToInt32(await _dal.ExecuteScalarAsync(sql));
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //public string DeleteAlertConfig(int alertConfigId)
        //{
        //    //var depSql = @"
        //    //    SELECT COUNT(*) FROM com_mst_reportfilteringcolumn 
        //    //    WHERE ReportId = (SELECT ReportId FROM com_mst_alertconfig WHERE AlertConfigId = @id)
        //    //    HAVING COUNT(*) > 0";
        //
        //    //var depDt = _dal.ExecuteQuery(depSql, _dal.CreateParameter("@id", alertConfigId));
        //
        //    //if (depDt.Rows.Count > 0)
        //    //{
        //    //    var count = Convert.ToInt32(depDt.Rows[0][0]);
        //    //    return $"DEPENDENT:Filtering Columns:{count}";
        //    //}
        //
        //    var depCount = Convert.ToInt32(_dal.ExecuteScalar(
        //        "SELECT COUNT(*) FROM com_mst_reportfilteringcolumn WHERE ReportId = (SELECT ReportId FROM com_mst_alertconfig WHERE AlertConfigId = @id)",
        //        _dal.CreateParameter("@id", alertConfigId)));
        //    if (depCount > 0)
        //        return $"DEPENDENT:Filtering Columns:{depCount}";
        //
        //    var rowsAffected = _dal.ExecuteNonQuery(
        //        "DELETE FROM com_mst_alertconfig WHERE AlertConfigId = @id",
        //        _dal.CreateParameter("@id", alertConfigId));
        //    return rowsAffected > 0 ? "DELETED" : "NOT_FOUND";
        //}
        #endregion

        public async Task<string> DeleteAlertConfigAsync(int alertConfigId)
        {
            // Capture old values before DELETE
            var oldJson = await _dal.GetRowAsJsonAsync(ReportConfig.AlertConfigTable, "AlertConfigId", alertConfigId);

            var depCount = Convert.ToInt32(await _dal.ExecuteScalarAsync(
                $"SELECT COUNT(*) FROM {ReportConfig.ReportFilteringColumnTable} WHERE ReportId = (SELECT ReportId FROM {ReportConfig.AlertConfigTable} WHERE AlertConfigId = @id)",
                _dal.CreateParameter("@id", alertConfigId)));
            if (depCount > 0)
                return $"DEPENDENT:Filtering Columns:{depCount}";

            var rowsAffected = await _dal.ExecuteNonQueryAsync(
                $"DELETE FROM {ReportConfig.AlertConfigTable} WHERE AlertConfigId = @id",
                _dal.CreateParameter("@id", alertConfigId));

            if (rowsAffected > 0)
                await _auditLog.LogChangeAsync(ReportConfig.AlertConfigTable, alertConfigId, "DELETE", oldJson);

            return rowsAffected > 0 ? "DELETED" : "NOT_FOUND";
        }

        private static string GetSortColumnExpression(string sortColumn)
        {
            switch (sortColumn)
            {
                case "AlertName": return "AlertName";
                case "ReportId": return "ReportId";
                case "IsEmailNotify": return "IsEmailNotify";
                case "EmailSubject": return "EmailSubject";
                case "EmailContentHeader": return "EmailContentHeader";
                case "EmailContentUrl": return "EmailContentUrl";
                case "AttachmentFileTypeId": return "AttachmentFileTypeId";
                case "EmailAttachmentUrl": return "EmailAttachmentUrl";
                case "StatusId": return "StatusId";
                case "AlertTypeId": return "AlertTypeId";
                case "IsMultiAlert": return "IsMultiAlert";
                case "MultiAlertQuery": return "MultiAlertQuery";
                case "EmailContentFooter": return "EmailContentFooter";
                case "CreatedBy": return "CreatedBy";
                case "CreatedOn": return "CreatedOn";
                case "UpdatedBy": return "UpdatedBy";
                case "UpdatedOn": return "UpdatedOn";
                default: return "AlertConfigId";
            }
        }
    }
}
