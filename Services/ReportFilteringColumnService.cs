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
    public class ReportFilteringColumnService
    {
        private readonly IDal _dal;
        private readonly AuditLogService _auditLog;
        private static readonly HashSet<string> ColumnWhitelist = new HashSet<string>
        {
            "ReportColumnId", "ReportId", "DataType", "Operator",
            "Value1", "Value2", "Condn", "IsDisable"
        };

        public ReportFilteringColumnService() : this(new Dal())
        {
        }

        public ReportFilteringColumnService(IDal dal)
        {
            _dal = dal;
            _auditLog = new AuditLogService(dal);
        }

        #region OLD_SYNC
        //public ReportFilteringColumnGridViewModel GetReportFilteringColumns(ReportFilteringColumnGridViewModel filter)
        //{
        //    if (filter == null)
        //        filter = new ReportFilteringColumnGridViewModel();

        //    var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        //    var pageSize = 7;
        //    if (pageSize < 10) pageSize = 7;
        //    if (pageSize > 100) pageSize = 100;

        //    var parameters = new List<DbParameter>();
        //    var where = new StringBuilder(" WHERE 1=1");

        //    if (filter.FilterReportId > 0)
        //    {
        //        where.Append(" AND ReportId = @filterReportId");
        //        parameters.Add(_dal.CreateParameter("@filterReportId", filter.FilterReportId));
        //    }

        //    if (!string.IsNullOrWhiteSpace(filter.SearchText))
        //    {
        //        string search = filter.SearchText.Trim();
        //        where.Append(" AND (DataType LIKE @search OR Operator LIKE @search OR Value1 LIKE @search OR Value2 LIKE @search OR Condn LIKE @search)");
        //        parameters.Add(_dal.CreateParameter("@search", $"%{search}%"));
        //    }

        //    string countSql = $"SELECT COUNT(*) FROM com_mst_reportfilteringcolumn{where}";
        //    filter.TotalRecords = Convert.ToInt32(_dal.ExecuteScalar(countSql, parameters.ToArray()));

        //    string sortColumn = GetSortColumnExpression(filter.SortColumn);
        //    string sortDir = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        //    int offset = (pageNumber - 1) * pageSize;

        //    string sql = $@"
        //        SELECT ReportFilterColumnId, ReportColumnId, ReportId, DataType, Operator,
        //               Value1, Value2, Condn, IsDisable
        //        FROM com_mst_reportfilteringcolumn{where}
        //        ORDER BY {sortColumn} {sortDir}
        //        {_dal.GetPaginationClause(offset, pageSize)}";

        //    var dataParams = parameters.Select(p => _dal.CreateParameter(p.ParameterName, p.Value)).ToList<DbParameter>();
        //    DataTable dt = _dal.ExecuteQuery(sql, dataParams.ToArray());

        //    var items = new List<ReportFilteringColumnGridItemViewModel>();

        //    foreach (DataRow row in dt.Rows)
        //    {
        //        var item = new ReportFilteringColumnGridItemViewModel();

        //        item.ReportFilterColumnId = row["ReportFilterColumnId"] == DBNull.Value ? 0 : Convert.ToInt32(row["ReportFilterColumnId"]);
        //        item.ReportColumnId = row["ReportColumnId"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["ReportColumnId"]);
        //        item.ReportId = row["ReportId"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["ReportId"]);
        //        item.DataType = row["DataType"] == DBNull.Value ? "" : row["DataType"].ToString();
        //        item.Operator = row["Operator"] == DBNull.Value ? "" : row["Operator"].ToString();
        //        item.Value1 = row["Value1"] == DBNull.Value ? "" : row["Value1"].ToString();
        //        item.Value2 = row["Value2"] == DBNull.Value ? "" : row["Value2"].ToString();
        //        item.Condn = row["Condn"] == DBNull.Value ? "" : row["Condn"].ToString();
        //        item.IsDisable = row["IsDisable"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row["IsDisable"]);

        //        items.Add(item);
        //    }

        //    filter.Items = items;
        //    filter.PageNumber = pageNumber;
        //    filter.PageSize = pageSize;

        //    return filter;
        //}
        #endregion

        public async Task<ReportFilteringColumnGridViewModel> GetReportFilteringColumnsAsync(ReportFilteringColumnGridViewModel filter)
        {
            if (filter == null)
                filter = new ReportFilteringColumnGridViewModel();

            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = ReportConfig.ReportFilteringColumnPageSize;
            if (pageSize > ReportConfig.MaxPageSize) pageSize = ReportConfig.MaxPageSize;

            var parameters = new List<DbParameter>();
            var where = new StringBuilder(" WHERE 1=1");

            if (filter.FilterReportId > 0)
            {
                where.Append(" AND ReportId = @filterReportId");
                parameters.Add(_dal.CreateParameter("@filterReportId", filter.FilterReportId));
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                string search = filter.SearchText.Trim();
                where.Append(" AND (DataType LIKE @search OR Operator LIKE @search OR Value1 LIKE @search OR Value2 LIKE @search OR Condn LIKE @search)");
                parameters.Add(_dal.CreateParameter("@search", $"%{search}%"));
            }

            string sortColumn = GetSortColumnExpression(filter.SortColumn);
            string sortDir = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            int offset = (pageNumber - 1) * pageSize;

            string sql = $@"
                SELECT ReportFilterColumnId, ReportColumnId, ReportId, DataType, Operator,
                       Value1, Value2, Condn, IsDisable, COUNT(*) OVER() AS TotalRecords
                FROM {ReportConfig.ReportFilteringColumnTable}{where}
                ORDER BY {sortColumn} {sortDir}
                {_dal.GetPaginationClause(offset, pageSize)}";

            DataTable dt = await _dal.ExecuteQueryAsync(sql, parameters.ToArray());

            filter.TotalRecords = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["TotalRecords"]) : 0;

            var items = new List<ReportFilteringColumnGridItemViewModel>();

            foreach (DataRow row in dt.Rows)
            {
                var item = new ReportFilteringColumnGridItemViewModel();

                item.ReportFilterColumnId = row["ReportFilterColumnId"] == DBNull.Value ? 0 : Convert.ToInt32(row["ReportFilterColumnId"]);
                item.ReportColumnId = row["ReportColumnId"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["ReportColumnId"]);
                item.ReportId = row["ReportId"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["ReportId"]);
                item.DataType = row["DataType"] == DBNull.Value ? "" : row["DataType"].ToString();
                item.Operator = row["Operator"] == DBNull.Value ? "" : row["Operator"].ToString();
                item.Value1 = row["Value1"] == DBNull.Value ? "" : row["Value1"].ToString();
                item.Value2 = row["Value2"] == DBNull.Value ? "" : row["Value2"].ToString();
                item.Condn = row["Condn"] == DBNull.Value ? "" : row["Condn"].ToString();
                item.IsDisable = row["IsDisable"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(row["IsDisable"]);

                items.Add(item);
            }

            filter.Items = items;
            filter.PageNumber = pageNumber;
            filter.PageSize = pageSize;

            return filter;
        }

        #region OLD_SYNC
        //public SaveFilteringColumnChangesResult SaveChanges(List<ReportFilteringColumnChange> changes, string mode)
        //{
        //    var result = new SaveFilteringColumnChangesResult { Status = "SAVED", IdMappings = new Dictionary<string, int>() };

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
        //        { "ReportColumnId", "ReportColumnId" },
        //        { "ReportId", "ReportId" },
        //        { "DataType", "DataType" },
        //        { "Operator", "Operator" },
        //        { "Value1", "Value1" },
        //        { "Value2", "Value2" },
        //        { "Condn", "Condn" },
        //        { "IsDisable", "IsDisable" }
        //    };

        //    var rows = changes.GroupBy(c => c.ReportFilterColumnId);

        //    var sql = new StringBuilder();

        //    var parameters = new List<DbParameter>();
        //    var paramIndex = 0;

        //    foreach (var rowGroup in rows)
        //    {
        //        var reportFilterColumnId = rowGroup.Key;

        //        if (reportFilterColumnId < 0)
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

        //                object paramValue;
        //                if (string.IsNullOrEmpty(change.NewValue))
        //                    paramValue = DBNull.Value;
        //                else if (change.Column == "IsDisable")
        //                    paramValue = change.NewValue == "1" ? 1 : 0;
        //                else
        //                    paramValue = change.NewValue;
        //                parameters.Add(_dal.CreateParameter(valParam, paramValue));
        //                paramIndex++;
        //            }

        //            if (cols.Count == 0) continue;

        //            sql.AppendLine($"    INSERT INTO com_mst_reportfilteringcolumn ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)});");
        //            sql.AppendLine($"    SELECT CAST({_dal.GetIdentityFunction()} AS {_dal.GetIdentityCastType()}) AS NewId, @tempId{paramIndex} AS TempId, NULL AS ErrorMessage;");
        //            parameters.Add(_dal.CreateParameter($"@tempId{paramIndex}", reportFilterColumnId));
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

        //                sql.AppendLine($"    UPDATE com_mst_reportfilteringcolumn SET {change.Column} = {valParam} WHERE ReportFilterColumnId = {pkParam};");

        //                object paramValue;
        //                if (string.IsNullOrEmpty(change.NewValue))
        //                    paramValue = DBNull.Value;
        //                else if (change.Column == "IsDisable")
        //                    paramValue = change.NewValue == "1" ? 1 : 0;
        //                else
        //                    paramValue = change.NewValue;
        //                parameters.Add(_dal.CreateParameter(valParam, paramValue));
        //                parameters.Add(_dal.CreateParameter(pkParam, reportFilterColumnId));
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

        public async Task<SaveFilteringColumnChangesResult> SaveChangesAsync(List<ReportFilteringColumnChange> changes, string mode)
        {
            var result = new SaveFilteringColumnChangesResult { Status = "SAVED", IdMappings = new Dictionary<string, int>() };

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

            // Whitelist checked via ColumnWhitelist

            var rows = changes.GroupBy(c => c.ReportFilterColumnId);

            var sql = new StringBuilder();

            var parameters = new List<DbParameter>();
            var paramIndex = 0;

            foreach (var rowGroup in rows)
            {
                var reportFilterColumnId = rowGroup.Key;

                if (reportFilterColumnId < 0)
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

                        object paramValue;
                        if (string.IsNullOrEmpty(change.NewValue))
                            paramValue = DBNull.Value;
                        else if (change.Column == "IsDisable")
                            paramValue = change.NewValue == "1" ? 1 : 0;
                        else
                            paramValue = change.NewValue;
                        parameters.Add(_dal.CreateParameter(valParam, paramValue));
                        paramIndex++;
                    }

                    if (cols.Count == 0) continue;

                    sql.AppendLine($"    INSERT INTO {ReportConfig.ReportFilteringColumnTable} ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)});");
                    sql.AppendLine($"    SELECT CAST({_dal.GetIdentityFunction()} AS {_dal.GetIdentityCastType()}) AS NewId, @tempId{paramIndex} AS TempId, NULL AS ErrorMessage;");
                    parameters.Add(_dal.CreateParameter($"@tempId{paramIndex}", reportFilterColumnId));
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

                        object paramValue;
                        if (string.IsNullOrEmpty(change.NewValue))
                            paramValue = DBNull.Value;
                        else if (change.Column == "IsDisable")
                            paramValue = change.NewValue == "1" ? 1 : 0;
                        else
                            paramValue = change.NewValue;
                        parameters.Add(_dal.CreateParameter(valParam, paramValue));
                        paramIndex++;
                    }

                    if (setClauses.Count == 0) continue;

                    var pkParam = $"@pk{paramIndex}";
                    sql.AppendLine($"    UPDATE {ReportConfig.ReportFilteringColumnTable} SET {string.Join(", ", setClauses)} WHERE ReportFilterColumnId = {pkParam};");
                    parameters.Add(_dal.CreateParameter(pkParam, reportFilterColumnId));
                    paramIndex++;
                }
            }

            var auditEntries = new List<(int RecordId, string OldValues)>();
            foreach (var rowGroup in rows)
            {
                if (rowGroup.Key < 0) continue;
                var oldJson = await _dal.GetRowAsJsonAsync(ReportConfig.ReportFilteringColumnTable, "ReportFilterColumnId", rowGroup.Key);
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
                    await _auditLog.LogChangeAsync(ReportConfig.ReportFilteringColumnTable, entry.RecordId, "UPDATE", entry.OldValues);

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
        //    var sql = $"SELECT {_dal.GetNullFunction()}(MAX(ReportFilterColumnId), 0) + 1 FROM com_mst_reportfilteringcolumn";
        //    return Convert.ToInt32(_dal.ExecuteScalar(sql));
        //}
        #endregion

        public async Task<int> GetNextIdentityAsync()
        {
            var sql = $"SELECT {_dal.GetNullFunction()}(MAX(ReportFilterColumnId), 0) + 1 FROM {ReportConfig.ReportFilteringColumnTable}";
            return Convert.ToInt32(await _dal.ExecuteScalarAsync(sql));
        }

        #region OLD_SYNC
        //public string DeleteReportFilteringColumn(int reportFilterColumnId)
        //{
        //    var rowsAffected = _dal.ExecuteNonQuery(
        //        "DELETE FROM com_mst_reportfilteringcolumn WHERE ReportFilterColumnId = @id",
        //        _dal.CreateParameter("@id", reportFilterColumnId));
        //    return rowsAffected > 0 ? "DELETED" : "NOT_FOUND";
        //}
        #endregion

        public async Task<string> DeleteReportFilteringColumnAsync(int reportFilterColumnId)
        {
            var oldJson = await _dal.GetRowAsJsonAsync(ReportConfig.ReportFilteringColumnTable, "ReportFilterColumnId", reportFilterColumnId);

            var rowsAffected = await _dal.ExecuteNonQueryAsync(
                $"DELETE FROM {ReportConfig.ReportFilteringColumnTable} WHERE ReportFilterColumnId = @id",
                _dal.CreateParameter("@id", reportFilterColumnId));
            if (rowsAffected > 0)
                await _auditLog.LogChangeAsync(ReportConfig.ReportFilteringColumnTable, reportFilterColumnId, "DELETE", oldJson);

            return rowsAffected > 0 ? "DELETED" : "NOT_FOUND";
        }

        private static string GetSortColumnExpression(string sortColumn)
        {
            switch (sortColumn)
            {
                case "ReportColumnId": return "ReportColumnId";
                case "ReportId": return "ReportId";
                case "IsDisable": return "IsDisable";
                default: return "ReportFilterColumnId";
            }
        }
    }
}
