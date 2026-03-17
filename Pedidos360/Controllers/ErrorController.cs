using Microsoft.AspNetCore.Mvc;

namespace Pedidos360.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult Index(int? statusCode = null)
        {
            if (statusCode == null)
            {
                statusCode = 500;
            }

            ViewBag.StatusCode = statusCode;
            return View();
        }
    }
}