using Microsoft.AspNetCore.Mvc;

namespace Aurelyn.Portal.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
