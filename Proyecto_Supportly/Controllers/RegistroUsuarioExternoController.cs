using Microsoft.AspNetCore.Mvc;
using Proyecto_Supportly.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto_Supportly.Controllers
{
    public class RegistroUsuarioExternoController : Controller
    {
        private readonly SupportDBContext _context;

        public RegistroUsuarioExternoController(SupportDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: /RegistroUsuarioExterno/Index
        /// Muestra el formulario de registro para usuarios externos.
        /// </summary>
        [HttpGet]
        public IActionResult Index()
        {
            // Inicializar ViewBag con cadenas vacías para que la vista comience “limpia”
            ViewBag.NombreCompleto = "";
            ViewBag.Email = "";
            ViewBag.Empresa = "";
            ViewBag.ContactoPrincipal = "";
            ViewBag.TelefonoContacto = "";
            ViewBag.Mensaje = null;
            ViewBag.ErrorGeneral = null;
            return View();
        }

        /// <summary>
        /// POST: /RegistroUsuarioExterno/Index
        /// Procesa el registro de un nuevo usuario externo.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            string NombreCompleto,
            string Email,
            string Contraseña,
            string Empresa,
            string ContactoPrincipal,
            string TelefonoContacto)
        {
            // 1) Repoblar ViewBag para que, si hay error, la vista conserve los valores ingresados
            ViewBag.NombreCompleto = NombreCompleto;
            ViewBag.Email = Email;
            ViewBag.Empresa = Empresa;
            ViewBag.ContactoPrincipal = ContactoPrincipal;
            ViewBag.TelefonoContacto = TelefonoContacto;
            ViewBag.Mensaje = null;
            ViewBag.ErrorGeneral = null;

            bool hayError = false;

            // 2) Validaciones de campo "NombreCompleto"
            if (string.IsNullOrWhiteSpace(NombreCompleto))
            {
                ModelState.AddModelError("NombreCompleto", "El nombre completo es obligatorio.");
                hayError = true;
            }

            // 3) Validaciones de campo "Email"
            if (string.IsNullOrWhiteSpace(Email))
            {
                ModelState.AddModelError("Email", "El correo electrónico es obligatorio.");
                hayError = true;
            }
            else if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(Email))
            {
                ModelState.AddModelError("Email", "Ingrese un correo electrónico válido.");
                hayError = true;
            }
            else
            {
                // Verificar que no exista otro usuario con el mismo email
                bool yaExiste = _context.Usuarios.Any(u => u.Email == Email);
                if (yaExiste)
                {
                    ModelState.AddModelError("Email", "Este correo ya está registrado.");
                    hayError = true;
                }
            }

            // 4) Validaciones de campo "Contraseña"
            if (string.IsNullOrWhiteSpace(Contraseña) || Contraseña.Length < 6)
            {
                ModelState.AddModelError("Contraseña", "La contraseña debe tener al menos 6 caracteres.");
                hayError = true;
            }

            // Si hubo errores de validación, regresamos inmediatamente la Vista
            if (hayError)
            {
                return View();
            }

            try
            {
                // 5) Crear la entidad Usuarios
                var nuevoUsuario = new Usuarios
                {
                    Nombre = NombreCompleto,
                    Email = Email,
                    Contraseña = Contraseña,
                    Empresa = string.IsNullOrWhiteSpace(Empresa) ? null : Empresa,
                    NombreContactoPrincipal = string.IsNullOrWhiteSpace(ContactoPrincipal) ? null : ContactoPrincipal,
                    Telefono = string.IsNullOrWhiteSpace(TelefonoContacto) ? null : TelefonoContacto,
                    TipoUsuario = "Externo",
                    RolID = 3, // Ajusta este ID para “Usuario Externo” en tu tabla Roles
                    FechaCreacion = DateTime.Now,
                    EstadoUsuario = false // Queda pendiente de aprobación
                };

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                // 6) Mostrar mensaje de éxito y limpiar campos
                ViewBag.Mensaje = "Registro exitoso. Tu cuenta está pendiente de aprobación.";
                ViewBag.NombreCompleto = "";
                ViewBag.Email = "";
                ViewBag.Empresa = "";
                ViewBag.ContactoPrincipal = "";
                ViewBag.TelefonoContacto = "";
                return View();
            }
            catch
            {
                // 7) En caso de error al guardar en BD
                ViewBag.ErrorGeneral = "Ocurrió un error al intentar registrar el usuario. Intenta nuevamente.";
                return View();
            }
        }
    }
}
