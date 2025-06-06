using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

            // 2) Si no existe, devolvemos error
            if (usuario == null)
            {
                ViewData["ErrorMessage"] = "Error, usuario inválido.";
                return View();
            }

            // 3) Si existe, verificamos si está activo (campo booleano Activo == true)
            if (!usuario.EstadoUsuario)
            {
                ViewData["ErrorMessage"] = "Tu cuenta está inactiva. Contacta al administrador.";
                return View();
            }

            //Si el usuario existe con todas sus validaciones 
            if (usuario != null)
            {
                // Se crean las variables de sesión
                HttpContext.Session.SetInt32("UsuarioId", usuario.UsuarioID);
                HttpContext.Session.SetString("Nombre", usuario.Nombre);
                HttpContext.Session.SetString("Correo", usuario.Email);
                HttpContext.Session.SetInt32("RolId", usuario.RolID);
                HttpContext.Session.SetString("TipoUsuario", usuario.TipoUsuario);

                // Obtener nombre del rol desde BD para asegurarnos
                var rol = _DbContext.Roles
                            .AsNoTracking()
                            .FirstOrDefault(r => r.RolID == usuario.RolID)?.Nombre;

                // Redirigir según rol:
                if (string.Equals(rol, "Administrador", StringComparison.OrdinalIgnoreCase))
                {
                    // Si es Administrador → Dashboard/Index
                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    // Si NO es Administrador (empleado/técnico) → InterfazTicket/Index
                    return RedirectToAction("Index", "InterfazTicket");
                }
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
