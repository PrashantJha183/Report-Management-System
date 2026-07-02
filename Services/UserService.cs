using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Xml.Linq;
using Report.Models;

namespace Report.Services
{
    public class UserService
    {
        private static readonly string UsersXmlPath;
        private static readonly int Iterations;
        private static readonly object _lock = new object();

        static UserService()
        {
            UsersXmlPath = HttpContext.Current.Server.MapPath("~/App_Data/Users.xml");
            int.TryParse(WebConfigurationManager.AppSettings["PasswordHashIterations"], out var iters);
            Iterations = iters > 0 ? iters : 100000;
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
                var storedSalt = (string)userElement.Element("PasswordSalt");

                bool valid;
                bool needsMigration = false;

                if (string.IsNullOrEmpty(storedSalt))
                {
                    valid = storedPassword == password;
                    if (valid)
                        needsMigration = true;
                }
                else
                {
                    valid = VerifyPassword(password, storedPassword, storedSalt);
                }

                if (!valid) return Task.FromResult<ComMstUser>(null);

                if (needsMigration)
                {
                    var newSalt = GenerateSalt();
                    var newHash = HashPassword(password, newSalt);
                    userElement.Element("Password").Value = newHash;
                    if (userElement.Element("PasswordSalt") == null)
                        userElement.Add(new XElement("PasswordSalt", newSalt));
                    else
                        userElement.Element("PasswordSalt").Value = newSalt;

                    var changedOn = userElement.Element("PasswordChangedOn");
                    if (changedOn == null)
                        userElement.Add(new XElement("PasswordChangedOn", DateTime.UtcNow.ToString("o")));
                    else
                        changedOn.Value = DateTime.UtcNow.ToString("o");

                    doc.Save(UsersXmlPath);
                }

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
                var storedSalt = (string)userElement.Element("PasswordSalt");

                bool valid;
                if (string.IsNullOrEmpty(storedSalt))
                    valid = storedPassword == currentPassword;
                else
                    valid = VerifyPassword(currentPassword, storedPassword, storedSalt);

                if (!valid)
                    return Task.FromResult("ERROR:Current password is incorrect.");

                var newSalt = GenerateSalt();
                var newHash = HashPassword(newPassword, newSalt);

                userElement.Element("Password").Value = newHash;
                if (userElement.Element("PasswordSalt") == null)
                    userElement.Add(new XElement("PasswordSalt", newSalt));
                else
                    userElement.Element("PasswordSalt").Value = newSalt;

                var changedOn = userElement.Element("PasswordChangedOn");
                if (changedOn == null)
                    userElement.Add(new XElement("PasswordChangedOn", DateTime.UtcNow.ToString("o")));
                else
                    changedOn.Value = DateTime.UtcNow.ToString("o");

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
                    CreatedOn = DateTime.Parse((string)u.Element("CreatedOn") ?? DateTime.UtcNow.ToString("o")),
                    PasswordChangedOn = u.Element("PasswordChangedOn") != null
                        ? DateTime.Parse((string)u.Element("PasswordChangedOn"))
                        : (DateTime?)null
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
                                        var salt = GenerateSalt();
                                        var hash = HashPassword(change.NewValue, salt);
                                        newUser.Element("Password").Value = hash;
                                        newUser.Add(new XElement("PasswordSalt", salt));
                                        newUser.Add(new XElement("PasswordChangedOn", DateTime.UtcNow.ToString("o")));
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
                                        var salt = GenerateSalt();
                                        var hash = HashPassword(change.NewValue, salt);
                                        userElement.Element("Password").Value = hash;
                                        if (userElement.Element("PasswordSalt") == null)
                                            userElement.Add(new XElement("PasswordSalt", salt));
                                        else
                                            userElement.Element("PasswordSalt").Value = salt;
                                        if (userElement.Element("PasswordChangedOn") == null)
                                            userElement.Add(new XElement("PasswordChangedOn", DateTime.UtcNow.ToString("o")));
                                        else
                                            userElement.Element("PasswordChangedOn").Value = DateTime.UtcNow.ToString("o");
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

        public static string GenerateSalt()
        {
            var salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }
            return Convert.ToBase64String(salt);
        }

        public static string HashPassword(string password, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations))
            {
                return Convert.ToBase64String(pbkdf2.GetBytes(32));
            }
        }

        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            var hashBytes = Convert.FromBase64String(HashPassword(password, storedSalt));
            var storedBytes = Convert.FromBase64String(storedHash);

            int diff = hashBytes.Length ^ storedBytes.Length;
            for (int i = 0; i < hashBytes.Length && i < storedBytes.Length; i++)
                diff |= hashBytes[i] ^ storedBytes[i];

            return diff == 0;
        }
    }
}
