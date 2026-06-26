using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Report.Services;

namespace Report.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            if (Session["IsLoggedIn"] != null && (bool)Session["IsLoggedIn"])
            {
                return RedirectToAction("Index", "Report");
            }

            var connections = ReportConfig.GetAllConnections();
            ViewBag.Connections = connections;

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Login(string username, string password, string connectionName)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Username and password are required.";
                ViewBag.Username = username;
                ViewBag.SelectedConnection = connectionName;
                ViewBag.Connections = ReportConfig.GetAllConnections();
                return View("Index");
            }

            if (string.IsNullOrWhiteSpace(connectionName))
            {
                ViewBag.Error = "Please select a database.";
                ViewBag.Username = username;
                ViewBag.SelectedConnection = connectionName;
                ViewBag.Connections = ReportConfig.GetAllConnections();
                return View("Index");
            }

            var userService = new UserService();
            var user = await userService.ValidateUserAsync(username, password);

            if (user != null)
            {
                var connections = ReportConfig.GetAllConnections();
                var selectedConnection = connections.FirstOrDefault(c => c.Name == connectionName);

                if (selectedConnection == null)
                {
                    ViewBag.Error = "Selected database not found.";
                    ViewBag.Username = username;
                    ViewBag.SelectedConnection = connectionName;
                    ViewBag.Connections = connections;
                    return View("Index");
                }

                Session["IsLoggedIn"] = true;
                Session["Username"] = user.Username;
                Session["DisplayName"] = user.DisplayName;
                Session["Role"] = user.Role;
                Session["UserId"] = user.UserId;
                Session["ConnectionString"] = selectedConnection.ConnectionString;
                Session["DbType"] = selectedConnection.DbType;
                Session["CompanyCode"] = selectedConnection.Name;

                return RedirectToAction("Index", "Report");
            }

            ViewBag.Error = "Invalid username or password.";
            ViewBag.Username = username;
            ViewBag.SelectedConnection = connectionName;
            ViewBag.Connections = ReportConfig.GetAllConnections();
            return View("Index");
        }

        [HttpPost]
        public ActionResult Logout()
        {
            Session["IsLoggedIn"] = null;
            Session.Clear();
            return RedirectToAction("Index");
        }
    }
}
