using System.Web.Mvc;

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
            return View();
        }

        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Username and password are required.";
                return View("Index");
            }

            if (username == "admin" && password == "admin")
            {
                Session["IsLoggedIn"] = true;
                return RedirectToAction("Index", "Report");
            }

            ViewBag.Error = "Invalid username or password.";
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
