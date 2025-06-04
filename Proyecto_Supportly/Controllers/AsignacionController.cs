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
    public class AsignacionController : Controller
    {
        private readonly SupportDBContext _context;

        public AsignacionController(SupportDBContext context)
        {
            _context = context;
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
