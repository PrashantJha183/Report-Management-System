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
    public class ReportService
    {
        private readonly IDal _dal;
        private readonly AuditLogService _auditLog;

        public ReportService() : this(new Dal())
        {
        }

        public ReportService(IDal dal)
        {
            _dal = dal;
            _auditLog = new AuditLogService(dal);
        }

        private static readonly HashSet<string> ColumnWhitelist = new HashSet<string>
        {
            "ReferenceLinkId", "Code", "Query", "WhereClause", "OrderBy",
            "DetailQuery", "DetailPrimaryKey", "IsMasterDetail", "IsStoreProcedure",
            "ReportTypeId", "IsLocationFilter"
        };

        #region OLD_SYNC (retained for reference — all callers use async)
        //public ReportGridViewModel GetReports(ReportGridViewModel filter)
        //{
        //    if (filter == null) filter = new ReportGridViewModel();
        //    var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        //    var pageSize = 7;
        //    if (pageSize < 10) pageSize = 7;
        //    if (pageSize > 100) pageSize = 100;
        //    var parameters = new List<DbParameter>();
        //    var where = new StringBuilder(" WHERE 1=1");
        //    if (!string.IsNullOrWhiteSpace(filter.SearchText))
        //    {
        //        var search = filter.SearchText.Trim();
        //        where.Append(" AND (r.Code LIKE @search OR r.Query LIKE @search OR r.WhereClause LIKE @search OR r.OrderBy LIKE @search OR r.DetailPrimaryKey LIKE @search OR l.LinkItemName LIKE @search)");
        //        parameters.Add(_dal.CreateParameter("@search", $"%{search}%"));
        //    }
        //    if (filter.FilterLinkItemId > 0)
        //    {
        //        where.Append(" AND r.ReferenceLinkId = @linkItemId");
        //        parameters.Add(_dal.CreateParameter("@linkItemId", filter.FilterLinkItemId));
        //    }
        //    var countSql = $"SELECT COUNT(*) FROM com_mst_report r LEFT JOIN com_mst_link_item l ON r.ReferenceLinkId = l.LinkItemId {where}";
        //    filter.TotalRecords = Convert.ToInt32(_dal.ExecuteScalar(countSql, parameters.ToArray()));
        //    var sortColumn = GetSortColumnExpression(filter.SortColumn);
        //    var sortDir = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        //    var offset = (pageNumber - 1) * pageSize;
        //    var sql = $@"
        //SELECT r.ReportId, r.ReferenceLinkId, r.Code, r.Query, r.WhereClause, r.OrderBy,
        //       r.DetailQuery, r.DetailPrimaryKey, r.IsMasterDetail, r.IsStoreProcedure,
        //       r.ReportTypeId, r.IsLocationFilter,
        //       l.LinkItemName
        //FROM com_mst_report r
        //LEFT JOIN com_mst_link_item l ON r.ReferenceLinkId = l.LinkItemId
        //{where}
        //ORDER BY {sortColumn} {sortDir}
        //{_dal.GetPaginationClause(offset, pageSize)}";
        //    var dataParams = parameters.Select(p => _dal.CreateParameter(p.ParameterName, p.Value)).ToList<DbParameter>();
        //    var dt = _dal.ExecuteQuery(sql, dataParams.ToArray());
        //    var items = new List<ReportGridItemViewModel>();
        //    foreach (DataRow row in dt.Rows)
        //    {
        //        items.Add(new ReportGridItemViewModel
        //        {
        //            ReportId = row["ReportId"] == DBNull.Value ? 0 : Convert.ToInt32(row["ReportId"]),
        //            ReferenceLinkId = row["ReferenceLinkId"] == DBNull.Value ? 0 : Convert.ToInt32(row["ReferenceLinkId"]),
        //            ReferenceLinkName = row["LinkItemName"] == DBNull.Value ? "" : row["LinkItemName"].ToString(),
        //            Code = row["Code"] == DBNull.Value ? "" : row["Code"].ToString(),
        //            Query = row["Query"] == DBNull.Value ? "" : row["Query"].ToString(),
        //            WhereClause = row["WhereClause"] == DBNull.Value ? "" : row["WhereClause"].ToString(),
        //            OrderBy = row["OrderBy"] == DBNull.Value ? "" : row["OrderBy"].ToString(),
        //            DetailQuery = row["DetailQuery"] == DBNull.Value ? "" : row["DetailQuery"].ToString(),
        //            DetailPrimaryKey = row["DetailPrimaryKey"] == DBNull.Value ? "" : row["DetailPrimaryKey"].ToString(),
        //            IsMasterDetail = row["IsMasterDetail"] == DBNull.Value ? false : Convert.ToBoolean(row["IsMasterDetail"]),
        //            IsStoreProcedure = row["IsStoreProcedure"] == DBNull.Value ? false : Convert.ToBoolean(row["IsStoreProcedure"]),
        //            ReportTypeId = row["ReportTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(row["ReportTypeId"]),
        //            IsLocationFilter = row["IsLocationFilter"] == DBNull.Value ? false : Convert.ToBoolean(row["IsLocationFilter"])
        //        });
        //    }
        //    filter.Items = items;
        //    filter.PageNumber = pageNumber;
        //    filter.PageSize = pageSize;
        //    return filter;
        //}
        #endregion

        public async Task<ReportGridViewModel> GetReportsAsync(ReportGridViewModel filter)
        {

            if (filter == null)
                filter = new ReportGridViewModel();

            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = ReportConfig.ReportPageSize;
            if (pageSize > ReportConfig.MaxPageSize) pageSize = ReportConfig.MaxPageSize;

            var parameters = new List<DbParameter>();
            var where = new StringBuilder(" WHERE 1=1");

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var search = filter.SearchText.Trim();
                where.Append(" AND (r.Code LIKE @search OR r.Query LIKE @search OR r.WhereClause LIKE @search OR r.OrderBy LIKE @search OR r.DetailPrimaryKey LIKE @search OR l.LinkItemName LIKE @search)");
                parameters.Add(_dal.CreateParameter("@search", $"%{search}%"));
            }

            if (filter.FilterLinkItemId > 0)
            {
                where.Append(" AND r.ReferenceLinkId = @linkItemId");
                parameters.Add(_dal.CreateParameter("@linkItemId", filter.FilterLinkItemId));
            }

            var sortColumn = GetSortColumnExpression(filter.SortColumn);
            var sortDir = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            var offset = (pageNumber - 1) * pageSize;

            // OLD: Separate COUNT + SELECT (2 round trips)
            //var countSql = $"SELECT COUNT(*) FROM com_mst_report r LEFT JOIN com_mst_link_item l ON r.ReferenceLinkId = l.LinkItemId {where}";
            //filter.TotalRecords = Convert.ToInt32(await _dal.ExecuteScalarAsync(countSql, parameters.ToArray()));
            //var dataParams = parameters.Select(p => _dal.CreateParameter(p.ParameterName, p.Value)).ToList<DbParameter>();
            //var dt = await _dal.ExecuteQueryAsync(sql, dataParams.ToArray());

            var sql = $@"
        SELECT r.ReportId, r.ReferenceLinkId, r.Code, r.Query, r.WhereClause, r.OrderBy,
               r.DetailQuery, r.DetailPrimaryKey, r.IsMasterDetail, r.IsStoreProcedure,
               r.ReportTypeId, r.IsLocationFilter,
               l.LinkItemName,
               COUNT(*) OVER() AS TotalRecords
        FROM {ReportConfig.ReportTable} r
        LEFT JOIN {ReportConfig.LinkItemTable} l ON r.ReferenceLinkId = l.LinkItemId
        {where}
        ORDER BY {sortColumn} {sortDir}
        {_dal.GetPaginationClause(offset, pageSize)}";

            var dt = await _dal.ExecuteQueryAsync(sql, parameters.ToArray());

            filter.TotalRecords = dt.Rows.Count > 0
                ? Convert.ToInt32(dt.Rows[0]["TotalRecords"])
                : 0;

            var items = new List<ReportGridItemViewModel>();
            foreach (DataRow row in dt.Rows)
            {
                items.Add(new ReportGridItemViewModel
                {
                    ReportId = row["ReportId"] == DBNull.Value ? 0 : Convert.ToInt32(row["ReportId"]),
                    ReferenceLinkId = row["ReferenceLinkId"] == DBNull.Value ? 0 : Convert.ToInt32(row["ReferenceLinkId"]),
                    ReferenceLinkName = row["LinkItemName"] == DBNull.Value ? "" : row["LinkItemName"].ToString(),
                    Code = row["Code"] == DBNull.Value ? "" : row["Code"].ToString(),
                    Query = row["Query"] == DBNull.Value ? "" : row["Query"].ToString(),
                    WhereClause = row["WhereClause"] == DBNull.Value ? "" : row["WhereClause"].ToString(),
                    OrderBy = row["OrderBy"] == DBNull.Value ? "" : row["OrderBy"].ToString(),
                    DetailQuery = row["DetailQuery"] == DBNull.Value ? "" : row["DetailQuery"].ToString(),
                    DetailPrimaryKey = row["DetailPrimaryKey"] == DBNull.Value ? "" : row["DetailPrimaryKey"].ToString(),
                    IsMasterDetail = row["IsMasterDetail"] == DBNull.Value ? false : Convert.ToBoolean(row["IsMasterDetail"]),
                    IsStoreProcedure = row["IsStoreProcedure"] == DBNull.Value ? false : Convert.ToBoolean(row["IsStoreProcedure"]),
                    ReportTypeId = row["ReportTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(row["ReportTypeId"]),
                    IsLocationFilter = row["IsLocationFilter"] == DBNull.Value ? false : Convert.ToBoolean(row["IsLocationFilter"])
                });
            }

            filter.Items = items;
            filter.PageNumber = pageNumber;
            filter.PageSize = pageSize;
            return filter;
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //public SaveChangesResult SaveChanges(List<ReportChange> changes, string mode)
        //{
        //    var result = new SaveChangesResult { Status = "SAVED", IdMappings = new Dictionary<string, int>() };
        //    if (mode == "CANCEL") { result.Status = "CANCELLED"; return result; }
        //    if (changes == null || changes.Count == 0) { result.Status = "NO_CHANGES"; return result; }
        //    var rows = changes.GroupBy(c => c.ReportId);
        //    var columnWhitelist = new Dictionary<string, string>
        //    {
        //        { "ReferenceLinkId", "ReferenceLinkId" }, { "Code", "Code" }, { "Query", "Query" },
        //        { "WhereClause", "WhereClause" }, { "OrderBy", "OrderBy" }, { "DetailQuery", "DetailQuery" },
        //        { "DetailPrimaryKey", "DetailPrimaryKey" }, { "IsMasterDetail", "IsMasterDetail" },
        //        { "IsStoreProcedure", "IsStoreProcedure" }, { "ReportTypeId", "ReportTypeId" },
        //        { "IsLocationFilter", "IsLocationFilter" }
        //    };
        //    var sql = new StringBuilder();
        //    var parameters = new List<DbParameter>();
        //    var paramIndex = 0;
        //    foreach (var rowGroup in rows)
        //    {
        //        var reportId = rowGroup.Key;
        //        if (reportId < 0)
        //        {
        //            var cols = new List<string>(); var vals = new List<string>();
        //            foreach (var change in rowGroup)
        //            {
        //                if (!columnWhitelist.ContainsKey(change.Column)) continue;
        //                var valParam = $"@val{paramIndex}"; cols.Add(change.Column); vals.Add(valParam);
        //                // ... value logic omitted for brevity in sync version
        //                parameters.Add(_dal.CreateParameter(valParam, change.NewValue));
        //                paramIndex++;
        //            }
        //            sql.AppendLine($"INSERT INTO com_mst_report ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)});");
        //            sql.AppendLine($"SELECT CAST({_dal.GetIdentityFunction()} AS {_dal.GetIdentityCastType()}) AS NewId, @tempId{paramIndex} AS TempId;");
        //            parameters.Add(_dal.CreateParameter($"@tempId{paramIndex}", reportId)); paramIndex++;
        //        }
        //        else
        //        {
        //            foreach (var change in rowGroup)
        //            {
        //                if (!columnWhitelist.ContainsKey(change.Column)) continue;
        //                var valParam = $"@val{paramIndex}"; var pkParam = $"@pk{paramIndex}";
        //                sql.AppendLine($"UPDATE com_mst_report SET {change.Column} = {valParam} WHERE ReportId = {pkParam};");
        //                // ... value logic omitted for brevity
        //                parameters.Add(_dal.CreateParameter(valParam, change.NewValue));
        //                parameters.Add(_dal.CreateParameter(pkParam, reportId)); paramIndex++;
        //            }
        //        }
        //    }
        //    try { _dal.BeginTransaction(); var dt = _dal.ExecuteQuery(sql.ToString(), parameters.ToArray()); _dal.Commit(); /* ... mapping ... */ return result; }
        //    catch (Exception ex) { _dal.Rollback(); result.Status = "ERROR:" + ex.Message; return result; }
        //}
        #endregion

        public async Task<SaveChangesResult> SaveChangesAsync(List<ReportChange> changes, string mode)
        {
            var result = new SaveChangesResult { Status = "SAVED", IdMappings = new Dictionary<string, int>() };

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

            var rows = changes.GroupBy(c => c.ReportId);
            // OLD: Dictionary whitelist (now uses static HashSet)
            //var columnWhitelist = new Dictionary<string, string> { ... };
            var sql = new StringBuilder();

            var parameters = new List<DbParameter>();
            var paramIndex = 0;

            foreach (var rowGroup in rows)
            {
                var reportId = rowGroup.Key;

                if (reportId < 0)
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

                        object valParamValue;
                        if (string.IsNullOrEmpty(change.NewValue))
                            valParamValue = DBNull.Value;
                        else if (change.Column == "IsMasterDetail" || change.Column == "IsStoreProcedure" || change.Column == "IsLocationFilter")
                            valParamValue = change.NewValue == "1" ? 1 : 0;
                        else
                            valParamValue = change.NewValue;
                        parameters.Add(_dal.CreateParameter(valParam, valParamValue));
                        paramIndex++;
                    }

                    sql.AppendLine($"    INSERT INTO {ReportConfig.ReportTable} ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)});");
                    sql.AppendLine($"    SELECT CAST({_dal.GetIdentityFunction()} AS {_dal.GetIdentityCastType()}) AS NewId, @tempId{paramIndex} AS TempId;");
                    parameters.Add(_dal.CreateParameter($"@tempId{paramIndex}", reportId));
                    paramIndex++;
                }
                else
                {
                    // OLD: Separate UPDATE per changed column
                    //foreach (var change in rowGroup)
                    //{
                    //    if (!ColumnWhitelist.Contains(change.Column)) continue;
                    //    ...
                    //}

                    // NEW: Single UPDATE per row with all columns
                    var setClauses = new List<string>();
                    var pkParam = $"@pk{paramIndex}";

                    foreach (var change in rowGroup)
                    {
                        if (!ColumnWhitelist.Contains(change.Column))
                            continue;
                        var valParam = $"@val{paramIndex}";
                        setClauses.Add($"{change.Column} = {valParam}");

                        object valParamValue;
                        if (string.IsNullOrEmpty(change.NewValue))
                            valParamValue = DBNull.Value;
                        else if (change.Column == "IsMasterDetail" || change.Column == "IsStoreProcedure" || change.Column == "IsLocationFilter")
                            valParamValue = change.NewValue == "1" ? 1 : 0;
                        else
                            valParamValue = change.NewValue;
                        parameters.Add(_dal.CreateParameter(valParam, valParamValue));
                        paramIndex++;
                    }

                    if (setClauses.Count > 0)
                    {
                        sql.AppendLine($"    UPDATE {ReportConfig.ReportTable} SET {string.Join(", ", setClauses)} WHERE ReportId = {pkParam};");
                        parameters.Add(_dal.CreateParameter(pkParam, reportId));
                        paramIndex++;
                    }
                }
            }

            var auditEntries = new List<(int RecordId, string OldValues)>();
            foreach (var rowGroup in rows)
            {
                if (rowGroup.Key < 0) continue;
                var oldJson = await _dal.GetRowAsJsonAsync(ReportConfig.ReportTable, "ReportId", rowGroup.Key);
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
                    await _auditLog.LogChangeAsync(ReportConfig.ReportTable, entry.RecordId, "UPDATE", entry.OldValues);

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
        //    var sql = $"SELECT {_dal.GetNullFunction()}(MAX(ReportId), 0) + 1 FROM com_mst_report";
        //    return Convert.ToInt32(_dal.ExecuteScalar(sql));
        //}
        #endregion

        public async Task<int> GetNextIdentityAsync()
        {
            var sql = $"SELECT {_dal.GetNullFunction()}(MAX(ReportId), 0) + 1 FROM {ReportConfig.ReportTable}";
            return Convert.ToInt32(await _dal.ExecuteScalarAsync(sql));
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //public string DeleteReport(int reportId)
        //{
        //    var colCount = Convert.ToInt32(_dal.ExecuteScalar(
        //        "SELECT COUNT(*) FROM com_mst_reportcolumn WHERE ReportId = @id",
        //        _dal.CreateParameter("@id", reportId)));
        //    if (colCount > 0) return $"DEPENDENT:Report Columns:{colCount}";
        //    var filterCount = Convert.ToInt32(_dal.ExecuteScalar(
        //        "SELECT COUNT(*) FROM com_mst_reportfilteringcolumn WHERE ReportId = @id",
        //        _dal.CreateParameter("@id", reportId)));
        //    if (filterCount > 0) return $"DEPENDENT:Filtering Columns:{filterCount}";
        //    var rowsAffected = _dal.ExecuteNonQuery(
        //        "DELETE FROM com_mst_report WHERE ReportId = @id",
        //        _dal.CreateParameter("@id", reportId));
        //    return rowsAffected > 0 ? "DELETED" : "NOT_FOUND";
        //}
        #endregion

        public async Task<string> DeleteReportAsync(int reportId)
        {
            var colCount = Convert.ToInt32(await _dal.ExecuteScalarAsync(
                $"SELECT COUNT(*) FROM {ReportConfig.ReportColumnTable} WHERE ReportId = @id",
                _dal.CreateParameter("@id", reportId)));
            if (colCount > 0)
                return $"DEPENDENT:Report Columns:{colCount}";

            var filterCount = Convert.ToInt32(await _dal.ExecuteScalarAsync(
                $"SELECT COUNT(*) FROM {ReportConfig.ReportFilteringColumnTable} WHERE ReportId = @id",
                _dal.CreateParameter("@id", reportId)));
            if (filterCount > 0)
                return $"DEPENDENT:Filtering Columns:{filterCount}";

            var oldJson = await _dal.GetRowAsJsonAsync(ReportConfig.ReportTable, "ReportId", reportId);

            var rowsAffected = await _dal.ExecuteNonQueryAsync(
                $"DELETE FROM {ReportConfig.ReportTable} WHERE ReportId = @id",
                _dal.CreateParameter("@id", reportId));

            if (rowsAffected > 0)
                await _auditLog.LogChangeAsync(ReportConfig.ReportTable, reportId, "DELETE", oldJson);

            return rowsAffected > 0 ? "DELETED" : "NOT_FOUND";
        }

        #region OLD_SYNC (retained for reference — all callers use async)
        //public DataTable GetLinkItems()
        //{
        //    return _dal.ExecuteQuery("SELECT LinkItemId, LinkItemName FROM com_mst_link_item ORDER BY LinkItemName");
        //}
        #endregion

        public async Task<DataTable> GetLinkItemsAsync()
        {
            return await _dal.ExecuteQueryAsync($"SELECT LinkItemId, LinkItemName FROM {ReportConfig.LinkItemTable} ORDER BY LinkItemName");
        }

        private static string GetSortColumnExpression(string sortColumn)
        {
            switch (sortColumn)
            {
                case "ReferenceLinkId": return "ReferenceLinkId";
                case "Code": return "Code";
                case "ReportTypeId": return "ReportTypeId";
                case "IsMasterDetail": return "IsMasterDetail";
                case "IsStoreProcedure": return "IsStoreProcedure";
                case "IsLocationFilter": return "IsLocationFilter";
                default: return "ReportId";
            }
        }
    }
}
