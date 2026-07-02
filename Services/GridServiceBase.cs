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
    /// <summary>
    /// Generic base class for grid data services.
    /// Provides the common SaveChangesAsync transaction pattern,
    /// audit logging, and pagination helpers.
    /// </summary>
    /// <typeparam name="TChange">The change type (e.g. AlertConfigChange)</typeparam>
    /// <typeparam name="TResult">The result type (e.g. SaveAlertConfigChangesResult)</typeparam>
    public abstract class GridServiceBase<TChange, TResult>
        where TResult : class, new()
    {
        protected readonly IDal _dal;
        protected readonly AuditLogService _auditLog;

        protected GridServiceBase() : this(new Dal())
        {
        }

        protected GridServiceBase(IDal dal)
        {
            _dal = dal;
            _auditLog = new AuditLogService(dal);
        }

        // ---------- Abstract: entity-specific ----------

        /// <summary>Table name for the entity (e.g. com_mst_alertconfig).</summary>
        protected abstract string TableName { get; }

        /// <summary>Primary key column name (e.g. AlertConfigId).</summary>
        protected abstract string PkColumn { get; }

        /// <summary>Display name for audit log (e.g. "Alert Config").</summary>
        protected abstract string EntityDisplayName { get; }

        /// <summary>
        /// Whitelist of columns that can be updated.
        /// Key = UI column name, Value = DB column name.
        /// </summary>
        protected abstract Dictionary<string, string> ColumnWhitelist { get; }

        /// <summary>Convert a change's NewValue to the appropriate DB parameter type.</summary>
        protected virtual object ConvertValue(string column, string newValue)
        {
            if (string.IsNullOrEmpty(newValue))
                return DBNull.Value;
            return newValue;
        }

        /// <summary>Build WHERE clause and parameters for the search/filter query.</summary>
        protected virtual void BuildWhere(StringBuilder where, List<DbParameter> parameters, string searchText)
        {
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string search = searchText.Trim();
                where.Append($" AND {PkColumn} LIKE @search");
                parameters.Add(_dal.CreateParameter("@search", $"%{search}%"));
            }
        }

        /// <summary>Select column list for the grid query.</summary>
        protected abstract string SelectColumns { get; }

        /// <summary>Map a DataRow to a grid item.</summary>
        protected abstract object MapRowToItem(DataRow row);

        /// <summary>
        /// Get the sort column expression (derived classes can override
        /// to translate UI sort column names to DB column names).
        /// </summary>
        protected virtual string GetSortColumnExpression(string sortColumn)
        {
            if (string.IsNullOrWhiteSpace(sortColumn))
                return PkColumn;
            return sortColumn;
        }

        /// <summary>Default page size.</summary>
        protected virtual int DefaultPageSize => 5;

        /// <summary>Maximum page size.</summary>
        protected virtual int MaxPageSize => 100;

        // ---------- GetNextIdentity ----------

        public virtual async Task<int> GetNextIdentityAsync()
        {
            string sql = $"SELECT MAX({PkColumn}) + 1 FROM {TableName}";
            var result = await _dal.ExecuteScalarAsync(sql);
            return result == DBNull.Value ? 1 : Convert.ToInt32(result);
        }

        // ---------- Audit: capture old JSON ----------

        protected virtual async Task<string> GetOldJsonAsync(int id)
        {
            string sql = $"SELECT * FROM {TableName} WHERE {PkColumn} = @id";
            var dt = await _dal.ExecuteQueryAsync(sql, _dal.CreateParameter("@id", id));
            if (dt.Rows.Count == 0) return "{}";
            return await _dal.GetRowAsJsonAsync(TableName, PkColumn, id);
        }

        // ---------- SaveChanges ----------

        public virtual async Task<TResult> SaveChangesAsync(List<TChange> changes, string mode)
        {
            var result = new TResult();
            var resultType = result.GetType();

            if (mode == "CANCEL")
            {
                resultType.GetProperty("Status")?.SetValue(result, "CANCELLED");
                return result;
            }

            if (changes == null || changes.Count == 0)
            {
                resultType.GetProperty("Status")?.SetValue(result, "NO_CHANGES");
                return result;
            }

            var idMappings = new Dictionary<string, int>();
            var columnWhitelist = ColumnWhitelist;

            var rows = changes.GroupBy(c => GetChangeId(c));
            var sql = new StringBuilder();
            var parameters = new List<DbParameter>();
            int paramIndex = 0;
            var istNow = _dal.GetCurrentIstTime();

            foreach (var rowGroup in rows)
            {
                var entityId = rowGroup.Key;

                if (entityId < 0) // NEW row
                {
                    int newId = await GetNextIdentityAsync();
                    var cols = new List<string> { PkColumn };
                    var vals = new List<string> { $"@pk{paramIndex}" };
                    parameters.Add(_dal.CreateParameter($"@pk{paramIndex}", newId));
                    paramIndex++;

                    foreach (var change in rowGroup)
                    {
                        if (!columnWhitelist.ContainsKey(GetChangeColumn(change)))
                            continue;

                        string dbCol = columnWhitelist[GetChangeColumn(change)];
                        var valParam = $"@val{paramIndex}";
                        cols.Add(dbCol);
                        vals.Add(valParam);
                        parameters.Add(_dal.CreateParameter(valParam, ConvertValue(GetChangeColumn(change), GetChangeNewValue(change))));
                        paramIndex++;
                    }

                    cols.Add("CreatedOn");
                    vals.Add("@createdOn");
                    parameters.Add(_dal.CreateParameter("@createdOn", istNow));

                    cols.Add("CreatedBy");
                    vals.Add("@createdBy");
                    parameters.Add(_dal.CreateParameter("@createdBy", 1));

                    sql.AppendLine($"INSERT INTO {TableName} ({string.Join(", ", cols)}) VALUES ({string.Join(", ", vals)});");

                    idMappings[entityId.ToString()] = newId;
                }
                else // EXISTING row: UPDATE
                {
                    var oldJson = await GetOldJsonAsync(entityId);
                    var setClauses = new List<string>();

                    foreach (var change in rowGroup)
                    {
                        if (!columnWhitelist.ContainsKey(GetChangeColumn(change)))
                            continue;

                        string dbCol = columnWhitelist[GetChangeColumn(change)];
                        var valParam = $"@val{paramIndex}";
                        setClauses.Add($"{dbCol} = {valParam}");
                        parameters.Add(_dal.CreateParameter(valParam, ConvertValue(GetChangeColumn(change), GetChangeNewValue(change))));
                        paramIndex++;
                    }

                    if (setClauses.Count > 0)
                    {
                        setClauses.Add("UpdatedOn = @updatedOn");
                        parameters.Add(_dal.CreateParameter("@updatedOn", istNow));
                        setClauses.Add("UpdatedBy = @updatedBy");
                        parameters.Add(_dal.CreateParameter("@updatedBy", 1));

                        sql.AppendLine($"UPDATE {TableName} SET {string.Join(", ", setClauses)} WHERE {PkColumn} = @pk{paramIndex};");
                        parameters.Add(_dal.Cre
