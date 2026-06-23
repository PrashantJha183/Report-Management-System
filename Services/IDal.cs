//using System;
//using System.Data;
//using System.Data.Common;

//namespace Report.Services
//{
//    public interface IDal
//    {
//        DataTable ExecuteQuery(string query, params DbParameter[] parameters);
//        object ExecuteScalar(string query, params DbParameter[] parameters);
//        int ExecuteNonQuery(string query, params DbParameter[] parameters);

//        string DatabaseType { get; }

//        DbParameter CreateParameter(string name, object value);

//        string GetPaginationClause(int offset, int pageSize);
//        string GetIdentityFunction();
//        string GetIdentityCastType();
//        string GetNullFunction();

//        void BeginTransaction();
//        void Commit();
//        void Rollback();

//        DateTime GetCurrentIstTime();
//    }
//}




using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Report.Services
{
    public interface IDal
    {
        //#region OLD_SYNC (commented — all callers use async)
        //DataTable ExecuteQuery(string query, params DbParameter[] parameters);
        //object ExecuteScalar(string query, params DbParameter[] parameters);
        //int ExecuteNonQuery(string query, params DbParameter[] parameters);
        //#endregion

        Task<DataTable> ExecuteQueryAsync(string query, params DbParameter[] parameters);
        Task<object> ExecuteScalarAsync(string query, params DbParameter[] parameters);
        Task<int> ExecuteNonQueryAsync(string query, params DbParameter[] parameters);
        Task<List<T>> ExecuteReaderAsync<T>(string query, Func<DbDataReader, T> map, params DbParameter[] parameters);
        Task<string> GetRowAsJsonAsync(string tableName, string pkColumn, object pkValue);

        string DatabaseType { get; }

        DbParameter CreateParameter(string name, object value);

        string GetPaginationClause(int offset, int pageSize);
        string GetTopClause(int count);
        string GetIdentityFunction();
        string GetIdentityCastType();
        string GetNullFunction();

        void BeginTransaction();
        void Commit();
        void Rollback();

        DateTime GetCurrentIstTime();
    }
}