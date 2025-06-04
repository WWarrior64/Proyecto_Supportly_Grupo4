using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Supportly.Models;

namespace Proyecto_Supportly.Controllers
{
    public class NotificacionController : Controller
    {
        private readonly SupportDBContext _context; // Declara un campo para el contexto de la DB

        // Constructor: Inyecta el contexto de la base de datos
        public NotificacionController(SupportDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: /Ticket/MisNotificaciones
        /// Muestra todas las notificaciones pendientes del usuario en sesión.
        /// </summary>
        public async Task<IActionResult> MisNotificaciones()
        {
            // 1. Verificar sesión de usuario
            var sessionUserId = HttpContext.Session.GetInt32("UsuarioId");
            if (sessionUserId == null)
                return RedirectToAction("Autenticar", "UsuarioLogin");

            // 2. Obtener nombre del usuario para mostrar en la cabecera
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UsuarioID == sessionUserId.Value);
            ViewBag.NombreUsuario = usuario?.Nombre ?? "(Usuario)";

            // 3. Recuperar todas las notificaciones para este usuario
            var notificaciones = await _context.Notificaciones
                .Where(n => n.UsuarioDestinatarioID == sessionUserId.Value)
                .OrderByDescending(n => n.FechaEnvio)
                .ToListAsync();

            ViewBag.Notificaciones = notificaciones;
            return View();
        }

        /// <summary>
        /// POST: /Ticket/MarcarTodasComoLeidas
        /// Marca como leídas (elimina) todas las notificaciones del usuario.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarTodasComoLeidas(int usuarioId)
        {
            // 1. Verificar que el usuarioId coincida con el que está en sesión
            var sessionUserId = HttpContext.Session.GetInt32("UsuarioId");
            if (sessionUserId == null || sessionUserId.Value != usuarioId)
                return Forbid();

            // 2. Eliminar todas las notificaciones de este usuario
            var aBorrar = _context.Notificaciones
                .Where(n => n.UsuarioDestinatarioID == usuarioId);
            _context.Notificaciones.RemoveRange(aBorrar);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Todas las notificaciones se han marcado como leídas.";
            return RedirectToAction(nameof(MisNotificaciones));
        }
    }
}