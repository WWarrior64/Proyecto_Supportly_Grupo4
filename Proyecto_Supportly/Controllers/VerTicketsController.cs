using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Supportly.Models;
using Proyecto_Supportly.Services;

namespace Proyecto_Supportly.Controllers
{
    public class VerTicketsController : Controller
    {
        private readonly SupportDBContext _context;
        private IConfiguration _configuration;

        public VerTicketsController(SupportDBContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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

            bool esAdministrador = string.Equals(rolUsuario.Nombre, "Administrador", StringComparison.OrdinalIgnoreCase);
            bool esSoporte = string.Equals(rolUsuario.Nombre, "Soporte Técnico", StringComparison.OrdinalIgnoreCase);

            ViewBag.EsAdministrador = esAdministrador;
            ViewBag.NombreUsuario = currentUser.Nombre;
            ViewBag.SessionUserId = currentUser.UsuarioID;
            var listaEstados = _context.Estados
                           .OrderBy(e => e.Nombre)
                           .Select(e => new { e.EstadoID, e.Nombre })
                           .ToList();
            ViewBag.EstadosList = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(listaEstados, "EstadoID", "Nombre");

            // 4) Construir la lista de tickets según el rol
            //    Filtramos solo los tickets que NO estén cerrados (EstadoID != 3)
            const int ESTADO_CERRADO = 4;

            List<dynamic> listaConJoin;
            if (esAdministrador)
            {
                // Administrador: ve todos los tickets que NO están cerrados
                listaConJoin = (from t in _context.Tickets
                                where t.EstadoID != ESTADO_CERRADO
                                join u in _context.Usuarios
                                    on t.UsuarioCreadorID equals u.UsuarioID
                                join e in _context.Estados on t.EstadoID equals e.EstadoID
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
                                    NombreCreador = u.Nombre,
                                    EstadoNombre = e.Nombre,
                                    FechaCierre = t.FechaCierre
                                })
                                .OrderByDescending(x => x.FechaCreacion)
                                .ToList<dynamic>();
            }
            else if (esSoporte)
            {
                // Soporte Técnico: solo ve los tickets asignados a él y NO cerrados
                listaConJoin = (from t in _context.Tickets
                                where t.EstadoID != ESTADO_CERRADO
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
                                    EstadoNombre = _context.Estados
                                                              .Where(e => e.EstadoID == t.EstadoID)
                                                              .Select(e => e.Nombre)
                                                              .FirstOrDefault(),
                                    t.CategoriaID,
                                    t.UsuarioCreadorID,
                                    NombreCreador = u.Nombre,
                                    FechaCierre = t.FechaCierre
                                })
                                .OrderByDescending(x => x.FechaCreacion)
                                .ToList<dynamic>();
            }
            else
            {
                // Cualquier otro rol no permitido
                HttpContext.Session.Clear();
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            ViewBag.TicketsAsignados = listaConJoin;

            // 5) Si se recibió ticketId, buscar ese ticket en la lista
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

            // 6) Cargar comentarios del ticket seleccionado (si aplica)
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
                ViewBag.SelectedTicket = null;
                ViewBag.Comentarios = new List<dynamic>();
            }

