using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace Report.Services
{
    public class RoleService
    {
        private static readonly string RolesXmlPath;

        static RoleService()
        {
            RolesXmlPath = HttpContext.Current.Server.MapPath("~/App_Data/Roles.xml");
        }

        public bool HasPermission(string roleName, string controller, string action)
        {
            if (roleName == "SuperAdmin") return true;

            if (!File.Exists(RolesXmlPath)) return false;

            var doc = XDocument.Load(RolesXmlPath);
            var role = doc.Root?.Elements("Role")
                .FirstOrDefault(r => (string)r.Element("Name") == roleName);

            if (role == null) return false;

            var permission = $"{controller}.{action}";
            return role.Elements("Permissions")?.Elements("Permission")
                .Any(p => (string)p == permission) ?? false;
        }

        public List<string> GetAllRoleNames()
        {
            if (!File.Exists(RolesXmlPath)) return new List<string>();

            var doc = XDocument.Load(RolesXmlPath);
            return doc.Root?.Elements("Role")
                .Select(r => (string)r.Element("Name"))
                .ToList() ?? new List<string>();
        }
    }
}
