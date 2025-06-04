using Microsoft.AspNetCore.Mvc;
using Proyecto_Supportly.Models;

namespace Proyecto_Supportly.Controllers
{
    public class PrincipalController : Controller
    {
        private readonly SupportDBContext _context;

        public PrincipalController(SupportDBContext context)
        {
            _context = context;
        }

        public IActionResult LayoutPrincipal()
        {
            return View();
        }
    }
}
