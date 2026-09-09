using System;
using System.Collections.Generic;
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
        private static readonly object _lock = new object();

        static UserService()
        {
            UsersXmlPath = HttpContext.Current.Server.MapPath("~/App_Data/Users.xml");
        }

        public Task<ComMstUser> ValidateUserAsync(string username, string password)
        {
            if (!File.Exists(UsersXmlPath)) return Task.FromResult<ComMstUser>(null);

            lock (_lock)
            {
                var doc = XDocument.Load(UsersXmlPath);
                var userElement = doc.Root?.Elements("User")
                    .FirstOrDefault(u =>
                        (string)u.Element("Username") == username &&
                        (string)u.Element("IsActive") == "true");

                if (userElement == null) return Task.FromResult<ComMstUser>(null);

                var storedPassword = (string)userElement.Element("Password");

                if (storedPassword != password)
                    return Task.FromResult<ComMstUser>(null);

                return Task.FromResult(new ComMstUser
                {
                    UserId = (int)userElement.Element("UserId"),
                    Username = (string)userElement.Element("Username"),
                    DisplayName = (string)userElement.Element("DisplayName"),
                    Role = (string)userElement.Element("Role"),
                    IsActive = (string)userElement.Element("IsActive") == "true",
                    CreatedOn = DateTime.Parse((string)userElement.Element("CreatedOn"))
                });
            }
        }

        public Task<string> ChangePasswordAsync(string username, string currentPassword, string newPassword)
        {
            if (!File.Exists(UsersXmlPath))
                return Task.FromResult("ERROR:Users configuration not found.");

            lock (_lock)
            {
                var doc = XDocument.Load(UsersXmlPath);
                var userElement = doc.Root?.Elements("User")
                    .FirstOrDefault(u => (string)u.Element("Username") == username);

                if (userElement == null)
                    return Task.FromResult("ERROR:User not found.");

                var storedPassword = (string)userElement.Element("Password");

                if (storedPassword != currentPassword)
                    return Task.FromResult("ERROR:Current password is incorrect.");

                userElement.Element("Password").Value = newPassword;

                doc.Save(UsersXmlPath);
            }

            return Task.FromResult("CHANGED");
        }

        public Task<UserGridViewModel> GetUsersAsync(UserGridViewModel filter)
        {
            filter = filter ?? new UserGridViewModel();

            var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
            var pageSize = 5;

            if (!File.Exists(UsersXmlPath))
            {
                filter.TotalRecords = 0;
                filter.Items = new List<UserGridItem>();
                return Task.FromResult(filter);
            }

            lock (_lock)
            {
                var doc = XDocument.Load(UsersXmlPath);
                var users = doc.Root?.Elements("User") ?? Enumerable.Empty<XElement>();

                var query = users.Select(u => new UserGridItem
                {
                    UserId = (int)u.Element("UserId"),
                    Username = (string)u.Element("Username") ?? "",
                    DisplayName = (string)u.Element("DisplayName") ?? "",
                    Role = (string)u.Element("Role") ?? "",
                    IsActive = (string)u.Element("IsActive") == "true",
                    CreatedOn = DateTime.Parse((string)u.Element("CreatedOn") ?? DateTime.UtcNow.ToString("o"))
                });

                if (!string.IsNullOrWhiteSpace(filter.SearchText))
                {
                    var search = filter.SearchText.Trim().ToLower();
                    query = query.Where(u =>
                        u.Username.ToLower().Contains(search) ||
                        u.DisplayName.ToLower().Contains(search) ||
                        u.Role.ToLower().Contains(search));
                }

                var sortCol = (filter.SortColumn ?? "UserId").ToLower();
                bool desc = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
                if (sortCol == "username")
                    query = desc ? query.OrderByDescending(u => u.Username) : query.OrderBy(u => u.Username);
                else if (sortCol == "displayname")
                    query = desc ? query.OrderByDescending(u => u.DisplayName) : query.OrderBy(u => u.DisplayName);
                else if (sortCol == "role")
                    query = desc ? query.OrderByDescending(u => u.Role) : query.OrderBy(u => u.Role);
                else if (sortCol == "isactive")
                    query = desc ? query.OrderByDescending(u => u.IsActive) : query.OrderBy(u => u.IsActive);
                else if (sortCol == "createdon")
                    query = desc ? query.OrderByDescending(u => u.CreatedOn) : query.OrderBy(u => u.CreatedOn);
                else
                    query = desc ? query.OrderByDescending(u => u.UserId) : query.OrderBy(u => u.UserId);

                filter.TotalRecords = query.Count();

                filter.Items = query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                filter.PageNumber = pageNumber;
                filter.PageSize = pageSize;
            }

            return Task.FromResult(filter);
        }

        public Task<UserSaveChangesResult> SaveChangesAsync(List<UserChange> changes, string mode)
        {
            var result = new UserSaveChangesResult { Status = "SAVED", IdMappings = new Dictionary<string, int>() };

            if (mode == "CANCEL")
            {
                result.Status = "CANCELLED";
                return Task.FromResult(result);
            }

            if (changes == null || changes.Count == 0)
            {
                result.Status = "NO_CHANGES";
                return Task.FromResult(result);
            }

            lock (_lock)
            {
                var doc = XDocument.Load(UsersXmlPath);
                var rows = changes.GroupBy(c => c.UserId);

                foreach (var rowGroup in rows)
                {
                    var userId = rowGroup.Key;

                    if (userId < 0)
                    {
                        var existingIds = doc.Root?.Elements("User")
                            .Select(u => (int)u.Element("UserId"))
                            .ToList() ?? new List<int>();
                        var newId = existingIds.Count > 0 ? existingIds.Max() + 1 : 1;

                        var newUser = new XElement("User",
                            new XElement("UserId", newId),
                            new XElement("Username", ""),
                            new XElement("Password", ""),
                            new XElement("DisplayName", ""),
                            new XElement("Role", ""),
                            new XElement("IsActive", "true"),
                            new XElement("CreatedOn", DateTime.UtcNow.ToString("o"))
                        );

                        foreach (var change in rowGroup)
                        {
                            switch (change.Column)
                            {
                                case "Username":
                                    newUser.Element("Username").Value = change.NewValue ?? "";
                                    break;
                                case "Password":
                                    if (!string.IsNullOrWhiteSpace(change.NewValue))
                                    {
                                        newUser.Element("Password").Value = change.NewValue;
                                    }
                                    break;
                                case "DisplayName":
                                    newUser.Element("DisplayName").Value = change.NewValue ?? "";
                                    break;
                                case "Role":
                                    newUser.Element("Role").Value = change.NewValue ?? "";
                                    break;
                                case "IsActive":
                                    newUser.Element("IsActive").Value = change.NewValue == "true" ? "true" : "false";
                                    break;
                            }
                        }

                        doc.Root.Add(newUser);
                        result.IdMappings[userId.ToString()] = newId;
                    }
                    else
                    {
                        var userElement = doc.Root?.Elements("User")
                            .FirstOrDefault(u => (int)u.Element("UserId") == userId);

                        if (userElement == null) continue;

                        foreach (var change in rowGroup)
                        {
                            switch (change.Column)
                            {
                                case "Username":
                                    userElement.Element("Username").Value = change.NewValue ?? "";
                                    break;
                                case "Password":
                                    if (!string.IsNullOrWhiteSpace(change.NewValue))
                                    {
                                        userElement.Element("Password").Value = change.NewValue;
                                    }
                                    break;
                                case "DisplayName":
                                    userElement.Element("DisplayName").Value = change.NewValue ?? "";
                                    break;
                                case "Role":
                                    userElement.Element("Role").Value = change.NewValue ?? "";
                                    break;
                                case "IsActive":
                                    userElement.Element("IsActive").Value = change.NewValue == "true" ? "true" : "false";
                                    break;
                            }
                        }
                    }
                }

                doc.Save(UsersXmlPath);
            }

            return Task.FromResult(result);
        }

        public Task<string> DeleteUserAsync(int userId)
        {
            if (!File.Exists(UsersXmlPath))
                return Task.FromResult("NOT_FOUND");

            lock (_lock)
            {
                var doc = XDocument.Load(UsersXmlPath);
                var userElement = doc.Root?.Elements("User")
                    .FirstOrDefault(u => (int)u.Element("UserId") == userId);

                if (userElement == null)
                    return Task.FromResult("NOT_FOUND");

                userElement.Element("IsActive").Value = "false";
                doc.Save(UsersXmlPath);
            }

            return Task.FromResult("DELETED");
        }

        public Task<string> ToggleUserActiveAsync(int userId)
        {
            if (!File.Exists(UsersXmlPath))
                return Task.FromResult("NOT_FOUND");

            lock (_lock)
            {
                var doc = XDocument.Load(UsersXmlPath);
                var userElement = doc.Root?.Elements("User")
                    .FirstOrDefault(u => (int)u.Element("UserId") == userId);

                if (userElement == null)
                    return Task.FromResult("NOT_FOUND");

                var current = (string)userElement.Element("IsActive");
                var newValue = current == "true" ? "false" : "true";
                userElement.Element("IsActive").Value = newValue;
                doc.Save(UsersXmlPath);

                return Task.FromResult(newValue == "true" ? "ACTIVATED" : "DEACTIVATED");
            }
        }

        public Task<int> GetNextUserIdAsync()
        {
            if (!File.Exists(UsersXmlPath))
                return Task.FromResult(1);

            lock (_lock)
            {
                var doc = XDocument.Load(UsersXmlPath);
                var maxId = doc.Root?.Elements("User")
                    .Select(u => (int)u.Element("UserId"))
                    .DefaultIfEmpty(0)
                    .Max() ?? 0;
                return Task.FromResult(maxId + 1);
            }
        }
    }
}
