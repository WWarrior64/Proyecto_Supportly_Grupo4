using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Supportly.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto_Supportly.Controllers
{
    public class CrearTicketController : Controller
    {
        private readonly SupportDBContext _context;

        public CrearTicketController(SupportDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: /CrearTicket/CrearTicket
        /// Prepara la vista para crear un nuevo ticket:
        /// - Toma datos del usuario autenticado (desde Session)
        /// - Carga dropdown de Categorias (áreas) y prioridades
        /// </summary>
        [HttpGet]
        public IActionResult CrearTicket()
        {
            // 1. Verificar que haya un UsuarioId en sesión
            var usuarioIdEnSesion = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioIdEnSesion == null)
            {
                // No está logueado → redirigir a Login
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            // 2. Obtener datos del usuario desde la BD
            var usuario = _context.Usuarios
                                  .FirstOrDefault(u => u.UsuarioID == usuarioIdEnSesion.Value);
            if (usuario == null)
            {
                // Si por alguna razón ya no existe en BD, limpiamos sesión y enviamos al login
                HttpContext.Session.Clear();
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            // 3. Pasar a ViewBag los datos de usuario (para mostrarlos como campos readonly)
            ViewBag.UsuarioNombres = usuario.Nombre;
            ViewBag.UsuarioEmail = usuario.Email;
            ViewBag.UsuarioTelefono = usuario.Telefono ?? "";
            ViewBag.UsuarioCreadorID = usuario.UsuarioID;

            // 4. Construir dropdown de “áreas” (Categorias)
            var listaCategorias = _context.Categorias
                                          .AsNoTracking()
                                          .OrderBy(c => c.Nombre)
                                          .ToList();
            ViewBag.AreasList = new SelectList(listaCategorias, "CategoriaID", "Nombre");

            // 5. Construir dropdown de prioridades (Baja / Media / Alta)
            var prioridades = new List<string> { "Baja", "Media", "Alta" };
            ViewBag.PrioridadesList = new SelectList(prioridades);

            // 6) tomar el enlace de TempData
            if (TempData.ContainsKey("DropboxLink"))
            {
                ViewBag.UploadMessage = TempData["UploadMessage"];
                ViewBag.MessageType = TempData["MessageType"];
                ViewBag.DropboxLink = TempData["DropboxLink"];
                ViewBag.UploadedFileName = TempData["UploadedFileName"];
            }

            // 7. Devolver un modelo vacío (o new Tickets()) para que la vista lo utilice
            return View(new Tickets());
        }

        /// <summary>
        /// POST: /CrearTicket/CrearTicket
        /// Recibe el ticket desde el formulario, completa campos faltantes y lo guarda en BD.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearTicket(Tickets ticket, string Area, string DropboxLink, string UploadedFileName)
        {
            // 1. Relevantar datos de usuario en sesión (para recargar en caso de error)
            var usuarioIdEnSesion = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioIdEnSesion == null)
            {
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            var usuario = _context.Usuarios
                                  .FirstOrDefault(u => u.UsuarioID == usuarioIdEnSesion.Value);
            if (usuario == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            // 2. Volver a poblar ViewBag con datos de usuario para mostrar en la vista
            ViewBag.UsuarioNombres = usuario.Nombre;
            ViewBag.UsuarioApellidos = usuario.NombreContactoPrincipal ?? "";
            ViewBag.UsuarioEmail = usuario.Email;
            ViewBag.UsuarioTelefono = usuario.Telefono ?? "";
            ViewBag.UsuarioCreadorID = usuario.UsuarioID;

            // 3. Volver a poblar dropdowns en caso de que la validación falle
            var listaCategorias = _context.Categorias
                                          .AsNoTracking()
                                          .OrderBy(c => c.Nombre)
                                          .ToList();
            ViewBag.AreasList = new SelectList(listaCategorias, "CategoriaID", "Nombre");

            var prioridades = new List<string> { "Baja", "Media", "Alta" };
            ViewBag.PrioridadesList = new SelectList(prioridades);

            // 4. Asignar los campos adicionales al ticket:
            ticket.UsuarioCreadorID = usuario.UsuarioID;

            // Convertir la cadena “Area” al entero correspondiente
            if (int.TryParse(Area, out var categoriaSeleccionada))
            {
                ticket.CategoriaID = categoriaSeleccionada;
            }
            else
            {
                ModelState.AddModelError("Area", "Debe seleccionar un área válida.");
            }

            // Fecha de creación y estado inicial (abierto = 1, por ejemplo)
            ticket.FechaCreacion = DateTime.Now;
            ticket.EstadoID = 1; // “Abierto” (ajusta según tus constantes en la DB)

            if (!ModelState.IsValid)
            {
                return View(ticket);
            }

            // 5. Guardar el ticket en la BD
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // 6. Ahora que ya existe ticket.TicketID, guardamos el adjunto si existió un DropboxLink
            if (!string.IsNullOrEmpty(DropboxLink) && !string.IsNullOrEmpty(UploadedFileName))
            {
                var nuevoAdjunto = new Adjuntos
                {
                    TicketID = ticket.TicketID,
                    EnlaceDrive = DropboxLink,
                    NombreArchivo = UploadedFileName,
                    FechaCreacion = DateTime.Now
                };
                _context.Adjuntos.Add(nuevoAdjunto);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Ticket creado correctamente.";
            return RedirectToAction("Index", "InterfazTicket");
        }
    }
}