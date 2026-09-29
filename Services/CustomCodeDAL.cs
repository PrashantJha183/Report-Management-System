using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web;
using Report.Models;

namespace Report.Services
{
    public class CustomCodeDAL
    {
        private string ConnectionString => HttpContext.Current?.Session?["ConnectionString"]?.ToString();

        private bool HasConnection => !string.IsNullOrWhiteSpace(ConnectionString);

        public async Task<List<DynamicCodeGridItem>> GetAllAsync(string searchText)
        {
            var items = new List<DynamicCodeGridItem>();
            if (!HasConnection)
                return items;

            using (var con = new SqlConnection(ConnectionString))
            {
                string sql = @"SELECT CustomCodeId, ControllerName, ActionName, Description
                               FROM CustomCode";

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    sql += @" WHERE ControllerName LIKE @search
                              OR ActionName LIKE @search
                              OR Description LIKE @search";
                }

                sql += " ORDER BY CustomCodeId DESC";

                var cmd = new SqlCommand(sql, con);
                if (!string.IsNullOrWhiteSpace(searchText))
                    cmd.Parameters.AddWithValue("@search", $"%{searchText.Trim()}%");

                await con.OpenAsync();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        items.Add(new DynamicCodeGridItem
                        {
                            DynamicCodeId = Convert.ToInt32(dr["CustomCodeId"]),
                            ControllerName = dr["ControllerName"]?.ToString(),
                            ActionName = dr["ActionName"]?.ToString(),
                            Description = dr["Description"]?.ToString()
                        });
                    }
                }
            }

            return items;
        }

        public async Task<IDEViewModel> GetByIdAsync(int id)
        {
            if (!HasConnection)
                return null;

            using (var con = new SqlConnection(ConnectionString))
            {
                string sql = @"SELECT CustomCodeId, ControllerName, ActionName,
                                      NameSpaces, CustomCode, Description
                               FROM CustomCode WHERE CustomCodeId = @Id";

                var cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Id", id);

                await con.OpenAsync();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    if (await dr.ReadAsync())
                    {
                        return new IDEViewModel
                        {
                            CodeID = Convert.ToInt32(dr["CustomCodeId"]),
                            ControllerName = dr["ControllerName"]?.ToString(),
                            ActionName = dr["ActionName"]?.ToString(),
                            Namespaces = dr["NameSpaces"]?.ToString(),
                            Code = dr["CustomCode"]?.ToString(),
                            Description = dr["Description"]?.ToString()
                        };
                    }
                }
            }

            return null;
        }

        public async Task<IDEViewModel> GetByControllerActionAsync(string controller, string action)
        {
            if (string.IsNullOrWhiteSpace(controller) || !HasConnection)
                return null;

            using (var con = new SqlConnection(ConnectionString))
            {
                string sql = @"SELECT TOP 1 CustomCodeId, ControllerName, ActionName,
                                      NameSpaces, CustomCode, Description
                               FROM CustomCode
                               WHERE LOWER(LTRIM(RTRIM(ControllerName))) = LOWER(@Controller)
                                 AND ( LOWER(LTRIM(RTRIM(ActionName))) = LOWER(@Action)
                                       OR (@Action = 'index' AND LEN(LTRIM(RTRIM(ISNULL(ActionName,'')))) = 0) )
                               ORDER BY CustomCodeId DESC";

                var cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Controller", controller ?? "");
                cmd.Parameters.AddWithValue("@Action", action ?? "");

                await con.OpenAsync();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    if (await dr.ReadAsync())
                    {
                        return new IDEViewModel
                        {
                            CodeID = Convert.ToInt32(dr["CustomCodeId"]),
                            ControllerName = dr["ControllerName"]?.ToString(),
                            ActionName = dr["ActionName"]?.ToString(),
                            Namespaces = dr["NameSpaces"]?.ToString(),
                            Code = dr["CustomCode"]?.ToString(),
                            Description = dr["Description"]?.ToString()
                        };
                    }
                }
            }

            return null;
        }

        public async Task<int> AddAsync(IDEViewModel model)
        {
            if (!HasConnection)
                return 0;

            using (var con = new SqlConnection(ConnectionString))
            {
                string sql = @"INSERT INTO CustomCode
                                (ControllerName, ActionName, NameSpaces, CustomCode, Description)
                                VALUES
                                (@ControllerName, @ActionName, @NameSpaces, @CustomCode, @Description);
                                SELECT SCOPE_IDENTITY();";

                var cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@ControllerName", model.ControllerName ?? "");
                cmd.Parameters.AddWithValue("@ActionName", model.ActionName ?? "");
                cmd.Parameters.AddWithValue("@NameSpaces", model.Namespaces ?? "");
                cmd.Parameters.AddWithValue("@CustomCode", model.Code ?? "");
                cmd.Parameters.AddWithValue("@Description", model.Description ?? "");

                await con.OpenAsync();
                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
        }

        public async Task UpdateAsync(IDEViewModel model)
        {
            if (!HasConnection)
                return;

            using (var con = new SqlConnection(ConnectionString))
            {
                string sql = @"UPDATE CustomCode
                               SET ControllerName = @ControllerName,
                                   ActionName = @ActionName,
                                   NameSpaces = @NameSpaces,
                                   CustomCode = @CustomCode,
                                   Description = @Description
                               WHERE CustomCodeId = @Id";

                var cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Id", model.CodeID);
                cmd.Parameters.AddWithValue("@ControllerName", model.ControllerName ?? "");
                cmd.Parameters.AddWithValue("@ActionName", model.ActionName ?? "");
                cmd.Parameters.AddWithValue("@NameSpaces", model.Namespaces ?? "");
                cmd.Parameters.AddWithValue("@CustomCode", model.Code ?? "");
                cmd.Parameters.AddWithValue("@Description", model.Description ?? "");

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            if (!HasConnection)
                return;

            using (var con = new SqlConnection(ConnectionString))
            {
                string sql = "DELETE FROM CustomCode WHERE CustomCodeId = @Id";
                var cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@Id", id);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
        }

public async Task<int> GetNextIdAsync()
        {
            if (!HasConnection)
                return 0;

            using (var con = new SqlConnection(ConnectionString))
            {
                string sql = "SELECT ISNULL(MAX(CustomCodeId), 0) + 1 FROM CustomCode";
                var cmd = new SqlCommand(sql, con);

                await con.OpenAsync();
                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }
        }

        public async Task<List<string>> GetRouteKeysAsync()
        {
            var keys = new List<string>();

            if (!HasConnection)
                return keys;

            using (var con = new SqlConnection(ConnectionString))
            {
                string sql = "SELECT ControllerName, ActionName FROM CustomCode";

                var cmd = new SqlCommand(sql, con);

                await con.OpenAsync();
                using (var dr = await cmd.ExecuteReaderAsync())
                {
                    while (await dr.ReadAsync())
                    {
                        string controller = dr["ControllerName"]?.ToString();
                        string action = dr["ActionName"]?.ToString();

                        if (string.IsNullOrWhiteSpace(controller))
                            continue;

                        if (controller.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                            controller = controller.Substring(0, controller.Length - "Controller".Length);

                        string effectiveAction = string.IsNullOrWhiteSpace(action) ? "Index" : action.Trim();
                        keys.Add(controller.Trim().ToLowerInvariant() + "/" + effectiveAction.ToLowerInvariant());
                    }
                }
            }

            return keys;
        }

        
    }
}