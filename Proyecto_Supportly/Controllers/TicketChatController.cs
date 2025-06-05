using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Proyecto_Supportly.Models;
using Proyecto_Supportly.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto_Supportly.Controllers
{
    public class TicketChatController : Controller
    {
        private readonly SupportDBContext _context;

        private IConfiguration _configuration;

        public TicketChatController(SupportDBContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// GET: /TicketChat/Chat/5?userId=42
        /// Muestra los comentarios y adjuntos de un ticket específico.
        /// </summary>
        public async Task<IActionResult> Chat(int id, int userId)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UsuarioId");
            if (sessionUserId == null)
                return RedirectToAction("Autenticar", "UsuarioLogin");

            // 1) Traer al usuario de la sesión (sin Include, basta con FindAsync o FirstOrDefaultAsync):
            var usuarioSesion = await _context.Usuarios
                                             .FirstOrDefaultAsync(u => u.UsuarioID == sessionUserId.Value);
            if (usuarioSesion == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            // 2) Con el RolID del usuario, obtengo el nombre del rol:
            var rolEnSesion = await _context.Roles
                                           .FindAsync(usuarioSesion.RolID);
            ViewBag.IsSoporte = (rolEnSesion?.Nombre == "Soporte Técnico");

            ViewBag.SessionUserId = sessionUserId.Value;

            // 1. Verificar que el ticket exista
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
                return NotFound();

            // 2. Obtener datos del creador (para mostrar la empresa o su nombre)
            var creador = await _context.Usuarios.FindAsync(ticket.UsuarioCreadorID);
            ViewBag.CompanyName = creador?.Empresa ?? "(Sin empresa)";

            // 3. Datos básicos del ticket
            ViewBag.TicketId = ticket.TicketID;
            ViewBag.TicketTitle = ticket.Titulo;
            ViewBag.CreationDate = ticket.FechaCreacion.ToString("yyyy-MM-dd HH:mm");
            ViewBag.TicketPriority = ticket.Prioridad;
            ViewBag.TicketDescription = ticket.Descripcion;
            var estado = await _context.Estados.FindAsync(ticket.EstadoID);
            ViewBag.TicketStatus = estado?.Nombre ?? "Desconocido";

            // Cargar lista de estados para el dropdown y marcar el actual:
            var estadosLista = await _context.Estados
                                             .OrderBy(e => e.Nombre)
                                             .ToListAsync();
            ViewBag.EstadosList = new SelectList(estadosLista, "EstadoID", "Nombre", ticket.EstadoID);
            ViewBag.CurrentEstadoID = ticket.EstadoID;

            // 4. Cargar todos los comentarios de este ticket
            var comentarios = await _context.Comentarios
                .Where(c => c.TicketID == id)
                .OrderBy(c => c.FechaComentario)
                .ToListAsync();

            // 5. Proyectar comentarios a una lista dinámica para la vista
            var mensajes = new List<dynamic>();
            foreach (var cm in comentarios)
            {
                var usuarioAutor = await _context.Usuarios.FindAsync(cm.UsuarioID);
                mensajes.Add(new
                {
                    IsUserMessage = (cm.UsuarioID == userId),
                    SenderName = usuarioAutor?.Nombre ?? "Desconocido",
                    Content = cm.Contenido,
                    Timestamp = cm.FechaComentario.ToString("yyyy-MM-dd HH:mm")
                });
            }

            // 6. Cargar adjuntos del ticket y agregarlos como “mensajes” al final
            //    Como cada adjunto no tiene UsuarioID, asumimos que lo subió el creador del ticket
            var adjuntos = await _context.Adjuntos
                .Where(a => a.TicketID == id)
                .OrderBy(a => a.FechaCreacion)
                .ToListAsync();

            foreach (var adj in adjuntos)
            {
                mensajes.Add(new
                {
                    // Si el creador del ticket coincide con el usuario en sesión, es mensaje de usuario:
                    IsUserMessage = (ticket.UsuarioCreadorID == userId),
                    SenderName = creador?.Nombre ?? "Usuario",
                    Content = $"SCREENSHOT:{adj.EnlaceDrive}",
                    Timestamp = adj.FechaCreacion.ToString("yyyy-MM-dd HH:mm")
                });
            }

            // 7. Pasar la lista de “mensajes” a la vista
            ViewBag.ChatMessages = mensajes;
            ViewBag.UserLoggedInName = (await _context.Usuarios.FindAsync(userId))?.Nombre ?? "Invitado";

            if (TempData.ContainsKey("DropboxLink"))
            {
                ViewBag.UploadMessage = TempData["UploadMessage"];
                ViewBag.MessageType = TempData["MessageType"];
                ViewBag.DropboxLink = TempData["DropboxLink"];
                ViewBag.UploadedFileName = TempData["UploadedFileName"];
            }

            return View();
        }

        /// <summary>
        /// POST: /TicketChat/UpdateStatus
        /// Cambia el estado del ticket al seleccionado.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int ticketId, int userId, int newEstadoID)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
                return NotFound();

            ticket.EstadoID = newEstadoID;
            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();

            // ───────────────────────────────────────────────────────────────────────────
            // 3) Enviar correo al “receptor” (el usuario al que se le está respondiendo)
            // Determinamos: si quien ejecuta la acción es el creador, entonces notificamos al asignado; 
            // si es el asignado (Soporte), notificamos al creador.

            // 3.1) Obtener datos del usuario que ejecutó (quien hizo el POST)
            //      userId viene del formulario como “quien está enviando”
            var usuarioEmisor = await _context.Usuarios.FindAsync(userId);

            // 3.2) Recuperar a quién notificar
            int destinatarioId;
            // Si quien cambia estado ES el creador, el destinatario será el soporte asignado (si existe)
            if (userId == ticket.UsuarioCreadorID)
            {
                // Buscamos la última asignación principal (ResponsablePrincipal = true)
                var ultimaAsign = await _context.Asignaciones
                    .Where(a => a.TicketID == ticketId && a.ResponsablePrincipal)
                    .OrderByDescending(a => a.FechaAsignacion)
                    .FirstOrDefaultAsync();

                destinatarioId = ultimaAsign != null
                    ? ultimaAsign.UsuarioAsignadoID
                    : -1;
            }
            else
            {
                // Si quien cambia estado NO es el creador, asumimos que es Soporte, entonces notificamos al creador.
                destinatarioId = ticket.UsuarioCreadorID;
            }

            if (destinatarioId > 0)
            {
                var destinatario = await _context.Usuarios.FindAsync(destinatarioId);
                if (destinatario != null && !string.IsNullOrWhiteSpace(destinatario.Email))
                {
                    // Obtener nombre del estado
                    var estadoNuevo = await _context.Estados
                        .Where(e => e.EstadoID == newEstadoID)
                        .Select(e => e.Nombre)
                        .FirstOrDefaultAsync();

                    // Construir asunto y cuerpo
                    string asunto = $"Ticket #{ticket.TicketID}: estado actualizado a \"{estadoNuevo}\"";
                    string cuerpo = $@"
                        Hola {destinatario.Nombre},

                        {(userId == ticket.UsuarioCreadorID
                            ? $"El creador del ticket (ID {ticket.TicketID}) cambió el estado a \"{estadoNuevo}\"."
                            : $"El técnico asignado ({usuarioEmisor.Nombre}) cambió el estado a \"{estadoNuevo}\".")}

                        Título: {ticket.Titulo}
                        Descripción: {ticket.Descripcion}
                        Fecha de cambio: {DateTime.Now:dd/MM/yyyy HH:mm}

                        Ingresa al sistema para más detalles.

                        Saludos,
                        Equipo de Soporte
                    ";

                    // Enviar correo
                    var servicioCorreo = new correo(_configuration);
                    servicioCorreo.enviar(destinatario.Email, asunto, cuerpo);
                }
            }
            // ───────────────────────────────────────────────────────────────────────────


            return RedirectToAction("Chat", new { id = ticketId, userId = userId });
        }

        /// <summary>
        /// POST: /TicketChat/SendMessage
        /// Agrega un nuevo comentario al ticket.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int ticketId, int userId, string messageContent, string DropboxLink, string UploadedFileName)
        {
            if (ticketId <= 0 || userId <= 0 || string.IsNullOrWhiteSpace(messageContent))
                return RedirectToAction("Chat", new { id = ticketId, userId = userId });

            // Confirmar que el ticket existe
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
                return NotFound();

            // Crear el comentario asignándole el userId que llegó desde el formulario
            var comentario = new Comentarios
            {
                TicketID = ticketId,
                UsuarioID = userId,
                Contenido = messageContent.Trim(),
                FechaComentario = DateTime.Now
            };
            _context.Comentarios.Add(comentario);
            await _context.SaveChangesAsync();

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

            // ───────────────────────────────────────────────────────────────────────────
            // 4) Enviar correo al “receptor” (el otro participante)
            // Determinamos: si el emisor ES el creador, entonces notificamos al Soporte Asignado; 
            // si el emisor NO es el creador, asumimos que es Soporte y notificamos al creador.

            // 4.1) Obtener datos del emisor
            var usuarioEmisor = await _context.Usuarios.FindAsync(userId);

            // 4.2) Determinar destinatario
            int destinatarioId;
            if (userId == ticket.UsuarioCreadorID)
            {
                // El creador envió el mensaje -> notificamos al soporte asignado
                var ultimaAsign = await _context.Asignaciones
                    .Where(a => a.TicketID == ticketId && a.ResponsablePrincipal)
                    .OrderByDescending(a => a.FechaAsignacion)
                    .FirstOrDefaultAsync();

                destinatarioId = ultimaAsign != null
                    ? ultimaAsign.UsuarioAsignadoID
                    : -1;
            }
            else
            {
                // El que envía NO es el creador, asumimos que es Soporte -> notificamos al creador
                destinatarioId = ticket.UsuarioCreadorID;
            }

            if (destinatarioId > 0)
            {
                var destinatario = await _context.Usuarios.FindAsync(destinatarioId);
                if (destinatario != null && !string.IsNullOrWhiteSpace(destinatario.Email))
                {
                    string asunto = $"Nuevo mensaje en Ticket #{ticket.TicketID}";
                    string cuerpo = $@"
                        Hola {destinatario.Nombre},

                        {(userId == ticket.UsuarioCreadorID
                            ? $"El cliente ({usuarioEmisor.Nombre}) ha escrito un mensaje en el ticket #{ticket.TicketID}."
                            : $"El técnico ({usuarioEmisor.Nombre}) ha respondido en el ticket #{ticket.TicketID}.")}

                        Mensaje:
                        ------------------------------
                        {messageContent.Trim()}
                        ------------------------------

                        Fecha y hora: {DateTime.Now:dd/MM/yyyy HH:mm}

                        Ingresa al sistema para ver la conversación completa y responder si es necesario.

                        Saludos,
                        Equipo de Soporte
                    ";

                    // Envío de correo
                    var servicioCorreo = new correo(_configuration);
                    servicioCorreo.enviar(destinatario.Email, asunto, cuerpo);
                }
            }
            // ───────────────────────────────────────────────────────────────────────────


            return RedirectToAction("Chat", new { id = ticketId, userId = userId });
        }

        /// <summary>
        /// POST: /TicketChat/CloseTicket
        /// Cambia el estado del ticket a "Cerrado".
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseTicket(int ticketId, int userId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
                return NotFound();

            // Asumimos que el ID de estado "Cerrado" es 3 (ajusta según tu BD)
            const int ESTADO_CERRADO = 3;
            ticket.EstadoID = ESTADO_CERRADO;
            ticket.FechaCierre = DateTime.Now;
            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();

            // ───────────────────────────────────────────────────────────────────────────
            // 5) Enviar correo al otro participante notificando cierre de ticket
            var usuarioEmisor = await _context.Usuarios.FindAsync(userId);

            int destinatarioId;
            if (userId == ticket.UsuarioCreadorID)
            {
                // El creador cerró -> notificamos al soporte asignado
                var ultimaAsign = await _context.Asignaciones
                    .Where(a => a.TicketID == ticketId && a.ResponsablePrincipal)
                    .OrderByDescending(a => a.FechaAsignacion)
                    .FirstOrDefaultAsync();

                destinatarioId = ultimaAsign != null
                    ? ultimaAsign.UsuarioAsignadoID
                    : -1;
            }
            else
            {
                // El soporte cerró -> notificamos al creador
                destinatarioId = ticket.UsuarioCreadorID;
            }

            if (destinatarioId > 0)
            {
                var destinatario = await _context.Usuarios.FindAsync(destinatarioId);
                if (destinatario != null && !string.IsNullOrWhiteSpace(destinatario.Email))
                {
                    string asunto = $"Ticket #{ticket.TicketID} cerrado";
                    string cuerpo = $@"
                        Hola {destinatario.Nombre},

                        {(userId == ticket.UsuarioCreadorID
                            ? $"El cliente ({usuarioEmisor.Nombre}) ha cerrado el ticket #{ticket.TicketID}."
                            : $"El técnico ({usuarioEmisor.Nombre}) ha cerrado el ticket #{ticket.TicketID}.")}

                        Fecha de cierre: {DateTime.Now:dd/MM/yyyy HH:mm}

                        Saludos,
                        Equipo de Soporte
                    ";

                    var servicioCorreo = new correo(_configuration);
                    servicioCorreo.enviar(destinatario.Email, asunto, cuerpo);
                }
            }
            // ───────────────────────────────────────────────────────────────────────────

            return RedirectToAction("Chat", new { id = ticketId, userId = userId });
        }
    }
}
