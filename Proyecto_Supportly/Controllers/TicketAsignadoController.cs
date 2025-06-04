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
    public class TicketAsignadoController : Controller
    {
        private readonly SupportDBContext _context;

        public TicketAsignadoController(SupportDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: /TicketAsignado/VerDetalleAsignacion/5
        /// Muestra toda la información del ticket asignado y prepara el dropdown
        /// para asignar un segundo empleado. Expone “Principal” y “Segundo” según el bit.
        /// </summary>
        public async Task<IActionResult> VerDetalleAsignacion(int id)
        {
            // 1. Obtener el ticket
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
                return NotFound();

            // 2. Nombre de la categoría
            var cat = await _context.Categorias.FindAsync(ticket.CategoriaID);
            ViewBag.CategoriaNombre = cat?.Nombre ?? "Desconocida";

            // 3. Nombre del estado
            var est = await _context.Estados.FindAsync(ticket.EstadoID);
            ViewBag.EstadoNombre = est?.Nombre ?? "Desconocido";

            // 4. Nombre del usuario creador
            var creador = await _context.Usuarios.FindAsync(ticket.UsuarioCreadorID);
            ViewBag.UsuarioCreador = creador;

            // 5. Buscar asignación “Principal” => ResponsablePrincipal = true
            var asignPrincipal = await _context.Asignaciones
                .Where(a => a.TicketID == id && a.ResponsablePrincipal==true)
                .OrderByDescending(a => a.FechaAsignacion) // en caso de varias, toma la más antigua
                .FirstOrDefaultAsync();

            if (asignPrincipal != null)
            {
                var uPrin = await _context.Usuarios.FindAsync(asignPrincipal.UsuarioAsignadoID);
                ViewBag.UsuarioAsignadoNombre = uPrin?.Nombre ?? "Desconocido";
            }
            else
            {
                ViewBag.UsuarioAsignadoNombre = "Sin asignar";
            }

            // 6. Buscar asignación “Secundaria” => ResponsablePrincipal = false
            var asignSegundo = await _context.Asignaciones
                .Where(a => a.TicketID == id && a.ResponsablePrincipal==false)
                .OrderByDescending(a => a.FechaAsignacion) // si hay varias, toma la primera añadida
                .FirstOrDefaultAsync();

            if (asignSegundo != null)
            {
                var uSec = await _context.Usuarios.FindAsync(asignSegundo.UsuarioAsignadoID);
                ViewBag.UsuarioSegundoAsignadoNombre = uSec?.Nombre ?? "Desconocido";
            }
            else
            {
                ViewBag.UsuarioSegundoAsignadoNombre = null;
            }

            // 7. Adjuntos (usa la entidad Adjuntos)
            var archivos = await _context.Adjuntos
                .Where(a => a.TicketID == id)
                .Select(a => a.EnlaceDrive)
                .ToListAsync();
            ViewBag.ArchivosAdjuntos = archivos;

            // 8. Progreso (ejemplo simple: 0=Abierto, 50=En Proceso, 100=Cerrado)
            int progreso = 0;

            // Obtenemos el nombre del estado a partir del EstadoID del ticket
            var estadoEntidad = await _context.Estados.FindAsync(ticket.EstadoID);
            string estadoNombre = estadoEntidad?.Nombre ?? "";

            // Asignamos un porcentaje según el estado
            switch (estadoNombre)
            {
                case "Abierto":
                    progreso = 0;
                    break;
                case "Pendiente":
                    progreso = 25;
                    break;
                case "En Progreso":
                    progreso = 50;
                    break;
                case "Resuelto":
                    progreso = 75;
                    break;
                case "Cerrado":
                    progreso = 100;
                    break;
                default:
                    progreso = 0;
                    break;
            }

            ViewBag.Progreso = progreso;

            // 9. Lista de usuarios internos disponibles
            var usuariosInternos = await _context.Usuarios
                .OrderBy(u => u.Nombre)
                .Select(u => new SelectListItem
                {
                    Text = u.Nombre,
                    Value = u.UsuarioID.ToString()
                })
                .ToListAsync();
            ViewBag.UsuariosInternosDisponibles = usuariosInternos;

            ViewBag.Ticket = ticket;
            return View();
        }

        /// <summary>
        /// POST: /TicketAsignado/AsignarSegundoEmpleado
        /// Recibe ticketId y usuarioAsignadoId, crea nueva Asignación y redirige.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AsignarSegundoEmpleado(int ticketId, int usuarioAsignadoId)
        {
            // Verificar que el ticket existe
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                TempData["Error"] = "Ticket no encontrado.";
                return RedirectToAction(nameof(VerDetalleAsignacion), new { id = ticketId });
            }

            // Verificar que el usuario existe
            var usuario = await _context.Usuarios.FindAsync(usuarioAsignadoId);
            if (usuario == null)
            {
                TempData["Error"] = "Empleado seleccionado no existe.";
                return RedirectToAction(nameof(VerDetalleAsignacion), new { id = ticketId });
            }

            try
            {
                // Crear la nueva asignación como secundario (ResponsablePrincipal=false)
                var asignacion = new Asignaciones
                {
                    TicketID = ticketId,
                    UsuarioAsignadoID = usuarioAsignadoId,
                    FechaAsignacion = DateTime.Now,
                    ResponsablePrincipal = false
                };
                _context.Asignaciones.Add(asignacion);
                await _context.SaveChangesAsync();

                TempData["Exito"] = "Segundo empleado asignado correctamente.";
            }
            catch
            {
                TempData["Error"] = "Ocurrió un error al asignar el segundo empleado.";
            }

            return RedirectToAction(nameof(VerDetalleAsignacion), new { id = ticketId });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _context.Dispose();
            base.Dispose(disposing);
        }
    }
}
