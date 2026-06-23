//using System;
//using System.Data;
//using System.Data.Common;
//using System.Data.SqlClient;
//using System.Web;
//using System.Xml.Linq;

//namespace Report.Services
//{
//    public class Dal : IDal
//    {
//        private readonly string _connectionString;

//        public Dal()
//        {
//            var path = HttpContext.Current.Server.MapPath("~/App_Data/DbConnection.xml");
//            var doc = XDocument.Load(path);
//            _connectionString = doc.Root.Element("Connection").Element("ConnectionString").Value;
//        }

//        public DataTable ExecuteQuery(string query, params DbParameter[] parameters)
//        {
//            using (var conn = new SqlConnection(_connectionString))
//            using (var cmd = new SqlCommand(query, conn))
//            {
//                if (parameters != null)
//                    cmd.Parameters.AddRange(parameters);
//                using (var da = new SqlDataAdapter(cmd))
//                {
//                    var dt = new DataTable();
//                    da.Fill(dt);
//                    return dt;
//                }
//            }
//        }

//        public object ExecuteScalar(string query, params DbParameter[] parameters)
//        {
//            using (var conn = new SqlConnection(_connectionString))
//            using (var cmd = new SqlCommand(query, conn))
//            {
//                if (parameters != null)
//                    cmd.Parameters.AddRange(parameters);
//                conn.Open();
//                return cmd.ExecuteScalar();
//            }
//        }

//        public int ExecuteNonQuery(string query, params DbParameter[] parameters)
//        {
//            using (var conn = new SqlConnection(_connectionString))
//            using (var cmd = new SqlCommand(query, conn))
//            {
//                if (parameters != null)
//                    cmd.Parameters.AddRange(parameters);
//                conn.Open();
//                return cmd.ExecuteNonQuery();
//            }
//        }
//    }
//}



//using System;
//using System.Data;
//using System.Data.Common;
//using MySql.Data.MySqlClient;
//using System.Web;
//using System.Xml.Linq;

//namespace Report.Services
//{
//    public class Dal : IDal
//    {
//        private readonly string _connectionString;
//        private readonly string _dbType;

//        //public object MySql { get; private set; }

//        public Dal()
//        {
//            var path = HttpContext.Current.Server.MapPath("~/App_Data/DbConnection.xml");
//            var doc = XDocument.Load(path);
//            var connElement = doc.Root.Element("Connection");
//            _connectionString = connElement.Element("ConnectionString").Value;
//            _dbType = connElement.Element("DbType")?.Value ?? "SQLSERVER";
//        }

//        private DbProviderFactory GetFactory()
//        {
//            switch (_dbType.ToUpper())
//            {
//                case "MYSQL":
//                    //return MySql.Data.MySqlClient.MySqlClientFactory.Instance;
//                    return MySqlClientFactory.Instance;
//                case "SQLSERVER":
//                default:
//                    return System.Data.SqlClient.SqlClientFactory.Instance;
//            }
//        }

//        public DataTable ExecuteQuery(string query, params DbParameter[] parameters)
//        {
//            var factory = GetFactory();
//            using (var conn = factory.CreateConnection())
//            {
//                conn.ConnectionString = _connectionString;
//                using (var cmd = conn.CreateCommand())
//                {
//                    cmd.CommandText = query;
//                    if (parameters != null)
//                    {
//                        foreach (var p in parameters)
//                            cmd.Parameters.Add(p);
//                    }
//                    using (var da = factory.CreateDataAdapter())
//                    {
//                        da.SelectCommand = cmd;
//                        var dt = new DataTable();
//                        da.Fill(dt);
//                        return dt;
//                    }
//                }
//            }
//        }

//        public object ExecuteScalar(string query, params DbParameter[] parameters)
//        {
//            var factory = GetFactory();
//            using (var conn = factory.CreateConnection())
//            {
//                conn.ConnectionString = _connectionString;
//                using (var cmd = conn.CreateCommand())
//                {
//                    cmd.CommandText = query;
//                    if (parameters != null)
//                    {
//                        foreach (var p in parameters)
//                            cmd.Parameters.Add(p);
//                    }
//                    conn.Open();
//                    return cmd.ExecuteScalar();
//                }
//            }
//        }

