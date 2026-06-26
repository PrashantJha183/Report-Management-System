using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Xml.Linq;
using Report.Models;

namespace Report.Services
{
    public static class ReportConfig
    {
        private static string GetSetting(string key, string defaultValue)
        {
            var value = WebConfigurationManager.AppSettings[key];
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        private static int GetIntSetting(string key, int defaultValue)
        {
            var value = WebConfigurationManager.AppSettings[key];
            int result;
            if (int.TryParse(value, out result))
                return result;
            return defaultValue;
        }

        // ── Connection ──
        public static string DbConnectionConfigPath => GetSetting("DbConnectionConfigPath", "~/App_Data/DbConnection.xml");
        public static string TimezoneId => GetSetting("TimezoneId", "India Standard Time");

        // ── Table Names ──
        public static string ReportTable => GetSetting("ReportTable", "com_mst_report");
        public static string ReportColumnTable => GetSetting("ReportColumnTable", "com_mst_reportcolumn");
        public static string ReportFilteringColumnTable => GetSetting("ReportFilteringColumnTable", "com_mst_reportfilteringcolumn");
        public static string AlertConfigTable => GetSetting("AlertConfigTable", "com_mst_alertconfig");
        public static string AlertScheduleTable => GetSetting("AlertScheduleTable", "com_mst_alertschedule");
        public static string AlertConfigAttachmentTable => GetSetting("AlertConfigAttachmentTable", "com_mst_alertconfigattachment");
        public static string LinkItemTable => GetSetting("LinkItemTable", "com_mst_link_item");
        public static string CompanyConfigTable => GetSetting("CompanyConfigTable", "com_mst_companyconfig");
        public static string AuditLogTable => GetSetting("AuditLogTable", "com_mst_audit_log");

        // ── Page Sizes ──
        public static int ReportPageSize => GetIntSetting("ReportPageSize", 5);
        public static int ReportColumnPageSize => GetIntSetting("ReportColumnPageSize", 5);
        public static int ReportFilteringColumnPageSize => GetIntSetting("ReportFilteringColumnPageSize", 5);
        public static int AlertConfigPageSize => GetIntSetting("AlertConfigPageSize", 5);
        public static int AlertSchedulePageSize => GetIntSetting("AlertSchedulePageSize", 5);
        public static int AlertConfigAttachmentPageSize => GetIntSetting("AlertConfigAttachmentPageSize", 5);
        public static int CompanyConfigPageSize => GetIntSetting("CompanyConfigPageSize", 5);
        public static int MaxPageSize => GetIntSetting("MaxPageSize", 100);

        // ── DB-specific lock hints ──
        public static string GetTableLockHint(string dbType)
        {
            return dbType.ToUpper() == "MYSQL"
                ? "FOR UPDATE"
                : "WITH (TABLOCKX, HOLDLOCK)";
        }

        public static string WrapLockQuery(string tableName, string dbType, string aggregateExpression)
        {
            if (dbType.ToUpper() == "MYSQL")
                return $"SELECT {aggregateExpression} FROM {tableName} FOR UPDATE";
            else
                return $"SELECT {aggregateExpression} FROM {tableName} WITH (TABLOCKX, HOLDLOCK)";
        }

        public static List<ConnectionInfo> GetAllConnections()
        {
            var configPath = DbConnectionConfigPath;
            var path = HttpContext.Current.Server.MapPath(configPath);
            var doc = XDocument.Load(path);
            var connections = doc.Root?.Element("Connections")?.Elements("Connection");

            if (connections == null) return new List<ConnectionInfo>();

            return connections.Select(c => new ConnectionInfo
            {
                Name = (string)c.Element("Name"),
                ConnectionString = (string)c.Element("ConnectionString"),
                DbType = (string)c.Element("DbType")
            }).ToList();
        }
    }
}
