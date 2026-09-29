//using System;
//using System.Collections.Concurrent;
//using System.Text.RegularExpressions;
//using System.Threading.Tasks;

//namespace Report.Services
//{
//    public static class DynamicRouteCache
//    {
//        private static readonly ConcurrentDictionary<string, byte> _routes =
//            new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

//        public static bool Contains(string controller, string action)
//        {
//            string key = BuildKey(controller, action);
//            return !string.IsNullOrEmpty(key) && _routes.ContainsKey(key);
//        }

//        public static void Clear()
//        {
//            _routes.Clear();
//        }

//        public static async Task RefreshAsync()
//        {
//            var next = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

//            try
//            {
//                var dal = new CustomCodeDAL();
//                var keys = await dal.GetRouteKeysAsync();

//                if (keys != null)
//                {
//                    foreach (var key in keys)
//                    {
//                        if (!string.IsNullOrWhiteSpace(key))
//                            next.TryAdd(key.Trim(), 1);
//                    }
//                }

//                StaticLogger.Log($"DynamicRouteCache.Refresh: {next.Count} routes cached");
//            }
//            catch (Exception ex)
//            {
//                StaticLogger.Log("DynamicRouteCache.Refresh: " + ex.Message);
//            }

//            Swap(next);
//        }

//        private static string BuildKey(string controller, string action)
//        {
//            if (string.IsNullOrWhiteSpace(controller))
//                return null;

//            string c = Regex.Replace(controller.Trim(), "Controller$", "", RegexOptions.IgnoreCase);
//            string a = string.IsNullOrWhiteSpace(action) ? "Index" : action.Trim();
//            return c.ToLowerInvariant() + "/" + a.ToLowerInvariant();
//        }

//        private static void Swap(ConcurrentDictionary<string, byte> next)
//        {
//            _routes.Clear();
//            foreach (var kvp in next)
//                _routes.TryAdd(kvp.Key, 1);
//        }
//    }
//}