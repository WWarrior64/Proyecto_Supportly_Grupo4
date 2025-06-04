using Microsoft.AspNetCore.Mvc;
using Proyecto_Supportly.Models;
using Proyecto_Supportly.Servicios;
using System.ComponentModel.DataAnnotations;

namespace Proyecto_Supportly.Controllers
{

    public class UsuarioLoginController : Controller
    {
        private readonly SupportDBContext _DbContext;

        public UsuarioLoginController(SupportDBContext context)
        {
            _DbContext = context;
        }

        [Autenticacion]
        public IActionResult Index()
        {
            var nombreUsuario = HttpContext.Session.GetString("Nombre");

            //Retorno información a la vista por ViewBag y ViewData
            ViewData["nombre"] = nombreUsuario;
            return View();
        }

        public IActionResult Autenticar()
        {
            ViewData["ErrorMessage"] = "";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Autenticar(string txtemail, string txtpassword)
        {
            //Valido al usuario con la base de datos
            var usuario = (from u in _DbContext.Usuarios
                           where u.Email == txtemail
                           && u.Contraseña == txtpassword
                           select u).FirstOrDefault();

            //Si el usuario existe con todas sus validaciones
            if (usuario != null)
            {
                //Se crean las variables de sesión
                HttpContext.Session.SetInt32("UsuarioId", usuario.UsuarioID);
                HttpContext.Session.SetString("Nombre", usuario.Nombre);
                HttpContext.Session.SetString("Correo", usuario.Email);
                HttpContext.Session.SetString("Tipo de Usuario", usuario.TipoUsuario);

                //Se redirecciona al método de Index del controlador Home
                return RedirectToAction("Index", "Home");
            }

            ViewData["ErrorMessage"] = "Error, usuario invalido!!!!";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Autenticar", "UsuarioLogin");
        }
    }
}
