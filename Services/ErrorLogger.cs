using System;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;

namespace Report.Services
{
    public static class ErrorLogger
    {
        private static readonly object _lock = new object();

        public static void Log(Exception ex, string query)
        {
            try
            {
                var ist = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                var username = HttpContext.Current?.Session["Username"]?.ToString() ?? "unknown";
                var companyCode = HttpContext.Current?.Session["CompanyCode"]?.ToString() ?? "unknown";

                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var logDir = Path.Combine(baseDir, "App_Data", "ErrorLogs");
                Directory.CreateDirectory(logDir);

                var filePath = Path.Combine(logDir, $"ErrorLog_{ist:yyyy-MM-dd}.xml");

                lock (_lock)
                {
                    if (File.Exists(filePath))
                    {
                        var content = File.ReadAllText(filePath);
                        var insertPos = content.LastIndexOf("</ErrorLogs>", StringComparison.Ordinal);
                        if (insertPos >= 0)
                        {
                            var entry = $"  <Log Time=\"{ist:yyyy-MM-dd HH:mm:ss}\" User=\"{username}\" Database=\"{companyCode}\">\n" +
                                        $"    <Message><![CDATA[{ex.Message}]]></Message>\n" +
                                        $"    <StackTrace><![CDATA[{ex.StackTrace}]]></StackTrace>\n" +
                                        $"    <Query><![CDATA[{query}]]></Query>\n" +
                                        $"  </Log>\n";
                            content = content.Insert(insertPos, entry);
                            File.WriteAllText(filePath, content);
                        }
                    }
                    else
                    {
                        using (var writer = XmlWriter.Create(filePath, new XmlWriterSettings
                        {
                            Indent = true,
                            Encoding = Encoding.UTF8
                        }))
                        {
                            writer.WriteStartDocument();
                            writer.WriteStartElement("ErrorLogs");

                            writer.WriteStartElement("Log");
                            writer.WriteAttributeString("Time", $"{ist:yyyy-MM-dd HH:mm:ss}");
                            writer.WriteAttributeString("User", username);
                            writer.WriteAttributeString("Database", companyCode);

                            writer.WriteStartElement("Message");
                            writer.WriteCData(ex.Message);
                            writer.WriteEndElement();

                            writer.WriteStartElement("StackTrace");
                            writer.WriteCData(ex.StackTrace ?? "");
                            writer.WriteEndElement();

                            writer.WriteStartElement("Query");
                            writer.WriteCData(query);
                            writer.WriteEndElement();

                            writer.WriteEndElement();
                            writer.WriteEndElement();
                            writer.WriteEndDocument();
                        }
                    }
                }
            }
            catch
            {
                // Silently ignore — never break the calling operation
            }
        }
    }
}
