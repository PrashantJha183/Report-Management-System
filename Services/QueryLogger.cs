using System;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;

namespace Report.Services
{
    public static class QueryLogger
    {
        private static readonly object _lock = new object();

        public static void Log(string query)
        {
            try
            {
                // Only log mutation queries (INSERT, UPDATE, DELETE)
                var trimmed = query.TrimStart();
                if (trimmed.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    var ist = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.UtcNow,
                        TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                    var username = HttpContext.Current?.Session["Username"]?.ToString() ?? "unknown";
                    var companyCode = HttpContext.Current?.Session["CompanyCode"]?.ToString() ?? "unknown";
                    var tableName = ExtractTableName(trimmed);

                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var logDir = Path.Combine(baseDir, "App_Data", "QueryLogs");
                    Directory.CreateDirectory(logDir);

                    var filePath = Path.Combine(logDir, $"QueryLog_{ist:yyyy-MM-dd}.xml");

                    lock (_lock)
                    {
                        if (File.Exists(filePath))
                        {
                            // Append to existing file — insert before closing </QueryLogs>
                            var content = File.ReadAllText(filePath);
                            var insertPos = content.LastIndexOf("</QueryLogs>", StringComparison.Ordinal);
                            if (insertPos >= 0)
                            {
                                var entry = $"  <Log Time=\"{ist:yyyy-MM-dd HH:mm:ss}\" User=\"{username}\" Database=\"{companyCode}\" Table=\"{tableName}\">\n" +
                                            $"    <![CDATA[{query}]]>\n" +
                                            $"  </Log>\n";
                                content = content.Insert(insertPos, entry);
                                File.WriteAllText(filePath, content);
                            }
                        }
                        else
                        {
                            // Create new file with root element
                            using (var writer = XmlWriter.Create(filePath, new XmlWriterSettings
                            {
                                Indent = true,
                                Encoding = Encoding.UTF8
                            }))
                            {
                                writer.WriteStartDocument();
                                writer.WriteStartElement("QueryLogs");

                                writer.WriteStartElement("Log");
                                writer.WriteAttributeString("Time", $"{ist:yyyy-MM-dd HH:mm:ss}");
                                writer.WriteAttributeString("User", username);
                                writer.WriteAttributeString("Database", companyCode);
                                writer.WriteAttributeString("Table", tableName);
                                writer.WriteCData(query);
                                writer.WriteEndElement();

                                writer.WriteEndElement();
                                writer.WriteEndDocument();
                            }
                        }
                    }
                }
            }
            catch
            {
                // Silently ignore — never break the calling operation
            }
        }

        private static string ExtractTableName(string trimmed)
        {
            if (trimmed.StartsWith("INSERT INTO", StringComparison.OrdinalIgnoreCase))
            {
                var after = trimmed.Substring("INSERT INTO".Length).TrimStart();
                var spaceIdx = after.IndexOf(' ');
                var parenIdx = after.IndexOf('(');
                var endIdx = spaceIdx > 0 && parenIdx > 0 ? Math.Min(spaceIdx, parenIdx) :
                             spaceIdx > 0 ? spaceIdx :
                             parenIdx > 0 ? parenIdx : after.Length;
                return after.Substring(0, endIdx);
            }
            if (trimmed.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            {
                var after = trimmed.Substring("UPDATE".Length).TrimStart();
                var spaceIdx = after.IndexOf(' ');
                return spaceIdx > 0 ? after.Substring(0, spaceIdx) : after;
            }
            if (trimmed.StartsWith("DELETE FROM", StringComparison.OrdinalIgnoreCase))
            {
                var after = trimmed.Substring("DELETE FROM".Length).TrimStart();
                var spaceIdx = after.IndexOf(' ');
                return spaceIdx > 0 ? after.Substring(0, spaceIdx) : after;
            }
            return "unknown";
        }
    }
}