using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Proyecto_Supportly.Models;
using Proyecto_Supportly.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto_Supportly.Controllers
{
    public class AsignacionController : Controller
    {
        private readonly SupportDBContext _context;

        private IConfiguration _configuration;

        public AsignacionController(SupportDBContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// GET: /Asignacion/Details/5
        /// Muestra la vista Details.cshtml con toda la información del ticket,
        /// los dropdowns y también los adjuntos cargados desde la tabla Adjuntos.
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            // 1. Obtener el ticket
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            // 2. Nombre de la categoría
            var cat = await _context.Categorias.FindAsync(ticket.CategoriaID);
            ViewBag.CategoriaNombre = cat?.Nombre ?? "Desconocida";

            // 3. Nombre del estado
            var est = await _context.Estados.FindAsync(ticket.EstadoID);
            ViewBag.EstadoNombre = est?.Nombre ?? "Desconocido";

            // 4. Nombre del usuario creador
            var creador = await _context.Usuarios.FindAsync(ticket.UsuarioCreadorID);
            ViewBag.UsuarioCreadorNombre = creador?.Nombre ?? "Desconocido";

            // 5. Buscar explicitamente la asignación principal:
            var asignPrincipal = await _context.Asignaciones
                .Where(a => a.TicketID == id && a.ResponsablePrincipal)
                .OrderByDescending(a => a.FechaAsignacion)
                .FirstOrDefaultAsync();


            if (asignPrincipal != null)
            {
                var u = await _context.Usuarios.FindAsync(asignPrincipal.UsuarioAsignadoID);
                ViewBag.UsuarioAsignadoNombre = u?.Nombre ?? "Desconocido";
            }
            else
            {
                ViewBag.UsuarioAsignadoNombre = "Sin asignar";
            }

            // 6. Cargar los adjuntos (tabla Adjuntos)
            var archivos = await _context.Adjuntos
                .Where(a => a.TicketID == id)
                .Select(a => a.EnlaceDrive)
                .ToListAsync();
            ViewBag.Archivos = archivos;

            // 7. Prioridades fijas
            var prioridades = new List<string> { "Baja", "Media", "Alta" };
            ViewBag.Prioridades = prioridades
                .Select(p => new SelectListItem { Text = p, Value = p })
                .ToList();

            // 8. Drop‐down de usuarios internos
            ViewBag.UsuariosInternos = await _context.Usuarios
                .Where(u => u.TipoUsuario == "Interno")
                .OrderBy(u => u.Nombre)
                .Select(u => new SelectListItem
                {
                    Text = u.Nombre,
                    Value = u.UsuarioID.ToString()
                })
                .ToListAsync();

            ViewBag.Ticket = ticket;
            return View();
        }

        /// <summary>
        /// POST: /Asignacion/Assign
        /// Recibe ticketId, prioridad y usuarioAsignadoId.
        /// Actualiza la prioridad del ticket y crea una nueva entrada en Asignaciones.
        /// Luego redirige a Details para mostrar los cambios.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int ticketId, string prioridad, int usuarioAsignadoId)
        {
            // 1. Buscar el ticket
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Ticket no encontrado.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            try
            {
                // 2. Actualizar la prioridad
                ticket.Prioridad = prioridad;
                _context.Tickets.Update(ticket);

                // 3. Crear la asignación
                var asignacion = new Asignaciones
                {
                    TicketID = ticketId,
                    UsuarioAsignadoID = usuarioAsignadoId,
                    FechaAsignacion = DateTime.Now,
                    ResponsablePrincipal = true
                };
                _context.Asignaciones.Add(asignacion);

                // 4. Guardar cambios
                await _context.SaveChangesAsync();

                // ───────────────────────────────────────────────────────────────────────────
                // 5. Después de guardar, enviar correos:
                //    5.1. Obtener correo del empleado asignado
                var empleadoAsignado = await _context.Usuarios
                                                 .Where(u => u.UsuarioID == usuarioAsignadoId)
                                                 .Select(u => new { u.Nombre, u.Email })
                                                 .FirstOrDefaultAsync();

                //    5.2. Obtener correo del creador del ticket
                var creador = await _context.Usuarios
                                      .Where(u => u.UsuarioID == ticket.UsuarioCreadorID)
                                      .Select(u => new { u.Nombre, u.Email })
                                      .FirstOrDefaultAsync();

                //    5.3. Preparar instancia de servicio de correo
                correo servicioCorreo = new correo(_configuration);

                //    5.4. Armar asunto y cuerpo para el empleado asignado
                if (empleadoAsignado != null)
                {
                    string asuntoEmpleado = $"Se te ha asignado el ticket #{ticket.TicketID}";
                    string cuerpoEmpleado = $@"
                Hola {empleadoAsignado.Nombre},
                
                Se te ha asignado el ticket con ID: {ticket.TicketID}.
                Título del ticket: {ticket.Titulo ?? "(sin título)"}
                Prioridad asignada: {ticket.Prioridad}
                Fecha de asignación: {DateTime.Now:dd/MM/yyyy HH:mm}

                Por favor, ingresa al sistema para revisar los detalles y proceder.
                
                Saludos,
                Equipo de Soporte
            ";
                    servicioCorreo.enviar(empleadoAsignado.Email, asuntoEmpleado, cuerpoEmpleado);
                }

                //    5.5. Armar asunto y cuerpo para el creador del ticket
                if (creador != null)
                {
                    string asuntoCreador = $"Tu ticket #{ticket.TicketID} ha sido asignado";
                    string cuerpoCreador = $@"
                Hola {creador.Nombre},
                
                Tu ticket con ID: {ticket.TicketID} ha sido asignado a {empleadoAsignado?.Nombre ?? "un usuario"}.
                Título del ticket: {ticket.Titulo ?? "(sin título)"}
                Prioridad actual: {ticket.Prioridad}
                Fecha de asignación: {DateTime.Now:dd/MM/yyyy HH:mm}

                Puedes hacer seguimiento ingresando al sistema de soporte.
                
                Saludos,
                Equipo de Soporte
            ";
                    servicioCorreo.enviar(creador.Email, asuntoCreador, cuerpoCreador);
                }
                // ───────────────────────────────────────────────────────────────────────────


                TempData["SuccessMessage"] = "Ticket asignado correctamente.";

                // Redirigir al detalle de asignación para mostrar todos los datos, incluidos los adjuntos
                return RedirectToAction("VerDetalleAsignacion", "TicketAsignado", new { id = ticketId });
            }
            catch
            {
                TempData["ErrorMessage"] = "Ocurrió un error al asignar el ticket.";
            }

            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        /// <summary>
        /// POST: /Asignacion/Follow
        /// Simula la acción de “seguir” el ticket (por ejemplo, marcarlo para recibir notificaciones).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Follow(int ticketId)
        {
            // 1. Verificar que el ticket existe
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Ticket no encontrado.";
                return RedirectToAction(nameof(Details), new { id = ticketId });
            }

            // 2. Lógica de seguimiento (aquí solo un TempData)
            TempData["InfoMessage"] = "Ahora estás siguiendo este ticket.";

            return RedirectToAction(nameof(Details), new { id = ticketId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
