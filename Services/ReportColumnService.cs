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
    public class ReportColumnService
    {
        private readonly IDal _dal;
        private readonly AuditLogService _auditLog;

        private static readonly HashSet<string> ColumnWhitelist = new HashSet<string>
        {
            "DisplayColumnName", "TableColumnName", "Datatype",
            "IsDefaultColumn", "IsSqlParameter", "ReportId",
            "GroupType", "GroupIndex", "SearchQuery", "SrNo",
            "IsFilterColumn", "MainReportColumnId"
        };

        public ReportColumnService() : this(new Dal())
        {
        }

        public ReportColumnService(IDal dal)
        {
            _dal = dal;
            _auditLog = new AuditLogService(dal);
        }

        //#region OLD_SYNC
        //public ReportColumnGridViewModel GetReportColumns(ReportColumnGridViewModel filter)
        //{
        //    if (filter == null)
        //        filter = new ReportColumnGridViewModel();
        //
        //    var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        //    var pageSize = 7;
        //    if (pageSize < 10) pageSize = 7;
        //    if (pageSize > 100) pageSize = 100;
        //
        //    var parameters = new List<DbParameter>();
        //    var where = new StringBuilder(" WHERE 1=1");
        //
        //    if (filter.FilterReportId > 0)
        //    {
        //        where.Append(" AND rc.ReportId = @filterReportId");
        //        parameters.Add(_dal.CreateParameter("@filterReportId", filter.FilterReportId));
        //    }
        //
        //    if (!string.IsNullOrWhiteSpace(filter.SearchText))
        //    {
        //        string search = filter.SearchText.Trim();
        //        where.Append(" AND (rc.DisplayColumnName LIKE @search OR rc.TableColumnName LIKE @search OR rc.Datatype LIKE @search OR rc.SearchQuery LIKE @search)");
        //        parameters.Add(_dal.CreateParameter("@search", $"%{search}%"));
        //    }
        //
        //    string countSql = $"SELECT COUNT(*) FROM com_mst_reportcolumn rc INNER JOIN com_mst_report r ON rc.ReportId = r.ReportId {where}";
        //    filter.TotalRecords = Convert.ToInt32(_dal.ExecuteScalar(countSql, parameters.ToArray()));
        //
        //    string sortColumn = GetSortColumnExpression(filter.SortColumn);
        //    string sortDir = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        //    int offset = (pageNumber - 1) * pageSize;
        //
        //    var dataParams = parameters.Select(p => _dal.CreateParameter(p.ParameterName, p.Value)).ToList<DbParameter>();
        //    string sql = $@"
        //            SELECT rc.ReportColumnId, rc.DisplayColumnName, rc.TableColumnName, rc.Datatype,
        //                   rc.IsDefaultColumn, rc.IsSqlParameter, rc.ReportId, rc.GroupType, rc.GroupIndex,
        //                   rc.SearchQuery, rc.SrNo, rc.IsFilterColumn, rc.MainReportColumnId
        //            FROM com_mst_reportcolumn rc
        //            INNER JOIN com_mst_report r ON rc.ReportId = r.ReportId
        //            {where}
        //            ORDER BY {sortColumn} {sortDir}
        //            {_dal.GetPaginationClause(offset, pageSize)}";
        //
        //    DataTable dt = _dal.ExecuteQuery(sql, dataParams.ToArray());
        //
        //    var items = new List<ReportColumnGridItemViewModel>();
        //
        //    foreach (DataRow row in dt.Rows)
        //    {
        //        var item = new ReportColumnGridItemViewModel();
        //
        //        item.ReportColumnId = row["ReportColumnId"] == DBNull.Value ? 0 : Convert.ToInt32(row["ReportColumnId"]);
        //        item.DisplayColumnName = row["DisplayColumnName"] == DBNull.Value ? "" : row["DisplayColumnName"].ToString();
        //        item.TableColumnName = row["TableColumnName"] == DBNull.Value ? "" : row["TableColumnName"].ToString();
        //        item.Datatype = row["Datatype"] == DBNull.Value ? "" : row["Datatype"].ToString();
        //        item.IsDefaultColumn = row["IsDefaultColumn"] == DBNull.Value ? false : Convert.ToBoolean(row["IsDefaultColumn"]);
        //        item.IsSqlParameter = row["IsSqlParameter"] == DBNull.Value ? false : Convert.ToBoolean(row["IsSqlParameter"]);
        //        item.ReportId = row["ReportId"] == DBNull.Value ? 0 : Convert.ToInt32(row["ReportId"]);
        //        item.GroupType = row["GroupType"] == DBNull.Value ? "" : row["GroupType"].ToString();
        //        item.GroupIndex = row["GroupIndex"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["GroupIndex"]);
        //        item.SearchQuery = row["SearchQuery"] == DBNull.Value ? "" : row["SearchQuery"].ToString();
        //        item.SrNo = row["SrNo"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["SrNo"]);
        //        item.IsFilterColumn = row["IsFilterColumn"] == DBNull.Value ? false : Convert.ToBoolean(row["IsFilterColumn"]);
        //        item.MainReportColumnId = row["MainReportColumnId"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["MainReportColumnId"]);
        //
        //        items.Add(item);
        //    }
        //
        //    filter.Items = items;
        //    filter.PageNumber = pageNumber;
        //    filter.PageSize = pageSize;
        //
        //    return filter;
        //}
        //#endregion

        public async Task<ReportColumnGridViewModel> GetReportColumnsAsync(ReportColumnGridViewModel filter)
        {
            if (filter == null)
                filter = new ReportColumnGridViewModel();

            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = ReportConfig.ReportColumnPageSize;
            if (pageSize > ReportConfig.MaxPageSize) pageSize = ReportConfig.MaxPageSize;

            var parameters = new List<DbParameter>();
            var where = new StringBuilder(" WHERE 1=1");

            if (filter.FilterReportId > 0)
            {
                where.Append(" AND rc.ReportId = @filterReportId");
                parameters.Add(_dal.CreateParameter("@filterReportId", filter.FilterReportId));
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                string search = filter.SearchText.Trim();
                where.Append(" AND (rc.DisplayColumnName LIKE @search OR rc.TableColumnName LIKE @search OR rc.Datatype LIKE @search OR rc.SearchQuery LIKE @search)");
                parameters.Add(_dal.CreateParameter("@search", $"%{search}%"));
            }

            string sortColumn = GetSortColumnExpression(filter.SortColumn);
            string sortDir = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            int offset = (pageNumber - 1) * pageSize;

            string sql = $@"
                    SELECT rc.ReportColumnId, rc.DisplayColumnName, rc.TableColumnName, rc.Datatype,
                           rc.IsDefaultColumn, rc.IsSqlParameter, rc.ReportId, rc.GroupType, rc.GroupIndex,
                           rc.SearchQuery, rc.SrNo, rc.IsFilterColumn, rc.MainReportColumnId,
                           COUNT(*) OVER() AS TotalRecords
                    FROM {ReportConfig.ReportColumnTable} rc
                    INNER JOIN {ReportConfig.ReportTable} r ON rc.ReportId = r.ReportId
                    {where}
                    ORDER BY {sortColumn} {sortDir}
                    {_dal.GetPaginationClause(offset, pageSize)}";

            DataTable dt = await _dal.ExecuteQueryAsync(sql, parameters.ToArray());

            filter.TotalRecords = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["TotalRecords"]) : 0;

            var items = new List<ReportColumnGridItemViewModel>();

            foreach (DataRow row in dt.Rows)
            {
                var item = new ReportColumnGridItemViewModel();

                item.ReportColumnId = row["ReportColumnId"] == DBNull.Value ? 0 : Convert.ToInt32(row["ReportColumnId"]);
                item.DisplayColumnName = row["DisplayColumnName"] == DBNull.Value ? "" : row["DisplayColumnName"].ToString();
                item.TableColumnName = row["TableColumnName"] == DBNull.Value ? "" : row["TableColumnName"].ToString();
                item.Datatype = row["Datatype"] == DBNull.Value ? "" : row["Datatype"].ToString();
                item.IsDefaultColumn = row["IsDefaultColumn"] == DBNull.Value ? false : Convert.ToBoolean(row["IsDefaultColumn"]);
                item.IsSqlParameter = row["IsSqlParameter"] == DBNull.Value ? false : Convert.ToBoolean(row["IsSqlParameter"]);
                item.ReportId = row["ReportId"] == DBNull.Value ? 0 : Convert.ToInt32(row["ReportId"]);
                item.GroupType = row["GroupType"] == DBNull.Value ? "" : row["GroupType"].ToString();
                item.GroupIndex = row["GroupIndex"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["GroupIndex"]);
                item.SearchQuery = row["SearchQuery"] == DBNull.Value ? "" : row["SearchQuery"].ToString();
                item.SrNo = row["SrNo"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["SrNo"]);
                item.IsFilterColumn = row["IsFilterColumn"] == DBNull.Value ? false : Convert.ToBoolean(row["IsFilterColumn"]);
                item.MainReportColumnId = row["MainReportColumnId"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["MainReportColumnId"]);

                items.Add(item);
            }

            filter.Items = items;
            filter.PageNumber = pageNumber;
            filter.PageSize = pageSize;

            return filter;
        }

        //#region OLD_SYNC
        //public SaveColumnChangesResult SaveChanges(List<ReportColumnChange> changes, string mode)
        //{
        //    var result = new SaveColumnChangesResult { Status = "SAVED", IdMappings = new Dictionary<string, int>() };
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
        //        { "DisplayColumnName", "DisplayColumnName" },
        //        { "TableColumnName", "TableColumnName" },
        //        { "Datatype", "Datatype" },
        //        { "IsDefaultColumn", "IsDefaultColumn" },
        //        { "IsSqlParameter", "IsSqlParameter" },
        //        { "ReportId", "ReportId" },
        //        { "GroupType", "GroupType" },
        //        { "GroupIndex", "GroupIndex" },
        //        { "SearchQuery", "SearchQuery" },
        //        { "SrNo", "SrNo" },
        //        { "IsFilterColumn", "IsFilterColumn" },
        //        { "MainReportColumnId", "MainReportColumnId" }
        //    };
        //
        //    var rows = changes.GroupBy(c => c.ReportColumnId);
        //
        //    var sql = new StringBuilder();
        //
        //    var parameters = new List<DbParameter>();
        //    var paramIndex = 0;
        //
        //    foreach (var rowGroup in rows)
        //    {
        //        var reportColumnId = rowGroup.Key;
        //
        //        if (reportColumnId < 0)
        //        {
        //            var cols = new List<string>();
        //            var vals = new List<string>();
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
        //                else if (change.Column == "IsDefaultColumn" || change.Column == "IsSqlParameter" || change.Column == "IsFilterColumn")
        //                    paramValue = change.NewValue == "1" ? 1 : 0;
        //                else
        //                    paramValue = change.NewValue;
        //                parameters.Add(_dal.CreateParameter(valParam, paramValue));
        //                paramIndex++;
        //            }
        //
        //            if (cols.Count == 0) continue;
        //
        //            sql.AppendLine($"    INSERT INTO com_mst_reportcolumn ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)});");
        //            sql.AppendLine($"    SELECT CAST({_dal.GetIdentityFunction()} AS {_dal.GetIdentityCastType()}) AS NewId, @tempId{paramIndex} AS TempId, NULL AS ErrorMessage;");
        //            parameters.Add(_dal.CreateParameter($"@tempId{paramIndex}", reportColumnId));
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
        //                sql.AppendLine($"    UPDATE com_mst_reportcolumn SET {change.Column} = {valParam} WHERE ReportColumnId = {pkParam};");
        //
        //                object paramValue;
        //                if (string.IsNullOrEmpty(change.NewValue))
        //                    paramValue = DBNull.Value;
        //                else if (change.Column == "IsDefaultColumn" || change.Column == "IsSqlParameter" || change.Column == "IsFilterColumn")
        //                    paramValue = change.NewValue == "1" ? 1 : 0;
        //                else
        //                    paramValue = change.NewValue;
        //                parameters.Add(_dal.CreateParameter(valParam, paramValue));
        //                parameters.Add(_dal.CreateParameter(pkParam, reportColumnId));
        //                paramIndex++;
        //            }
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
        //#endregion

        public async Task<SaveColumnChangesResult> SaveChangesAsync(List<ReportColumnChange> changes, string mode)
        {
            var result = new SaveColumnChangesResult { Status = "SAVED", IdMappings = new Dictionary<string, int>() };

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

            var rows = changes.GroupBy(c => c.ReportColumnId);

            var auditEntries = new List<(int RecordId, string OldValues)>();
            foreach (var rowGroup in rows)
            {
                if (rowGroup.Key < 0) continue;
                var oldJson = await _dal.GetRowAsJsonAsync(ReportConfig.ReportColumnTable, "ReportColumnId", rowGroup.Key);
                if (oldJson != null)
                    auditEntries.Add((rowGroup.Key, oldJson));
            }

            var sql = new StringBuilder();

            var parameters = new List<DbParameter>();
            var paramIndex = 0;

            foreach (var rowGroup in rows)
            {
                var reportColumnId = rowGroup.Key;

                if (reportColumnId < 0)
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
                        else if (change.Column == "IsDefaultColumn" || change.Column == "IsSqlParameter" || change.Column == "IsFilterColumn")
                            paramValue = change.NewValue == "1" ? 1 : 0;
                        else
                            paramValue = change.NewValue;
                        parameters.Add(_dal.CreateParameter(valParam, paramValue));
                        paramIndex++;
                    }

                    if (cols.Count == 0) continue;

                    sql.AppendLine($"    INSERT INTO {ReportConfig.ReportColumnTable} ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)});");
                    sql.AppendLine($"    SELECT CAST({_dal.GetIdentityFunction()} AS {_dal.GetIdentityCastType()}) AS NewId, @tempId{paramIndex} AS TempId, NULL AS ErrorMessage;");
                    parameters.Add(_dal.CreateParameter($"@tempId{paramIndex}", reportColumnId));
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
                        else if (change.Column == "IsDefaultColumn" || change.Column == "IsSqlParameter" || change.Column == "IsFilterColumn")
                            paramValue = change.NewValue == "1" ? 1 : 0;
                        else
                            paramValue = change.NewValue;
                        parameters.Add(_dal.CreateParameter(valParam, paramValue));
                        paramIndex++;
                    }

                    if (setClauses.Count == 0) continue;

                    var pkParam = $"@pk{paramIndex}";
                    sql.AppendLine($"    UPDATE {ReportConfig.ReportColumnTable} SET {string.Join(", ", setClauses)} WHERE ReportColumnId = {pkParam};");
                    parameters.Add(_dal.CreateParameter(pkParam, reportColumnId));
                    paramIndex++;
                }
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
                    await _auditLog.LogChangeAsync(ReportConfig.ReportColumnTable, entry.RecordId, "UPDATE", entry.OldValues);

                return result;
            }
            catch (Exception ex)
            {
                _dal.Rollback();
                result.Status = "ERROR:" + ex.Message;
                return result;
            }
        }

        //#region OLD_SYNC
        //public int GetNextIdentity()
        //{
        //    var sql = $"SELECT {_dal.GetNullFunction()}(MAX(ReportColumnId), 0) + 1 FROM com_mst_reportcolumn";
        //    return Convert.ToInt32(_dal.ExecuteScalar(sql));
        //}
        //#endregion

        public async Task<int> GetNextIdentityAsync()
        {
            var sql = $"SELECT {_dal.GetNullFunction()}(MAX(ReportColumnId), 0) + 1 FROM {ReportConfig.ReportColumnTable}";
            return Convert.ToInt32(await _dal.ExecuteScalarAsync(sql));
        }

        //#region OLD_SYNC
        //public string DeleteReportColumn(int reportColumnId)
        //{
        //    //var depSql = @"
        //    //        SELECT COUNT(*) FROM com_mst_reportfilteringcolumn
        //    //        WHERE ReportId = (SELECT ReportId FROM com_mst_reportcolumn WHERE ReportColumnId = @id)
        //    //        HAVING COUNT(*) > 0";
        //
        //    //var depDt = _dal.ExecuteQuery(depSql, _dal.CreateParameter("@id", reportColumnId));
        //
        //    //if (depDt.Rows.Count > 0)
        //    //{
        //    //    var count = Convert.ToInt32(depDt.Rows[0][0]);
        //    //    return $"DEPENDENT:Filtering Columns:{count}";
        //    //}
        //
        //    var depCount = Convert.ToInt32(_dal.ExecuteScalar(
        //        "SELECT COUNT(*) FROM com_mst_reportfilteringcolumn WHERE ReportId = (SELECT ReportId FROM com_mst_reportcolumn WHERE ReportColumnId = @id)",
        //        _dal.CreateParameter("@id", reportColumnId)));
        //    if (depCount > 0)
        //        return $"DEPENDENT:Filtering Columns:{depCount}";
        //
        //    var rowsAffected = _dal.ExecuteNonQuery(
        //        "DELETE FROM com_mst_reportcolumn WHERE ReportColumnId = @id",
        //        _dal.CreateParameter("@id", reportColumnId));
        //    return rowsAffected > 0 ? "DELETED" : "NOT_FOUND";
        //}
        //#endregion

        public async Task<string> DeleteReportColumnAsync(int reportColumnId)
        {
            var oldJson = await _dal.GetRowAsJsonAsync(ReportConfig.ReportColumnTable, "ReportColumnId", reportColumnId);

            var depCount = Convert.ToInt32(await _dal.ExecuteScalarAsync(
                $"SELECT COUNT(*) FROM {ReportConfig.ReportFilteringColumnTable} WHERE ReportId = (SELECT ReportId FROM {ReportConfig.ReportColumnTable} WHERE ReportColumnId = @id)",
                _dal.CreateParameter("@id", reportColumnId)));
            if (depCount > 0)
                return $"DEPENDENT:Filtering Columns:{depCount}";

            var rowsAffected = await _dal.ExecuteNonQueryAsync(
                $"DELETE FROM {ReportConfig.ReportColumnTable} WHERE ReportColumnId = @id",
                _dal.CreateParameter("@id", reportColumnId));
            if (rowsAffected > 0)
                await _auditLog.LogChangeAsync(ReportConfig.ReportColumnTable, reportColumnId, "DELETE", oldJson);
            return rowsAffected > 0 ? "DELETED" : "NOT_FOUND";
        }

        private static string GetSortColumnExpression(string sortColumn)
        {
            switch (sortColumn)
            {
                case "ReportId": return "ReportId";
                case "GroupType": return "GroupType";
                case "SrNo": return "SrNo";
                case "IsDefaultColumn": return "IsDefaultColumn";
                case "IsSqlParameter": return "IsSqlParameter";
                case "IsFilterColumn": return "IsFilterColumn";
                default: return "ReportColumnId";
            }
        }
    }
}
