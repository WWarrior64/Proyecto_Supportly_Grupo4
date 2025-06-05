using Microsoft.AspNetCore.Mvc;

namespace Proyecto_Supportly.Controllers
{
    public class InicioController : Controller
    {
        public IActionResult Inicio()
        {
            return View();
        }
    }
}
