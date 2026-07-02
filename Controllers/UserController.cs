using Report.Models;
using Report.Services;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Report.Controllers
{
    public class UserController : Controller
    {
        private readonly UserService _userService;

        public UserController()
        {
            _userService = new UserService();
        }

        [HttpGet]
        public async Task<ActionResult> Index(UserGridViewModel model)
        {
            var result = await _userService.GetUsersAsync(model);
            ViewBag.NextIdentity = await _userService.GetNextUserIdAsync();
            ViewBag.TableName = "Users";
            return View(result);
        }

        [HttpPost]
        public async Task<JsonResult> SaveChanges(UserSaveChangesRequest request)
        {
            try
            {
                var result = await _userService.SaveChangesAsync(request.Changes, request.Mode);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new UserSaveChangesResult { Status = "ERROR:" + ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            var status = await _userService.DeleteUserAsync(id);
            if (status == "DELETED")
                return Json(new { Status = "DELETED" });
            return Json(new { Status = status });
        }

        [HttpPost]
        public async Task<JsonResult> ToggleActive(int id)
        {
            var status = await _userService.ToggleUserActiveAsync(id);
            return Json(new { Status = status });
        }
    }
}
