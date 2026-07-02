using System.Threading.Tasks;
using System.Web.Mvc;
using Report.Models;
using Report.Services;

namespace Report.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public ActionResult ChangePassword()
        {
            if (Session["IsLoggedIn"] == null || !(bool)Session["IsLoggedIn"])
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (Session["IsLoggedIn"] == null || !(bool)Session["IsLoggedIn"])
                return RedirectToAction("Index", "Home");

            if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                ModelState.AddModelError("CurrentPassword", "Current password is required.");

            if (string.IsNullOrWhiteSpace(model.NewPassword))
                ModelState.AddModelError("NewPassword", "New password is required.");
            else if (model.NewPassword.Length < 6)
                ModelState.AddModelError("NewPassword", "New password must be at least 6 characters.");

            if (model.NewPassword != model.ConfirmPassword)
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match.");

            if (model.CurrentPassword == model.NewPassword)
                ModelState.AddModelError("NewPassword", "New password must be different from current password.");

            if (!ModelState.IsValid)
                return View(model);

            var username = Session["Username"]?.ToString();
            var userService = new UserService();
            var result = await userService.ChangePasswordAsync(username, model.CurrentPassword, model.NewPassword);

            if (result == "CHANGED")
            {
                var auditLog = new AuditLogService();
                await auditLog.LogChangeAsync(
                    "Users",
                    int.Parse(Session["UserId"]?.ToString() ?? "0"),
                    "PASSWORD_CHANGE",
                    null,
                    username);

                ViewBag.Message = "Password changed successfully.";
                return View();
            }

            ModelState.AddModelError("CurrentPassword", result.Replace("ERROR:", ""));
            return View(model);
        }
    }
}
