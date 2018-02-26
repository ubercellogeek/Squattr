using System.Web.Mvc;

namespace Squattr.RESTAPI.API.Controllers.MVC
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
