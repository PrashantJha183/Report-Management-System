using System.Web.Mvc;

namespace Report.Controllers
{
    public class VehicleController : Controller
    {
        public ActionResult Index()
        {
            var v = new Vehicle { VehicleNo = "MH045655", Color = "White" };
            ViewBag.Header = "Vehicle Info";
            return View(v);
        }
    }
}