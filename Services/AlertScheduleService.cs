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
    public class AlertScheduleService
    {
        private readonly IDal _dal;
        private readonly AuditLogService _auditLog;

        private static readonly HashSet<string> ColumnWhitelist = new HashSet<string>
        {
            "AlertConfigId", "ToEmail", "FrequencyHours",
            "NextScheduleDate", "BCCEmail", "CCEmail"
        };

        public AlertScheduleService() : this(new Dal())
        {
        }

        public AlertScheduleService(IDal dal)
        {
            _dal = dal;
            _auditLog = new AuditLogService(dal);
        }

        #region OLD_SYNC
        //public AlertScheduleGridViewModel GetAlertSchedules(AlertScheduleGridViewModel filter)
        //{
        //    if (filter == null)
        //        filter = new AlertScheduleGridViewModel();

        //    var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        //    var pageSize = 7;
        //    if (pageSize < 10) pageSize = 7;
        //    if (pageSize > 100) pageSize = 100;

        //    var parameters = new List<DbParameter>();
        //    var where = new StringBuilder(" WHERE 1=1");

        //    if (!string.IsNullOrWhiteSpace(filter.SearchText))
        //    {
        //        string search = filter.SearchText.Trim();
        //        where.Append(" AND (ToEmail LIKE @search OR BCCEmail LIKE @search OR CCEmail LIKE @search)");
        //        parameters.Add(_dal.CreateParameter("@search", $"%{search}%"));
        //    }

        //    if (filter.FilterAlertConfigId > 0)
        //    {
        //        where.Append(" AND AlertConfigId = @filterAlertConfigId");
        //        parameters.Add(_dal.CreateParameter("@filterAlertConfigId", filter.FilterAlertConfigId));
        //    }

        //    string countSql = $"SELECT COUNT(*) FROM com_mst_alertschedule{where}";
        //    filter.TotalRecords = Convert.ToInt32(_dal.ExecuteScalar(countSql, parameters.ToArray()));

        //    string sortColumn = GetSortColumnExpression(filter.SortColumn);
        //    string sortDir = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        //    int offset = (pageNumber - 1) * pageSize;

        //    var dataParams = parameters.Select(p => _dal.CreateParameter(p.ParameterName, p.Value)).ToList<DbParameter>();
        //    string sql = $@"
        //        SELECT AlertScheduleId, AlertConfigId, ToEmail, FrequencyHours,
        //               NextScheduleDate, BCCEmail, CCEmail
        //        FROM com_mst_alertschedule{where}
        //        ORDER BY {sortColumn} {sortDir}
        //        {_dal.GetPaginationClause(offset, pageSize)}";

        //    DataTable dt = _dal.ExecuteQuery(sql, dataParams.ToArray());

        //    var items = new List<AlertScheduleGridItemViewModel>();
        //    foreach (DataRow row in dt.Rows)
        //    {
        //        var item = new AlertScheduleGridItemViewModel
        //        {
        //            AlertScheduleId = row["AlertScheduleId"] == DBNull.Value ? 0 : Convert.ToInt32(row["AlertScheduleId"]),
        //            AlertConfigId = row["AlertConfigId"] == DBNull.Value ? 0 : Convert.ToInt32(row["AlertConfigId"]),
        //            ToEmail = row["ToEmail"] == DBNull.Value ? "" : row["ToEmail"].ToString(),
        //            FrequencyHours = row["FrequencyHours"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["FrequencyHours"]),
        //            NextScheduleDate = row["NextScheduleDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["NextScheduleDate"]),
        //            BCCEmail = row["BCCEmail"] == DBNull.Value ? "" : row["BCCEmail"].ToString(),
        //            CCEmail = row["CCEmail"] == DBNull.Value ? "" : row["CCEmail"].ToString()
        //        };
        //        items.Add(item);
        //    }

        //    filter.Items = items;
        //    filter.PageNumber = pageNumber;
        //    filter.PageSize = pageSize;
        //    return filter;
        //}
        #endregion

        public async Task<AlertScheduleGridViewModel> GetAlertSchedulesAsync(AlertScheduleGridViewModel filter)
        {
            if (filter == null)
                filter = new AlertScheduleGridViewModel();

            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = ReportConfig.AlertSchedulePageSize;
            if (pageSize > ReportConfig.MaxPageSize) pageSize = ReportConfig.MaxPageSize;

            var parameters = new List<DbParameter>();
            var where = new StringBuilder(" WHERE 1=1");

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                string search = filter.SearchText.Trim();
                where.Append(" AND (ToEmail LIKE @search OR BCCEmail LIKE @search OR CCEmail LIKE @search)");
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
                SELECT COUNT(*) OVER() AS TotalRecords, AlertScheduleId, AlertConfigId, ToEmail, FrequencyHours,
                       NextScheduleDate, BCCEmail, CCEmail
FROM {ReportConfig.AlertScheduleTable}{where}
                ORDER BY {sortColumn} {sortDir}
                {_dal.GetPaginationClause(offset, pageSize)}";

            DataTable dt = await _dal.ExecuteQueryAsync(sql, parameters.ToArray());

            filter.TotalRecords = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["TotalRecords"]) : 0;

            var items = new List<AlertScheduleGridItemViewModel>();
            foreach (DataRow row in dt.Rows)
            {
                var item = new AlertScheduleGridItemViewModel
                {
                    AlertScheduleId = row["AlertScheduleId"] == DBNull.Value ? 0 : Convert.ToInt32(row["AlertScheduleId"]),
                    AlertConfigId = row["AlertConfigId"] == DBNull.Value ? 0 : Convert.ToInt32(row["AlertConfigId"]),
                    ToEmail = row["ToEmail"] == DBNull.Value ? "" : row["ToEmail"].ToString(),
                    FrequencyHours = row["FrequencyHours"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["FrequencyHours"]),
                    NextScheduleDate = row["NextScheduleDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["NextScheduleDate"]),
                    BCCEmail = row["BCCEmail"] == DBNull.Value ? "" : row["BCCEmail"].ToString(),
                    CCEmail = row["CCEmail"] == DBNull.Value ? "" : row["CCEmail"].ToString()
                };
                items.Add(item);
            }

            filter.Items = items;
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
        //public SaveAlertScheduleChangesResult SaveChanges(List<AlertScheduleChange> changes, string mode)
        //{
        //    var result = new SaveAlertScheduleChangesResult { Status = "SAVED", IdMappings = new Dictionary<string, int>() };

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
        //        { "ToEmail", "ToEmail" },
        //        { "FrequencyHours", "FrequencyHours" },
        //        { "NextScheduleDate", "NextScheduleDate" },
        //        { "BCCEmail", "BCCEmail" },
        //        { "CCEmail", "CCEmail" }
        //    };

        //    var rows = changes.GroupBy(c => c.AlertScheduleId);

        //    var sql = new StringBuilder();

        //    var parameters = new List<DbParameter>();
        //    var paramIndex = 0;

        //    foreach (var rowGroup in rows)
        //    {
        //        var alertScheduleId = rowGroup.Key;

        //        if (alertScheduleId < 0)
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

        //            sql.AppendLine($"    INSERT INTO com_mst_alertschedule ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)});");
        //            sql.AppendLine($"    SELECT CAST({_dal.GetIdentityFunction()} AS {_dal.GetIdentityCastType()}) AS NewId, @tempId{paramIndex} AS TempId, NULL AS ErrorMessage;");
        //            parameters.Add(_dal.CreateParameter($"@tempId{paramIndex}", alertScheduleId));
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

        //                sql.AppendLine($"    UPDATE com_mst_alertschedule SET {change.Column} = {valParam} WHERE AlertScheduleId = {pkParam};");

        //                object paramValue = string.IsNullOrEmpty(change.NewValue) ? DBNull.Value : (object)change.NewValue;
        //                parameters.Add(_dal.CreateParameter(valParam, paramValue));
        //                parameters.Add(_dal.CreateParameter(pkParam, alertScheduleId));
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

        public async Task<SaveAlertScheduleChangesResult> SaveChangesAsync(List<AlertScheduleChange> changes, string mode)
        {
            var result = new SaveAlertScheduleChangesResult { Status = "SAVED", IdMappings = new Dictionary<string, int>() };

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

            // whitelist check using static ColumnWhitelist HashSet

            var rows = changes.GroupBy(c => c.AlertScheduleId);

            var sql = new StringBuilder();

            var parameters = new List<DbParameter>();
            var paramIndex = 0;

            foreach (var rowGroup in rows)
            {
                var alertScheduleId = rowGroup.Key;

                if (alertScheduleId < 0)
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

                    sql.AppendLine($"    INSERT INTO {ReportConfig.AlertScheduleTable} ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)});");
                    sql.AppendLine($"    SELECT CAST({_dal.GetIdentityFunction()} AS {_dal.GetIdentityCastType()}) AS NewId, @tempId{paramIndex} AS TempId, NULL AS ErrorMessage;");
                    parameters.Add(_dal.CreateParameter($"@tempId{paramIndex}", alertScheduleId));
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

                    if (setClauses.Count > 0)
                    {
                        var pkParam = $"@pk{paramIndex}";
                        sql.AppendLine($"    UPDATE {ReportConfig.AlertScheduleTable} SET {string.Join(", ", setClauses)} WHERE AlertScheduleId = {pkParam};");
                        parameters.Add(_dal.CreateParameter(pkParam, alertScheduleId));
                        paramIndex++;
                    }
                }
            }

            var auditEntries = new List<(int RecordId, string OldValues)>();
            foreach (var rowGroup in rows)
            {
                if (rowGroup.Key < 0) continue;
                var oldJson = await _dal.GetRowAsJsonAsync(ReportConfig.AlertScheduleTable, "AlertScheduleId", rowGroup.Key);
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
                    await _auditLog.LogChangeAsync(ReportConfig.AlertScheduleTable, entry.RecordId, "UPDATE", entry.OldValues);

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
        //public int GetNextIdentity()
        //{
        //    var sql = $"SELECT {_dal.GetNullFunction()}(MAX(AlertScheduleId), 0) + 1 FROM com_mst_alertschedule";
        //    return Convert.ToInt32(_dal.ExecuteScalar(sql));
        //}
        #endregion

        public async Task<int> GetNextIdentityAsync()
        {
            var sql = $"SELECT {_dal.GetNullFunction()}(MAX(AlertScheduleId), 0) + 1 FROM {ReportConfig.AlertScheduleTable}";
            return Convert.ToInt32(await _dal.ExecuteScalarAsync(sql));
        }

        #region OLD_SYNC
        //public string DeleteAlertSchedule(int alertScheduleId)
        //{
        //    var rowsAffected = _dal.ExecuteNonQuery(
        //        "DELETE FROM com_mst_alertschedule WHERE AlertScheduleId = @id",
        //        _dal.CreateParameter("@id", alertScheduleId));
        //    return rowsAffected > 0 ? "DELETED" : "NOT_FOUND";
        //}
        #endregion

        public async Task<string> DeleteAlertScheduleAsync(int alertScheduleId)
        {
            var oldJson = await _dal.GetRowAsJsonAsync(ReportConfig.AlertScheduleTable, "AlertScheduleId", alertScheduleId);
            var rowsAffected = await _dal.ExecuteNonQueryAsync(
                $"DELETE FROM {ReportConfig.AlertScheduleTable} WHERE AlertScheduleId = @id",
                _dal.CreateParameter("@id", alertScheduleId));
            if (rowsAffected > 0)
                await _auditLog.LogChangeAsync(ReportConfig.AlertScheduleTable, alertScheduleId, "DELETE", oldJson);
            return rowsAffected > 0 ? "DELETED" : "NOT_FOUND";
        }

        private static string GetSortColumnExpression(string sortColumn)
        {
            switch (sortColumn)
            {
                case "AlertConfigId": return "AlertConfigId";
                case "ToEmail": return "ToEmail";
                case "FrequencyHours": return "FrequencyHours";
                case "NextScheduleDate": return "NextScheduleDate";
                case "BCCEmail": return "BCCEmail";
                case "CCEmail": return "CCEmail";
                default: return "AlertScheduleId";
            }
        }
    }
}