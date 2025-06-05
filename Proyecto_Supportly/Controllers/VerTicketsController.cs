using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Supportly.Models;

namespace Proyecto_Supportly.Controllers
{
    public class VerTicketsController : Controller
    {
        private readonly SupportDBContext _context;

        public VerTicketsController(SupportDBContext context)
        {
            _context = context;
        }
        /// <summary>
        /// GET: /Ticket/MisTicketsAsignados?ticketId={ticketId}
        ///
        /// - Si el usuario es Administrador (según Roles.Nombre): muestra todos los tickets (asignados y no asignados).
        /// - Si el usuario es Empleado o Técnico: muestra solo aquellos tickets asignados a él.
        ///
        /// Además, si se recibe ticketId, carga ViewBag.SelectedTicket y ViewBag.Comentarios para mostrar detalles.
        /// </summary>
        public IActionResult MisTicketsAsignados(int? ticketId)
        {
            // 1) Verificar sesión y obtener UsuarioID
            var usuarioIdEnSesion = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioIdEnSesion == null)
            {
                // No está logueado → redirigir a Login
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            // 2) Obtener datos del usuario desde la BD
            var currentUser = _context.Usuarios
                                      .FirstOrDefault(u => u.UsuarioID == usuarioIdEnSesion.Value);
            if (currentUser == null)
            {
                // Si por alguna razón ya no existe en BD, limpiamos sesión y enviamos al login
                HttpContext.Session.Clear();
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            // 3) Obtener el rol del usuario desde la tabla Roles
            var rolUsuario = _context.Roles
                                     .FirstOrDefault(r => r.RolID == currentUser.RolID);
            if (rolUsuario == null)
            {
                // Si el rol no existe, limpiar sesión y forzar login nuevamente
                HttpContext.Session.Clear();
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            // 4) Determinar si es Administrador, Empleado o Técnico
            //    Se asume que Roles.Nombre contiene cadenas como "Administrador", "Empleado" o "Técnico".
            bool esAdministrador = string.Equals(rolUsuario.Nombre, "Administrador", StringComparison.OrdinalIgnoreCase);
            bool esEmpleadoOtecnico = string.Equals(rolUsuario.Nombre, "Soporte Técnico", StringComparison.OrdinalIgnoreCase)
                                      || string.Equals(rolUsuario.Nombre, "Soporte Técnico", StringComparison.OrdinalIgnoreCase);

            ViewBag.EsAdministrador = esAdministrador;

            // 5) Construir la lista de tickets según el rol
            List<dynamic> listaConJoin;
            if (esAdministrador)
            {
                // Administrador: ve todos los tickets (asignados y no asignados)
                listaConJoin = (from t in _context.Tickets
                                join u in _context.Usuarios
                                    on t.UsuarioCreadorID equals u.UsuarioID
                                select new
                                {
                                    t.TicketID,
                                    t.Titulo,
                                    t.Descripcion,
                                    t.FechaCreacion,
                                    t.Prioridad,
                                    t.EstadoID,
                                    t.CategoriaID,
                                    t.UsuarioCreadorID,
                                    NombreCreador = u.Nombre
                                })
                                .OrderByDescending(x => x.FechaCreacion)
                                .ToList<dynamic>();
            }
            else if (esEmpleadoOtecnico)
            {
                // Empleado o Técnico: solo ve los tickets asignados a él
                listaConJoin = (from t in _context.Tickets
                                join a in _context.Asignaciones
                                    on t.TicketID equals a.TicketID
                                where a.UsuarioAsignadoID == currentUser.UsuarioID
                                join u in _context.Usuarios
                                    on t.UsuarioCreadorID equals u.UsuarioID
                                select new
                                {
                                    t.TicketID,
                                    t.Titulo,
                                    t.Descripcion,
                                    t.FechaCreacion,
                                    t.Prioridad,
                                    t.EstadoID,
                                    t.CategoriaID,
                                    t.UsuarioCreadorID,
                                    NombreCreador = u.Nombre
                                })
                                .OrderByDescending(x => x.FechaCreacion)
                                .ToList<dynamic>();
            }
            else
            {
                // Cualquier otro rol no permitido, limpiar sesión y redirigir a login
                HttpContext.Session.Clear();
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            ViewBag.TicketsAsignados = listaConJoin;

            // 6) Si se recibió ticketId, buscar ese ticket en esta misma lista dinámica
            if (ticketId.HasValue)
            {
                var seleccionado = listaConJoin
                                   .FirstOrDefault(x => x.TicketID == ticketId.Value);
                ViewBag.SelectedTicket = seleccionado;
            }
            else
            {
                ViewBag.SelectedTicket = null;
            }

            // 7) Cargar comentarios del ticket seleccionado (si aplica)
            if (ticketId.HasValue)
            {
                var comentariosBD = (from c in _context.Comentarios
                                     where c.TicketID == ticketId.Value
                                     select new
                                     {
                                         c.Contenido,
                                         Usuario = (_context.Usuarios
                                                    .Where(u => u.UsuarioID == c.UsuarioID)
                                                    .Select(u => u.Nombre)
                                                    .FirstOrDefault()),
                                         c.FechaComentario
                                     })
                                     .OrderBy(c => c.FechaComentario)
                                     .ToList<dynamic>();

                ViewBag.Comentarios = comentariosBD;
            }
            else
            {
                ViewBag.Comentarios = null;
            }

            // 8) Nombre del usuario actual para la vista
            ViewBag.NombreUsuario = currentUser.Nombre;

            // 9) ID del usuario actual para la vista
            ViewBag.SessionUserId = currentUser.UsuarioID;

            return View();
        }

        public IActionResult HistorialTickets()
        {
            // 1) Verificar sesión y obtener UsuarioID
            var usuarioIdEnSesion = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioIdEnSesion == null)
            {
                // No está logueado → redirigir a Login
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            // 2) Obtener datos del usuario desde la BD
            var currentUser = _context.Usuarios.FirstOrDefault(u => u.UsuarioID == usuarioIdEnSesion.Value);
            if (currentUser == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            // 3) Obtener el rol del usuario desde la tabla Roles
            var rolUsuario = _context.Roles.FirstOrDefault(r => r.RolID == currentUser.RolID);
            if (rolUsuario == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            bool esAdministrador = string.Equals(rolUsuario.Nombre, "Administrador", StringComparison.OrdinalIgnoreCase);
            bool esEmpleadoOtecnico = string.Equals(rolUsuario.Nombre, "Soporte Técnico", StringComparison.OrdinalIgnoreCase)
                                      || string.Equals(rolUsuario.Nombre, "Soporte Técnico", StringComparison.OrdinalIgnoreCase);

            // 4) Construir la lista de tickets cerrados según el rol:
            //    condicion: FechaCierre != null (o podrías filtrar por EstadoID == 3 si "3" es Cerrado)
            List<dynamic> listaCerrados;
            if (esAdministrador)
            {
                // Todos los tickets que tengan FechaCierre asignada
                listaCerrados = (from t in _context.Tickets
                                 where t.FechaCierre.HasValue
                                 join u in _context.Usuarios
                                     on t.UsuarioCreadorID equals u.UsuarioID
                                 select new
                                 {
                                     t.TicketID,
                                     t.Titulo,
                                     t.Prioridad,
                                     t.FechaCreacion,
                                     t.FechaCierre,
                                     NombreCreador = u.Nombre
                                 })
                                 .OrderByDescending(x => x.FechaCierre)
                                 .ToList<dynamic>();
            }
            else if (esEmpleadoOtecnico)
            {
                // Solo los que este técnico/empleado haya cerrado o que le hayan asignado y cerrado
                listaCerrados = (from t in _context.Tickets
                                 where t.FechaCierre.HasValue
                                 join a in _context.Asignaciones
                                     on t.TicketID equals a.TicketID
                                 where a.UsuarioAsignadoID == currentUser.UsuarioID
                                 join u in _context.Usuarios
                                     on t.UsuarioCreadorID equals u.UsuarioID
                                 select new
                                 {
                                     t.TicketID,
                                     t.Titulo,
                                     t.Prioridad,
                                     t.FechaCreacion,
                                     t.FechaCierre,
                                     NombreCreador = u.Nombre
                                 })
                                 .OrderByDescending(x => x.FechaCierre)
                                 .ToList<dynamic>();
            }
            else
            {
                // Cualquier otro rol no permitido → redirigir a Login
                HttpContext.Session.Clear();
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            // 5) Pasar datos al ViewBag
            ViewBag.TicketsCerrados = listaCerrados;
            ViewBag.EsAdministrador = esAdministrador;
            ViewBag.NombreUsuario = currentUser.Nombre;

            return View();
        }

    }
}