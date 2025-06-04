using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Supportly.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Proyecto_Supportly.Controllers
{
    public class InterfazTicketController : Controller
    {
        private readonly SupportDBContext _context;

        public InterfazTicketController(SupportDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET: /InterfazTicket/Index
        /// Muestra el dashboard con los últimos 20 tickets del usuario.
        /// Si el usuario es Interno, también carga tickets “asignados”.
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // 1. Verificar sesión de usuario
            var userId = HttpContext.Session.GetInt32("UsuarioId");
            if (userId == null)
                return RedirectToAction("Autenticar", "UsuarioLogin");

            // 2. Obtener datos del usuario
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UsuarioID == userId.Value);

            if (usuario == null)
                return RedirectToAction("Autenticar", "UsuarioLogin");

            // 3. Nombre y empresa (si aplica)
            ViewBag.UserName = usuario.Nombre;
            ViewBag.CompanyName = usuario.Empresa ?? "";

            // 4. Últimos 20 tickets creados por este usuario
            var userTickets = (from t in _context.Tickets.AsNoTracking()
                               join e in _context.Estados.AsNoTracking() on t.EstadoID equals e.EstadoID into estadoJoin
                               from e in estadoJoin.DefaultIfEmpty()
                               where t.UsuarioCreadorID == userId
                               orderby t.FechaCreacion descending
                               select new
                               {
                                    TicketID = t.TicketID,
                                    Titulo = t.Titulo,
                                    FechaUltimaInteraccion = t.FechaCreacion,
                                    EstadoTicket = t.EstadoID != null ? e.Nombre : "Sin estado"
                                }).ToList<dynamic>();

            ViewBag.UserTickets = userTickets;


            // 5. ¿Es empleado (Interno) o cliente (Externo)?
            bool esEmpleado = usuario.TipoUsuario == "Interno";
            ViewBag.IsEmployee = esEmpleado;

            if (esEmpleado)
            {
                // 6. Para empleados, cargamos “tickets asignados”.
                // Aquí hemos asumido que “asignados” son aquellos que NO haya creado el empleado.
                // Ajusta la condición según tu lógica real de asignación.
                var assignedTickets = await (
                    from t in _context.Tickets.AsNoTracking()
                    join e in _context.Estados.AsNoTracking() on t.EstadoID equals e.EstadoID
                    where t.UsuarioCreadorID != userId.Value
                    orderby t.FechaCreacion descending
                    select new
                    {
                        TicketID = t.TicketID,
                        Titulo = t.Titulo,
                        FechaUltimaInteraccion = t.FechaCreacion,
                        EstadoTicket = e.Nombre
                    }
                )
                .Take(20)
                .ToListAsync();

                ViewBag.AssignedTickets = assignedTickets;
            }
            else
            {
                ViewBag.AssignedTickets = new List<dynamic>();
            }

            return View();
        }
    }
}