//        public int ExecuteNonQuery(string query, params DbParameter[] parameters)
//        {
//            var factory = GetFactory();
//            using (var conn = factory.CreateConnection())
//            {
//                conn.ConnectionString = _connectionString;
//                using (var cmd = conn.CreateCommand())
//                {
//                    cmd.CommandText = query;
//                    if (parameters != null)
//                    {
//                        foreach (var p in parameters)
//                            cmd.Parameters.Add(p);
//                    }
//                    conn.Open();
//                    return cmd.ExecuteNonQuery();
//                }
//            }
//        }
//    }
//}


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
        private static readonly string _connectionString;
        private static readonly string _dbType;
        private static readonly TimeZoneInfo _istZone;

        private DbConnection _currentConnection;
        private DbTransaction _currentTransaction;

        static Dal()
        {
            var configPath = ReportConfig.DbConnectionConfigPath;
            var path = HttpContext.Current.Server.MapPath(configPath);
            var doc = XDocument.Load(path);
            var connElement = doc.Root.Element("Connection");
            _connectionString = connElement.Element("ConnectionString").Value;
            _dbType = connElement.Element("DbType")?.Value ?? "SQLSERVER";
            _istZone = TimeZoneInfo.FindSystemTimeZoneById(ReportConfig.TimezoneId);
        }

        // OLD: instance constructor (replaced by static constructor above)
        //public Dal()
        //{
        //    var path = HttpContext.Current.Server.MapPath("~/App_Data/DbConnection.xml");
        //    var doc = XDocument.Load(path);
        //    var connElement = doc.Root.Element("Connection");
        //    _connectionString = connElement.Element("ConnectionString").Value;
        //    _dbType = connElement.Element("DbType")?.Value ?? "SQLSERVER";
        //}

        public Dal()
        {
        }

        public string DatabaseType => _dbType;

        //string IDal.DatabaseType => throw new NotImplementedException();

        public string GetTopClause(int count)
        {
            if (_dbType.ToUpper() == "MYSQL")
                return $"LIMIT {count}";
            else
                return $"SELECT TOP {count}";
        }

        private DbProviderFactory GetFactory()
        {
            switch (_dbType.ToUpper())
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
            if (_dbType.ToUpper() == "MYSQL")
                return $"LIMIT {pageSize} OFFSET {offset}";
            else
                return $"OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";
        }

        public string GetIdentityFunction()
        {
            if (_dbType.ToUpper() == "MYSQL")
                return "LAST_INSERT_ID()";
            else
                return "SCOPE_IDENTITY()";
        }

        public string GetIdentityCastType()
        {
            if (_dbType.ToUpper() == "MYSQL")
                return "SIGNED";
            else
                return "INT";
        }

        public string GetNullFunction()
        {
            if (_dbType.ToUpper() == "MYSQL")
                return "IFNULL";
            else
                return "ISNULL";
        }

        public void BeginTransaction()
        {
            var factory = GetFactory();
            _currentConnection = factory.CreateConnection();
            _currentConnection.ConnectionString = _connectionString;
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

        #region OLD_SYNC (retained for reference — all callers use async variants)
        //public DataTable ExecuteQuery(string query, params DbParameter[] parameters)
        //{
        //    var factory = GetFactory();
        //    DbConnection conn = null;
        //    bool ownConnection = false;
        //    try
        //    {
        //        if (_currentTransaction != null)
        //            conn = _currentTransaction.Connection;
        //        else
        //        {
        //            conn = factory.CreateConnection();
        //            conn.ConnectionString = _connectionString;
        //            ownConnection = true;
        //        }
        //        using (var cmd = conn.CreateCommand())
        //        {
        //            cmd.CommandText = query;
        //            if (_currentTransaction != null)
        //                cmd.Transaction = _currentTransaction;
        //            if (parameters != null)
        //                foreach (var p in parameters)
        //                    cmd.Parameters.Add(p);
        //            using (var da = factory.CreateDataAdapter())
        //            {
        //                da.SelectCommand = cmd;
        //                var dt = new DataTable();
        //                da.Fill(dt);
        //                return dt;
        //            }
        //        }
        //    }
        //    finally
        //    {
        //        if (ownConnection && conn != null)
        //            conn.Dispose();
        //    }
        //}

        //public object ExecuteScalar(string query, params DbParameter[] parameters)
        //{
        //    var factory = GetFactory();
        //    DbConnection conn = null;
        //    bool ownConnection = false;
        //    try
        //    {
        //        if (_currentTransaction != null)
        //            conn = _currentTransaction.Connection;
        //        else
        //        {
        //            conn = factory.CreateConnection();
        //            conn.ConnectionString = _connectionString;
        //            ownConnection = true;
        //        }
        //        using (var cmd = conn.CreateCommand())
        //        {
        //            cmd.CommandText = query;
        //            if (_currentTransaction != null)
        //                cmd.Transaction = _currentTransaction;
        //            if (parameters != null)
        //                foreach (var p in parameters)
        //                    cmd.Parameters.Add(p);
        //            if (ownConnection)
        //                conn.Open();
        //            return cmd.ExecuteScalar();
        //        }
        //    }
        //    finally
        //    {
        //        if (ownConnection && conn != null)
        //            conn.Dispose();
        //    }
        //}

        //public int ExecuteNonQuery(string query, params DbParameter[] parameters)
        //{
        //    var factory = GetFactory();
        //    DbConnection conn = null;
        //    bool ownConnection = false;
        //    try
        //    {
        //        if (_currentTransaction != null)
        //            conn = _currentTransaction.Connection;
        //        else
        //        {
        //            conn = factory.CreateConnection();
        //            conn.ConnectionString = _connectionString;
        //            ownConnection = true;
        //        }
        //        using (var cmd = conn.CreateCommand())
        //        {
        //            cmd.CommandText = query;
        //            if (_currentTransaction != null)
        //                cmd.Transaction = _currentTransaction;
        //            if (parameters != null)
        //                foreach (var p in parameters)
        //                    cmd.Parameters.Add(p);
        //            if (ownConnection)
        //                conn.Open();
        //            return cmd.ExecuteNonQuery();
        //        }
        //    }
        //    finally
        //    {
        //        if (ownConnection && conn != null)
        //            conn.Dispose();
        //    }
        //}
        #endregion

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
                        conn.ConnectionString = _connectionString;
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
                            // OLD: SELECT queries should not be logged (only INSERT/UPDATE/DELETE)
                            //QueryLogger.Log(query);   
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
                        conn.ConnectionString = _connectionString;
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
                        // OLD: scalar queries (COUNT, MAX) should not be logged
                        //QueryLogger.Log(query);   
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
                        conn.ConnectionString = _connectionString;
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
                    conn.ConnectionString = _connectionString;
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
            // OLD (uncached timezone lookup):
            //var utc = DateTime.UtcNow;
            //var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            //return TimeZoneInfo.ConvertTimeFromUtc(utc, ist);

            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _istZone);
        }
    }
}
