using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace Report.Services
{
    public class Dal : IDal
    {
        private static readonly TimeZoneInfo _istZone;

        private DbConnection _currentConnection;
        private DbTransaction _currentTransaction;

        static Dal()
        {
            _istZone = TimeZoneInfo.FindSystemTimeZoneById(ReportConfig.TimezoneId);
        }

        public Dal()
        {
        }

        private string ConnectionString => HttpContext.Current?.Session["ConnectionString"]?.ToString();

        private string DbType => HttpContext.Current?.Session["DbType"]?.ToString() ?? "MYSQL";

        public string DatabaseType => DbType;

        public string GetTopClause(int count)
        {
            if (DbType.ToUpper() == "MYSQL")
                return $"LIMIT {count}";
            else
                return $"SELECT TOP {count}";
        }

        private DbProviderFactory GetFactory()
        {
            switch (DbType.ToUpper())
            {
                case "MYSQL":
                    return MySql.Data.MySqlClient.MySqlClientFactory.Instance;
                case "SQLSERVER":
                default:
                    return System.Data.SqlClient.SqlClientFactory.Instance;
            }
        }

        public DbParameter CreateParameter(string name, object value)
        {
            var factory = GetFactory();
            var param = factory.CreateParameter();
            param.ParameterName = name;
            param.Value = value ?? DBNull.Value;
            return param;
        }

        public string GetPaginationClause(int offset, int pageSize)
        {
            if (DbType.ToUpper() == "MYSQL")
                return $"LIMIT {pageSize} OFFSET {offset}";
            else
                return $"OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
        }

        public string GetIdentityFunction()
        {
            if (DbType.ToUpper() == "MYSQL")
                return "LAST_INSERT_ID()";
            else
                return "SCOPE_IDENTITY()";
        }

        public string GetIdentityCastType()
        {
            if (DbType.ToUpper() == "MYSQL")
                return "SIGNED";
            else
                return "INT";
        }

        public string GetNullFunction()
        {
            if (DbType.ToUpper() == "MYSQL")
                return "IFNULL";
            else
                return "ISNULL";
        }

        public void BeginTransaction()
        {
            var factory = GetFactory();
            _currentConnection = factory.CreateConnection();
            _currentConnection.ConnectionString = ConnectionString;
            _currentConnection.Open();
            _currentTransaction = _currentConnection.BeginTransaction();
        }

        public void Commit()
        {
            try
            {
                _currentTransaction?.Commit();
            }
            finally
            {
                CleanupTransaction();
            }
        }

        public void Rollback()
        {
            try
            {
                _currentTransaction?.Rollback();
            }
            finally
            {
                CleanupTransaction();
            }
        }

        private void CleanupTransaction()
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
            _currentConnection?.Dispose();
            _currentConnection = null;
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, params DbParameter[] parameters)
        {
            var factory = GetFactory();
            DbConnection conn = null;
            bool ownConnection = false;

            try
            {
                try
                {
                    if (_currentTransaction != null)
                    {
                        conn = _currentTransaction.Connection;
                    }
                    else
                    {
                        conn = factory.CreateConnection();
                        conn.ConnectionString = ConnectionString;
                        ownConnection = true;
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = query;
                        if (_currentTransaction != null)
                            cmd.Transaction = _currentTransaction;

                        if (parameters != null)
                        {
                            foreach (var p in parameters)
                                cmd.Parameters.Add(p);
                        }

                        if (ownConnection)
                            await conn.OpenAsync();

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            var dt = new DataTable();
                            dt.Load(reader);
                            return dt;
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.Log(ex, query);
                    throw;
                }
            }
            finally
            {
                if (ownConnection && conn != null)
                    conn.Dispose();
            }
        }

        public async Task<object> ExecuteScalarAsync(string query, params DbParameter[] parameters)
        {
            var factory = GetFactory();
            DbConnection conn = null;
            bool ownConnection = false;

            try
            {
                try
                {
                    if (_currentTransaction != null)
                    {
                        conn = _currentTransaction.Connection;
                    }
                    else
                    {
                        conn = factory.CreateConnection();
                        conn.ConnectionString = ConnectionString;
                        ownConnection = true;
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = query;
                        if (_currentTransaction != null)
                            cmd.Transaction = _currentTransaction;

                        if (parameters != null)
                        {
                            foreach (var p in parameters)
                                cmd.Parameters.Add(p);
                        }

                        if (ownConnection)
                            await conn.OpenAsync();

                        var result = await cmd.ExecuteScalarAsync();
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.Log(ex, query);
                    throw;
                }
            }
            finally
            {
                if (ownConnection && conn != null)
                    conn.Dispose();
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string query, params DbParameter[] parameters)
        {
            var factory = GetFactory();
            DbConnection conn = null;
            bool ownConnection = false;

            try
            {
                try
                {
                    if (_currentTransaction != null)
                    {
                        conn = _currentTransaction.Connection;
                    }
                    else
                    {
                        conn = factory.CreateConnection();
                        conn.ConnectionString = ConnectionString;
                        ownConnection = true;
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = query;
                        if (_currentTransaction != null)
                            cmd.Transaction = _currentTransaction;

                        if (parameters != null)
                        {
                            foreach (var p in parameters)
                                cmd.Parameters.Add(p);
                        }

                        if (ownConnection)
                            await conn.OpenAsync();

                        var result = await cmd.ExecuteNonQueryAsync();
                        QueryLogger.Log(query);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.Log(ex, query);
                    throw;
                }
            }
            finally
            {
                if (ownConnection && conn != null)
                    conn.Dispose();
            }
        }

        public async Task<List<T>> ExecuteReaderAsync<T>(string query, Func<DbDataReader, T> map, params DbParameter[] parameters)
        {
            var factory = GetFactory();
            DbConnection conn = null;
            bool ownConnection = false;

            try
            {
                if (_currentTransaction != null)
                {
                    conn = _currentTransaction.Connection;
                }
                else
                {
                    conn = factory.CreateConnection();
                    conn.ConnectionString = ConnectionString;
                    ownConnection = true;
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = query;
                    if (_currentTransaction != null)
                        cmd.Transaction = _currentTransaction;

                    if (parameters != null)
                    {
                        foreach (var p in parameters)
                            cmd.Parameters.Add(p);
                    }

                    if (ownConnection)
                        await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var results = new List<T>();
                        while (await reader.ReadAsync())
                            results.Add(map(reader));
                        return results;
                    }
                }
            }
            finally
            {
                if (ownConnection && conn != null)
                    conn.Dispose();
            }
        }

        public async Task<string> GetRowAsJsonAsync(string tableName, string pkColumn, object pkValue)
        {
            var sql = $"SELECT * FROM {tableName} WHERE {pkColumn} = @pk";
            var dt = await ExecuteQueryAsync(sql, CreateParameter("@pk", pkValue));
            if (dt.Rows.Count == 0) return null;
            var dict = new Dictionary<string, object>();
            foreach (DataColumn col in dt.Columns)
                dict[col.ColumnName] = dt.Rows[0][col] == DBNull.Value ? null : dt.Rows[0][col];
            return JsonConvert.SerializeObject(dict);
        }

        public DateTime GetCurrentIstTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _istZone);
        }
    }
}
