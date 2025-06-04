using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Supportly.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto_Supportly.Controllers
{
    public class TicketChatController : Controller
    {
        private readonly SupportDBContext _context;

        public TicketChatController(SupportDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: /TicketChat/Chat/5?userId=42
        /// Muestra los comentarios y adjuntos de un ticket específico.
        /// </summary>
        public async Task<IActionResult> Chat(int id, int userId)
        {
            var sessionUserId = HttpContext.Session.GetInt32("UsuarioId");
            if (sessionUserId == null)
            {
                // Redirige al login u otro manejo
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

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
            var estado = await _context.Estados.FindAsync(ticket.EstadoID);
            ViewBag.TicketStatus = estado?.Nombre ?? "Desconocido";

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
            return View();
        }

        /// <summary>
        /// POST: /TicketChat/SendMessage
        /// Agrega un nuevo comentario al ticket.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int ticketId, int userId, string messageContent)
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

            return RedirectToAction("Chat", new { id = ticketId, userId = userId });
        }
    }
}
