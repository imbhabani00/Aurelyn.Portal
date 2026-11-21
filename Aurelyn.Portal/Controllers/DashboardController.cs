using Aurelyn.App.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Aurelyn.Portal.Controllers
{
    public class DashboardController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
