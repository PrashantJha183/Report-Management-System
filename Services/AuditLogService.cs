using Report.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Report.Services
{
    public class AuditLogService
    {
        private readonly IDal _dal;

        public AuditLogService() : this(new Dal())
        {
        }

        public AuditLogService(IDal dal)
        {
            _dal = dal;
        }

        public async Task LogChangeAsync(string tableName, int recordId, string operationType, string oldValues, string createdBy = null)
        {
            if (oldValues == null)
                return;

            var sql = new StringBuilder();
            sql.Append($"INSERT INTO {ReportConfig.AuditLogTable} (TableName, RecordId, OperationType, OldValues, CreatedOn, CreatedBy) ");
            sql.Append("VALUES (@tableName, @recordId, @operationType, @oldValues, @createdOn, @createdBy)");

            var parameters = new List<DbParameter>
            {
                _dal.CreateParameter("@tableName", tableName),
                _dal.CreateParameter("@recordId", recordId),
                _dal.CreateParameter("@operationType", operationType),
                _dal.CreateParameter("@oldValues", oldValues),
                _dal.CreateParameter("@createdOn", _dal.GetCurrentIstTime()),
                _dal.CreateParameter("@createdBy", createdBy ?? (object)DBNull.Value)
            };

            await _dal.ExecuteNonQueryAsync(sql.ToString(), parameters.ToArray());
        }

        public async Task<List<AuditLog>> GetAuditLogsAsync(string tableName, int recordId, int page = 1, int pageSize = 0)
        {
            var sql = $"SELECT AuditLogId, TableName, RecordId, OperationType, OldValues, CreatedOn, CreatedBy FROM {ReportConfig.AuditLogTable} WHERE TableName = @tableName AND RecordId = @recordId ORDER BY CreatedOn DESC";

            if (pageSize > 0)
            {
                int offset = (page - 1) * pageSize;
                sql += $" {_dal.GetPaginationClause(offset, pageSize)}";
            }

            var parameters = new List<DbParameter>
            {
                _dal.CreateParameter("@tableName", tableName),
                _dal.CreateParameter("@recordId", recordId)
            };

            return await _dal.ExecuteReaderAsync(sql, map, parameters.ToArray());
        }

        public async Task<int> GetAuditLogCountAsync(string tableName, int recordId)
        {
            var sql = $"SELECT COUNT(*) FROM {ReportConfig.AuditLogTable} WHERE TableName = @tableName AND RecordId = @recordId";
            var parameters = new List<DbParameter>
            {
                _dal.CreateParameter("@tableName", tableName),
                _dal.CreateParameter("@recordId", recordId)
            };
            var result = await _dal.ExecuteScalarAsync(sql, parameters.ToArray());
            return Convert.ToInt32(result);
        }

        public async Task<List<AuditLog>> GetDeletedRecordsAsync(string tableName, int page = 1, int pageSize = 0)
        {
            var sql = $"SELECT AuditLogId, TableName, RecordId, OperationType, OldValues, CreatedOn, CreatedBy FROM {ReportConfig.AuditLogTable} WHERE TableName = @tableName AND OperationType = 'DELETE' ORDER BY CreatedOn DESC";

            if (pageSize > 0)
            {
                int offset = (page - 1) * pageSize;
                sql += $" {_dal.GetPaginationClause(offset, pageSize)}";
            }

            var parameters = new List<DbParameter>
            {
                _dal.CreateParameter("@tableName", tableName)
            };

            return await _dal.ExecuteReaderAsync(sql, map, parameters.ToArray());
        }

        public async Task<int> GetDeletedRecordsCountAsync(string tableName)
        {
            var sql = $"SELECT COUNT(*) FROM {ReportConfig.AuditLogTable} WHERE TableName = @tableName AND OperationType = 'DELETE'";
            var parameters = new List<DbParameter>
            {
                _dal.CreateParameter("@tableName", tableName)
            };
            var result = await _dal.ExecuteScalarAsync(sql, parameters.ToArray());
            return Convert.ToInt32(result);
        }

        public async Task<Dictionary<int, string>> GetLinkItemNamesAsync(IEnumerable<int> referenceLinkIds)
        {
            var result = new Dictionary<int, string>();
            foreach (var id in referenceLinkIds)
            {
                if (!result.ContainsKey(id))
                    result[id] = "";
            }

            if (result.Count == 0)
                return result;

            var sql = $"SELECT LinkItemId, LinkItemName FROM {ReportConfig.LinkItemTable} WHERE LinkItemId IN ({string.Join(",", result.Keys)})";
            var dt = await _dal.ExecuteQueryAsync(sql);
            foreach (DataRow row in dt.Rows)
            {
                var linkItemId = Convert.ToInt32(row["LinkItemId"]);
                result[linkItemId] = row["LinkItemName"].ToString();
            }

            return result;
        }

        private static AuditLog map(DbDataReader reader)
        {
            return new AuditLog
            {
                AuditLogId = Convert.ToInt32(reader["AuditLogId"]),
                TableName = reader["TableName"].ToString(),
                RecordId = Convert.ToInt32(reader["RecordId"]),
                OperationType = reader["OperationType"].ToString(),
                OldValues = reader["OldValues"] == DBNull.Value ? null : reader["OldValues"].ToString(),
                CreatedOn = Convert.ToDateTime(reader["CreatedOn"]),
                CreatedBy = reader["CreatedBy"] == DBNull.Value ? null : reader["CreatedBy"].ToString()
            };
        }
    }
}