            ViewBag.SessionUserId = currentUser.UsuarioID;
            return View();
        }

        /// <summary>
        /// POST: /VerTickets/UpdateStatus
        /// Recibe el ticketId y el nuevo estado (newEstadoID).
        /// Solo puede hacerlo Administrador o Soporte Técnico.
        /// Luego redirige de vuelta a MisTicketsAsignados, manteniendo el ticket seleccionado.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int ticketId, int newEstadoID)
        {
            // 1) Verificar sesión
            var usuarioIdEnSesion = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioIdEnSesion == null)
                return RedirectToAction("Autenticar", "UsuarioLogin");

            // 2) Obtener currentUser y su rol
            var currentUser = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioID == usuarioIdEnSesion.Value);
            if (currentUser == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            var rolUsuario = await _context.Roles.FirstOrDefaultAsync(r => r.RolID == currentUser.RolID);
            bool esAdministrador = rolUsuario != null &&
                                   string.Equals(rolUsuario.Nombre, "Administrador", StringComparison.OrdinalIgnoreCase);
            bool esSoporteTecnico = rolUsuario != null &&
                                    string.Equals(rolUsuario.Nombre, "Soporte Técnico", StringComparison.OrdinalIgnoreCase);

            if (!esAdministrador && !esSoporteTecnico)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            // 3) Buscar el ticket
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                TempData["ErrorMessage"] = "Ticket no encontrado.";
                return RedirectToAction("MisTicketsAsignados");
            }

            // 4) Actualizar el estado y FechaCierre (si corresponde)
            ticket.EstadoID = newEstadoID;
            ticket.FechaCierre = DateTime.Now;
            _context.Tickets.Update(ticket);
            await _context.SaveChangesAsync();
            // ───────────────────────────────────────────────────────────────────────────
            // 5) Luego de guardar, notificar vía correo

            // 5.1) Obtener datos del creador (cliente)
            var creador = await _context.Usuarios
                .Where(u => u.UsuarioID == ticket.UsuarioCreadorID)
                .Select(u => new { u.Nombre, u.Email })
                .FirstOrDefaultAsync();

            // 5.2) Obtener el nombre del nuevo estado (para el cuerpo del correo)
            var estadoNuevoNombre = await _context.Estados
                .Where(e => e.EstadoID == newEstadoID)
                .Select(e => e.Nombre)
                .FirstOrDefaultAsync();

            // 5.3) Obtener el empleado asignado actualmente (ResponsablePrincipal más reciente)
            var asignPrincipal = await _context.Asignaciones
                .Where(a => a.TicketID == ticketId && a.ResponsablePrincipal)
                .OrderByDescending(a => a.FechaAsignacion)
                .FirstOrDefaultAsync();

            var empleadoAsignado = asignPrincipal != null
                ? await _context.Usuarios
                    .Where(u => u.UsuarioID == asignPrincipal.UsuarioAsignadoID)
                    .Select(u => new { u.Nombre, u.Email })
                    .FirstOrDefaultAsync()
                : null;

            // 5.4) Instanciar el servicio de correo
            correo servicioCorreo = new correo(_configuration);

            // 5.5) Enviar correo al creador del ticket (cliente)
            if (creador != null && !string.IsNullOrWhiteSpace(creador.Email))
            {
                string asuntoCliente = $"Ticket #{ticket.TicketID}: estado actualizado";
                string cuerpoCliente = $@"
                    Hola {creador.Nombre},
                    
                    El estado de tu ticket con ID: {ticket.TicketID} ha cambiado.
                    Nuevo estado: {estadoNuevoNombre}
                    Fecha de cambio: {DateTime.Now:dd/MM/yyyy HH:mm}

                    Título: {ticket.Titulo}
                    Descripción: {ticket.Descripcion}

                    Ingresa al sistema para más detalles.
                    
                    Saludos,
                    Equipo de Soporte
                ";
                servicioCorreo.enviar(creador.Email, asuntoCliente, cuerpoCliente);
            }

            // 5.6) (Opcional) Enviar correo al empleado asignado
            if (empleadoAsignado != null && !string.IsNullOrWhiteSpace(empleadoAsignado.Email))
            {
                string asuntoEmpleado = $"Ticket #{ticket.TicketID}: estado cambiado a \"{estadoNuevoNombre}\"";
                string cuerpoEmpleado = $@"
                    Hola {empleadoAsignado.Nombre},
                    
                    El ticket que tienes asignado (ID: {ticket.TicketID}) cambió de estado.
                    Nuevo estado: {estadoNuevoNombre}
                    Fecha de cambio: {DateTime.Now:dd/MM/yyyy HH:mm}

                    Título: {ticket.Titulo}
                    Descripción: {ticket.Descripcion}

                    Ingresa al sistema para verificar si requieres tomar alguna acción adicional.
                    
                    Saludos,
                    Equipo de Soporte
                ";
                servicioCorreo.enviar(empleadoAsignado.Email, asuntoEmpleado, cuerpoEmpleado);
            }
            // ───────────────────────────────────────────────────────────────────────────

            TempData["SuccessMessage"] = "Estado actualizado y notificaciones enviadas.";
            return RedirectToAction("MisTicketsAsignados", new { ticketId = ticketId });
        }

        /// <summary>
        /// POST: /VerTickets/AddComment
        /// Solo el Administrador puede agregar un comentario desde esta misma página.
        /// Luego redirige de vuelta a MisTicketsAsignados, manteniendo el ticket seleccionado.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddComment(int ticketId, string contenido)
        {
            var usuarioIdEnSesion = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioIdEnSesion == null)
                return RedirectToAction("Autenticar", "UsuarioLogin");

            var currentUser = _context.Usuarios.FirstOrDefault(u => u.UsuarioID == usuarioIdEnSesion.Value);
            if (currentUser == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            var rolUsuario = _context.Roles.FirstOrDefault(r => r.RolID == currentUser.RolID);
            bool esAdministrador = rolUsuario != null && string.Equals(rolUsuario.Nombre, "Administrador", StringComparison.OrdinalIgnoreCase);
            if (!esAdministrador)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            if (!string.IsNullOrWhiteSpace(contenido))
            {
                var nuevoComentario = new Comentarios
                {
                    TicketID = ticketId,
                    UsuarioID = currentUser.UsuarioID,
                    Contenido = contenido.Trim(),
                    FechaComentario = DateTime.Now
                };
                _context.Comentarios.Add(nuevoComentario);
                _context.SaveChanges();

                // 2) Obtener los datos del ticket y del creador (cliente)
                var ticket = _context.Tickets.FirstOrDefault(t => t.TicketID == ticketId);
                if (ticket != null)
                {
                    var creador = _context.Usuarios.FirstOrDefault(u => u.UsuarioID == ticket.UsuarioCreadorID);
                    if (creador != null && !string.IsNullOrWhiteSpace(creador.Email))
                    {
                        // 3) Armar asunto y cuerpo del correo
                        string asunto = $"Se agregó un comentario en tu ticket #{ticket.TicketID}";
                        string cuerpo = $@"
                            Hola {creador.Nombre},

                            Se ha agregado un nuevo comentario en tu ticket con ID {ticket.TicketID}.

                            Contenido del comentario:
                            --------------------------------------------
                            {contenido.Trim()}
                            --------------------------------------------

                            Fecha y hora: {DateTime.Now:dd/MM/yyyy HH:mm}
                            Usuario que comentó: {currentUser.Nombre}

                            Ingresa al sistema para ver más detalles o responder.
                            
                            Saludos,
                            Equipo de Soporte
                        ";

                        // 4) Enviar el correo usando tu servicio 'correo'
                        var servicioCorreo = new correo(_configuration);
                        servicioCorreo.enviar(creador.Email, asunto, cuerpo);
                    }
                }
            }

            // Redirigimos a la misma vista, indicando ticketId seleccionado
            return RedirectToAction("MisTicketsAsignados", new { ticketId = ticketId });
        }

        /// <summary>
        /// GET: /VerTickets/HistorialTickets
        /// - Si es Administrador: muestra todos los tickets cuyo EstadoID == 3 (Cerrado).
        /// - Si es Soporte Técnico: muestra solo los tickets cerrados asignados a él.
        /// </summary>
        public IActionResult HistorialTickets(int? ticketId)
        {
            // 1) Verificar sesión y obtener UsuarioID
            var usuarioIdEnSesion = HttpContext.Session.GetInt32("UsuarioId");
            if (usuarioIdEnSesion == null)
                return RedirectToAction("Autenticar", "UsuarioLogin");

            // 2) Obtener datos del usuario
            var currentUser = _context.Usuarios
                                      .FirstOrDefault(u => u.UsuarioID == usuarioIdEnSesion.Value);
            if (currentUser == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            // 3) Obtener el rol del usuario
            var rolUsuario = _context.Roles
                                     .FirstOrDefault(r => r.RolID == currentUser.RolID);
            if (rolUsuario == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            bool esAdministrador = string.Equals(rolUsuario.Nombre, "Administrador", StringComparison.OrdinalIgnoreCase);
            bool esSoporte = string.Equals(rolUsuario.Nombre, "Soporte Técnico", StringComparison.OrdinalIgnoreCase);

            ViewBag.NombreUsuario = currentUser.Nombre;
            const int ESTADO_CERRADO_ID = 4;

            // 4) Construir la lista de tickets cerrados según el rol, incluyendo todos los campos
            //    que la vista necesita: TicketID, Titulo, Descripcion, Prioridad, FechaCreacion, FechaCierre,
            //    EstadoNombre, NombreCreador y lista de enlaces adjuntos.
            List<dynamic> listaCerrados;

            if (esAdministrador)
            {
                listaCerrados = _context.Tickets
                    .Where(t => t.EstadoID == ESTADO_CERRADO_ID)
                    .Select(t => new
                    {
                        t.TicketID,
                        t.Titulo,
                        t.Descripcion,
                        t.Prioridad,
                        t.FechaCreacion,
                        FechaCierre = t.FechaCierre ?? DateTime.MinValue,
                        EstadoNombre = _context.Estados
                                                 .Where(e => e.EstadoID == t.EstadoID)
                                                 .Select(e => e.Nombre)
                                                 .FirstOrDefault(),
                        NombreCreador = _context.Usuarios
                                                .Where(u => u.UsuarioID == t.UsuarioCreadorID)
                                                .Select(u => u.Nombre)
                                                .FirstOrDefault(),
                        Archivos = _context.Adjuntos
                                           .Where(a => a.TicketID == t.TicketID)
                                           .Select(a => a.EnlaceDrive)
                                           .ToList()
                    })
                    .OrderByDescending(x => x.FechaCierre)
                    .ToList<dynamic>();
            }
            else if (esSoporte)
            {
                listaCerrados = (from t in _context.Tickets
                                 where t.EstadoID == ESTADO_CERRADO_ID
                                 join a in _context.Asignaciones on t.TicketID equals a.TicketID
                                 where a.UsuarioAsignadoID == currentUser.UsuarioID
                                 select new
                                 {
                                     t.TicketID,
                                     t.Titulo,
                                     t.Descripcion,
                                     t.Prioridad,
                                     t.FechaCreacion,
                                     FechaCierre = t.FechaCierre,    // dejamos el nullable tal cual
                                     EstadoNombre = _context.Estados
                                                                .Where(e => e.EstadoID == t.EstadoID)
                                                                .Select(e => e.Nombre)
                                                                .FirstOrDefault(),
                                     NombreCreador = _context.Usuarios
                                                             .Where(u => u.UsuarioID == t.UsuarioCreadorID)
                                                             .Select(u => u.Nombre)
                                                             .FirstOrDefault(),
                                     Archivos = _context.Adjuntos
                                                             .Where(a2 => a2.TicketID == t.TicketID)
                                                             .Select(a2 => a2.EnlaceDrive)
                                                             .ToList()
                                 })
                                 // Ordenamos descendente sobre el nullable: los registros con FechaCierre == null quedarán al final
                                 .OrderByDescending(x => x.FechaCierre)
                                 .ToList<dynamic>();
            }
            else
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Autenticar", "UsuarioLogin");
            }

            ViewBag.HistorialTickets = listaCerrados;

            // 5) Cargar el ticket seleccionado (para luego mostrar el detalle a la derecha)
            if (ticketId.HasValue)
            {
                // Buscamos dentro de la misma lista dinámica
                var seleccionado = listaCerrados
                                   .FirstOrDefault(x => x.TicketID == ticketId.Value);
                ViewBag.SelectedTicket = seleccionado;

                // 6) Obtener comentarios de ese ticket
                var comentariosBD = _context.Comentarios
                    .Where(c => c.TicketID == ticketId.Value)
                    .Select(c => new
                    {
                        c.Contenido,
                        Usuario = _context.Usuarios
                                          .Where(u => u.UsuarioID == c.UsuarioID)
                                          .Select(u => u.Nombre)
                                          .FirstOrDefault(),
                        c.FechaComentario
                    })
                    .OrderBy(c => c.FechaComentario)
                    .ToList<dynamic>();

                ViewBag.Comentarios = comentariosBD;
            }
            else
            {
                ViewBag.SelectedTicket = null;
                ViewBag.Comentarios = new List<dynamic>();
            }

            return View();
        }
    }
}