using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Aurelyn.App.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ErrorController : Controller
    {
        #region Properties
        private readonly ILogger<ErrorController> _logger;
        #endregion

        #region Constructor
        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }
        #endregion

        #region Index  (/Error/Index?code=404 - target of UseStatusCodePagesWithReExecute)
        [HttpGet]
        public IActionResult Index(int? code)
        {
            switch (code)
            {
                case 401:
                case 403:
                    return AccessDenied();
                case 404:
                    return NotFound404();
                case 500:
                    return ServerError();
            }

            ViewData["Code"] = code?.ToString() ?? "Error";
            ViewData["RequestId"] = GetRequestId();
            Response.StatusCode = code is >= 400 and < 600 ? code.Value : 500;
            return View("Index");
        }
        #endregion

        #region AccessDenied  (/Error/AccessDenied)
        [HttpGet]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View("AccessDenied");
        }
        #endregion

        #region NotFound  (/Error/NotFound)
        [HttpGet]
        [ActionName("NotFound")]
        public IActionResult NotFound404()
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            ViewData["RequestedPath"] = HttpContext.Features.Get<IStatusCodeReExecuteFeature>()?.OriginalPath;
            return View("NotFound");
        }
        #endregion

        #region ServerError  (/Error/ServerError - target of UseExceptionHandler)
        [HttpGet]
        public IActionResult ServerError()
        {
            var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (feature?.Error != null)
            {
                _logger.LogError(feature.Error, "Unhandled exception at {Path}", feature.Path);
            }

            Response.StatusCode = StatusCodes.Status500InternalServerError;
            ViewData["RequestId"] = GetRequestId();   // never expose exception details to the user
            return View("ServerError");
        }
        #endregion

        #region Helpers
        private string GetRequestId() => Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        #endregion
    }
}