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
    public class CompanyConfigService
    {
        private readonly IDal _dal;
        private readonly AuditLogService _auditLog;

        public CompanyConfigService() : this(new Dal())
        {
        }

        public CompanyConfigService(IDal dal)
        {
            _dal = dal;
            _auditLog = new AuditLogService(dal);
        }

        private static readonly HashSet<string> ColumnWhitelist = new HashSet<string>
        {
            "CompanyId", "Code", "ConfigValue", "Description"
        };

        public async Task<CompanyConfigGridViewModel> GetCompanyConfigsAsync(CompanyConfigGridViewModel filter)
        {
            if (filter == null)
                filter = new CompanyConfigGridViewModel();

            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = ReportConfig.CompanyConfigPageSize;
            if (pageSize < ReportConfig.CompanyConfigPageSize) pageSize = ReportConfig.CompanyConfigPageSize;
            if (pageSize > ReportConfig.MaxPageSize) pageSize = ReportConfig.MaxPageSize;

            var parameters = new List<DbParameter>();
            var where = new StringBuilder(" WHERE 1=1");

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                string search = filter.SearchText.Trim();
                where.Append(" AND (Code LIKE @search OR Description LIKE @search)");
                parameters.Add(_dal.CreateParameter("@search", $"%{search}%"));
            }

            string sortColumn = GetSortColumnExpression(filter.SortColumn);
            string sortDir = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            int offset = (pageNumber - 1) * pageSize;

            string sql = $@"
                SELECT CompanyConfigId, CompanyId, Code, ConfigValue, Description,
                       COUNT(*) OVER() AS TotalRecords
                FROM {ReportConfig.CompanyConfigTable}{where}
                ORDER BY {sortColumn} {sortDir}
                {_dal.GetPaginationClause(offset, pageSize)}";

            DataTable dt = await _dal.ExecuteQueryAsync(sql, parameters.ToArray());
            filter.TotalRecords = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["TotalRecords"]) : 0;

            var items = new List<CompanyConfigGridItemViewModel>();
            foreach (DataRow row in dt.Rows)
            {
                var item = new CompanyConfigGridItemViewModel
                {
                    CompanyConfigId = row["CompanyConfigId"] == DBNull.Value ? 0 : Convert.ToInt32(row["CompanyConfigId"]),
                    CompanyId = row["CompanyId"] == DBNull.Value ? 0 : Convert.ToInt32(row["CompanyId"]),
                    Code = row["Code"] == DBNull.Value ? "" : row["Code"].ToString(),
                    ConfigValue = row["ConfigValue"] == DBNull.Value ? "" : row["ConfigValue"].ToString(),
                    Description = row["Description"] == DBNull.Value ? "" : row["Description"].ToString()
                };
                items.Add(item);
            }

            filter.Items = items;
            filter.PageNumber = pageNumber;
            filter.PageSize = pageSize;
            return filter;
        }

        public async Task<SaveCompanyConfigChangesResult> SaveChangesAsync(List<CompanyConfigChange> changes, string mode)
        {
            var result = new SaveCompanyConfigChangesResult { Status = "SAVED", IdMappings = new Dictionary<string, int>() };

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

            var rows = changes.GroupBy(c => c.CompanyConfigId);

            var auditEntries = new List<(int RecordId, string OldValues)>();
            foreach (var rowGroup in rows)
            {
                if (rowGroup.Key < 0) continue;
                var oldJson = await _dal.GetRowAsJsonAsync(ReportConfig.CompanyConfigTable, "CompanyConfigId", rowGroup.Key);
                if (oldJson != null)
                    auditEntries.Add((rowGroup.Key, oldJson));
            }

            var sql = new StringBuilder();
            var parameters = new List<DbParameter>();
            var paramIndex = 0;

            try
            {
                _dal.BeginTransaction();

                foreach (var rowGroup in rows)
                {
                    var companyConfigId = rowGroup.Key;

                    if (companyConfigId < 0)
                    {
                        var lockSql = ReportConfig.WrapLockQuery(ReportConfig.CompanyConfigTable, _dal.DatabaseType,
                            $"{_dal.GetNullFunction()}(MAX(CompanyConfigId), 0) + 1");
                        var maxId = Convert.ToInt32(await _dal.ExecuteScalarAsync(lockSql));
                        var newId = maxId + 1;

                        var cols = new List<string> { "CompanyConfigId" };
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
                            else
                                paramValue = change.NewValue;
                            parameters.Add(_dal.CreateParameter(valParam, paramValue));
                            paramIndex++;
                        }

                        if (cols.Count == 1) continue;

                        sql.AppendLine($"    INSERT INTO {ReportConfig.CompanyConfigTable} ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)});");
                        sql.AppendLine($"    SELECT {newId} AS NewId, @tempId{paramIndex} AS TempId, NULL AS ErrorMessage;");
                        parameters.Add(_dal.CreateParameter($"@tempId{paramIndex}", companyConfigId));
                        paramIndex++;
                    }
                    else
                    {
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
                            else
                                paramValue = change.NewValue;
                            parameters.Add(_dal.CreateParameter(valParam, paramValue));
                            paramIndex++;
                        }

                        if (setClauses.Count > 0)
                        {
                            sql.AppendLine($"    UPDATE {ReportConfig.CompanyConfigTable} SET {string.Join(", ", setClauses)} WHERE CompanyConfigId = {pkParam};");
                            parameters.Add(_dal.CreateParameter(pkParam, companyConfigId));
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

                foreach (var entry in auditEntries)
                    await _auditLog.LogChangeAsync(ReportConfig.CompanyConfigTable, entry.RecordId, "UPDATE", entry.OldValues);

                return result;
            }
            catch (Exception ex)
            {
                _dal.Rollback();
                result.Status = "ERROR:" + ex.Message;
                return result;
            }
        }

        public async Task<int> GetNextIdentityAsync()
        {
            var sql = $"SELECT {_dal.GetNullFunction()}(MAX(CompanyConfigId), 0) + 1 FROM {ReportConfig.CompanyConfigTable}";
            return Convert.ToInt32(await _dal.ExecuteScalarAsync(sql));
        }

        public async Task<string> DeleteCompanyConfigAsync(int companyConfigId)
        {
            var oldJson = await _dal.GetRowAsJsonAsync(ReportConfig.CompanyConfigTable, "CompanyConfigId", companyConfigId);

            var rowsAffected = await _dal.ExecuteNonQueryAsync(
                $"DELETE FROM {ReportConfig.CompanyConfigTable} WHERE CompanyConfigId = @id",
                _dal.CreateParameter("@id", companyConfigId));

            if (rowsAffected > 0)
                await _auditLog.LogChangeAsync(ReportConfig.CompanyConfigTable, companyConfigId, "DELETE", oldJson);

            return rowsAffected > 0 ? "DELETED" : "NOT_FOUND";
        }

        private static string GetSortColumnExpression(string sortColumn)
        {
            switch (sortColumn)
            {
                case "CompanyId": return "CompanyId";
                case "Code": return "Code";
                case "ConfigValue": return "ConfigValue";
                case "Description": return "Description";
                default: return "CompanyConfigId";
            }
        }
    }
}
