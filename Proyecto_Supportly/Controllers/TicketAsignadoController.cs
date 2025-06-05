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
    public class TicketAsignadoController : Controller
    {
        private readonly SupportDBContext _context;
        private IConfiguration _configuration;

        public TicketAsignadoController(SupportDBContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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
                .Where(u => u.TipoUsuario == "Interno")
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

                // ─────────────────────────────────────────────────────────────────────
                // 3. Crear notificaciones:

                // 3.1. Notificar al nuevo empleado asignado
                var notEmpleado = new Notificaciones
                {
                    TicketID = ticketId,
                    UsuarioDestinatarioID = usuarioAsignadoId,
                    Tipo = "Asignación Secundaria",
                    Mensaje = $"Se te ha asignado como segundo responsable del ticket #{ticket.TicketID}.",
                    FechaEnvio = DateTime.Now
                };
                _context.Notificaciones.Add(notEmpleado);

                // 3.2. Notificar al creador del ticket (cliente)
                var notCreador = new Notificaciones
                {
                    TicketID = ticketId,
                    UsuarioDestinatarioID = ticket.UsuarioCreadorID,
                    Tipo = "Asignación Secundaria",
                    Mensaje = $"Tu ticket #{ticket.TicketID} ha recibido un segundo responsable (ID: {usuarioAsignadoId}).",
                    FechaEnvio = DateTime.Now
                };
                _context.Notificaciones.Add(notCreador);

                // 3.3. Guardar las notificaciones
                await _context.SaveChangesAsync();

                // ─────────────────────────────────────────────────────────────────────

                // 4. Enviar correos:

                // 4.1. Obtener correo del nuevo empleado asignado
                var empleadoAsignado = await _context.Usuarios
                    .Where(u => u.UsuarioID == usuarioAsignadoId)
                    .Select(u => new { u.Nombre, u.Email })
                    .FirstOrDefaultAsync();

                // 4.2. Obtener correo del creador del ticket
                var creador = await _context.Usuarios
                    .Where(u => u.UsuarioID == ticket.UsuarioCreadorID)
                    .Select(u => new { u.Nombre, u.Email })
                    .FirstOrDefaultAsync();

                // 4.3. Instanciar servicio de correo
                var servicioCorreo = new correo(_configuration);

                // 4.4. Armar asunto y cuerpo para el nuevo empleado asignado
                if (empleadoAsignado != null && !string.IsNullOrWhiteSpace(empleadoAsignado.Email))
                {
                    string asuntoEmpleado = $"Se te ha asignado como segundo responsable del ticket #{ticket.TicketID}";
                    string cuerpoEmpleado = $@"
Hola {empleadoAsignado.Nombre},

Has sido asignado como segundo responsable para el ticket con ID: {ticket.TicketID}.
Título del ticket: {ticket.Titulo}
Prioridad: {ticket.Prioridad}
Fecha de asignación: {DateTime.Now:dd/MM/yyyy HH:mm}

Por favor, ingresa al sistema para revisar los detalles y gestionar lo que corresponda.

Saludos,
Equipo de Soporte
";
                    servicioCorreo.enviar(empleadoAsignado.Email, asuntoEmpleado, cuerpoEmpleado);
                }

                // 4.5. Armar asunto y cuerpo para el creador del ticket
                if (creador != null && !string.IsNullOrWhiteSpace(creador.Email))
                {
                    string asuntoCreador = $"Tu ticket #{ticket.TicketID} tiene un segundo responsable asignado";
                    string cuerpoCreador = $@"
Hola {creador.Nombre},

Tu ticket con ID: {ticket.TicketID} ahora cuenta con un segundo responsable (Empleado: {empleadoAsignado?.Nombre ?? "ID " + usuarioAsignadoId}).
Título del ticket: {ticket.Titulo}
Prioridad: {ticket.Prioridad}
Fecha de asignación: {DateTime.Now:dd/MM/yyyy HH:mm}

Puedes ingresar al sistema para ver quién es y hacer seguimiento.

Saludos,
Equipo de Soporte
";
                    servicioCorreo.enviar(creador.Email, asuntoCreador, cuerpoCreador);
                }
                // ─────────────────────────────────────────────────────────────────────

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
