using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Pedidos360.Models;

namespace Pedidos360.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            _logger.LogInformation("Usuario entró a Home/Index");
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error500()
        {
            _logger.LogError("Se produjo un error 500");
            return View();
        }

        public IActionResult Error404()
        {
            _logger.LogWarning("Página no encontrada (404)");
            return View();
        }

        public IActionResult ProbarError500()
        {
            throw new Exception("Error de prueba");
        }

    }
}