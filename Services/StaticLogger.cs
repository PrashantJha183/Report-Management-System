using System;
using System.IO;
using System.Web.Hosting;

namespace Report.Services
{
    public static class StaticLogger
    {
        private static readonly object _lock = new object();

        private static string LogDirectory
        {
            get
            {
                try
                {
                    var configured = System.Configuration.ConfigurationManager.AppSettings["LogDirectory"];
                    if (!string.IsNullOrWhiteSpace(configured))
                    {
                        var mapped = HostingEnvironment.MapPath(configured);
                        if (!string.IsNullOrEmpty(mapped))
                        {
                            Directory.CreateDirectory(mapped);
                            return mapped;
                        }
                    }
                }
                catch { }

                try
                {
                    var fallback = HostingEnvironment.MapPath("~/App_Data/Logs");
                    if (!string.IsNullOrEmpty(fallback))
                    {
                        Directory.CreateDirectory(fallback);
                        return fallback;
                    }
                }
                catch { }

                return Path.GetTempPath();
            }
        }

        public static void Log(string message, string context = "DynamicCode")
        {
            try
            {
                var dir = LogDirectory;
                var fileName = $"{context}_{DateTime.Now:yyyy-MM-dd}.log";
                var filePath = Path.Combine(dir, fileName);
                var entry = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";

                lock (_lock)
                {
                    File.AppendAllText(filePath, entry);
                }
            }
            catch { }
        }

        public static void LogError(Exception ex, string context = "DynamicCode")
        {
            try
            {
                var message = $"[ERROR] {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                if (ex.InnerException != null)
                    message += $"{Environment.NewLine}[INNER] {ex.InnerException.Message}{Environment.NewLine}{ex.InnerException.StackTrace}";

                Log(message, context);
            }
            catch { }
        }

        public static void LogError(string message, string context = "DynamicCode")
        {
            Log($"[ERROR] {message}", context);
        }
    }
}
