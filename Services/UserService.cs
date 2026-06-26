using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Report.Models;

namespace Report.Services
{
    public class UserService
    {
        private static readonly string UsersXmlPath;

        static UserService()
        {
            UsersXmlPath = HttpContext.Current.Server.MapPath("~/App_Data/Users.xml");
        }

        public async Task<ComMstUser> ValidateUserAsync(string username, string password)
        {
            if (!File.Exists(UsersXmlPath)) return null;

            var doc = XDocument.Load(UsersXmlPath);
            var userElement = doc.Root?.Elements("User")
                .FirstOrDefault(u =>
                    (string)u.Element("Username") == username &&
                    (string)u.Element("Password") == password &&
                    (string)u.Element("IsActive") == "true");

            if (userElement == null) return null;

            return new ComMstUser
            {
                UserId = (int)userElement.Element("UserId"),
                Username = (string)userElement.Element("Username"),
                Password = (string)userElement.Element("Password"),
                DisplayName = (string)userElement.Element("DisplayName"),
                Role = (string)userElement.Element("Role"),
                IsActive = (string)userElement.Element("IsActive") == "true",
                CreatedOn = DateTime.Parse((string)userElement.Element("CreatedOn"))
            };
        }
    }
}
