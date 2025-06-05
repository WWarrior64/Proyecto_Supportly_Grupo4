using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Proyecto_Supportly.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Proyecto_Supportly.Controllers
{
    public class DashboardController : Controller
    {
        private readonly SupportDBContext _context;

        public DashboardController(SupportDBContext context)
        {
            _context = context;
        }

        // GET: Dashboard
        public async Task<IActionResult> Index(
            string filterType,
            int? employeeId,
            int? month,
            int? year,
            DateTime? startDate,
            DateTime? endDate)
        {
            // 1. Determinar el filtro activo
            ViewBag.SelectedFilterType = string.IsNullOrEmpty(filterType) ? "Tendencias" : filterType;

            // 2. Poblar dropdowns (empleados y meses)
            ViewBag.AvailableEmployees = new SelectList(
                await _context.Usuarios
                    .OrderBy(u => u.Nombre)
                    .Select(u => new { u.UsuarioID, Texto = u.Nombre + " (" + u.Email + ")" })
                    .ToListAsync(),
                "UsuarioID", "Texto");

            ViewBag.AvailableMonths = Enumerable.Range(1, 12)
                .Select(m => new SelectListItem
                {
                    Value = m.ToString(),
                    Text = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)
                })
                .ToList();

            ViewBag.SelectedYear = year ?? DateTime.Now.Year;
            ViewBag.CustomStartDate = startDate;
            ViewBag.CustomEndDate = endDate;

            // 3. Recuperar lista de tickets según el filtro, ya en memoria (List<Tickets>)
            List<Tickets> ticketsList;

            switch (ViewBag.SelectedFilterType)
            {
                case "Empleados":
                    // Si hay employeeId, obtenemos los IDs de tickets asignados a ese usuario
                    if (employeeId.HasValue)
                    {
                        var ticketIds = await _context.Asignaciones
                            .Where(a => a.UsuarioAsignadoID == employeeId.Value)
                            .Select(a => a.TicketID)
                            .ToListAsync();

                        ticketsList = await _context.Tickets
                            .Where(t => ticketIds.Contains(t.TicketID))
                            .ToListAsync();
                    }
                    else
                    {
                        ticketsList = await _context.Tickets.ToListAsync();
                    }
                    break;

                case "Mes":
                    if (month.HasValue && year.HasValue)
                    {
                        var rangoInicio = new DateTime(year.Value, month.Value, 1);
                        var rangoFin = rangoInicio.AddMonths(1).AddDays(-1);

                        ticketsList = await _context.Tickets
                            .Where(t => t.FechaCreacion.Date >= rangoInicio.Date
                                     && t.FechaCreacion.Date <= rangoFin.Date)
                            .ToListAsync();
                    }
                    else
                    {
                        var inicioAno = new DateTime(DateTime.Now.Year, 1, 1);
                        var finHoy = DateTime.Now;

                        ticketsList = await _context.Tickets
                            .Where(t => t.FechaCreacion.Date >= inicioAno.Date
                                     && t.FechaCreacion.Date <= finHoy.Date)
                            .ToListAsync();
                    }
                    break;

                case "Personalizado":
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        var inicioFiltro = startDate.Value.Date;
                        var finFiltro = endDate.Value.Date;
                        ticketsList = await _context.Tickets
                            .Where(t => t.FechaCreacion.Date >= inicioFiltro
                                     && t.FechaCreacion.Date <= finFiltro)
                            .ToListAsync();
                    }
                    else
                    {
                        ticketsList = await _context.Tickets.ToListAsync();
                    }
                    break;

                default: // “Tendencias”
                    {
                        var inicioAnoActual = new DateTime(DateTime.Now.Year, 1, 1);
                        var finHoyActual = DateTime.Now;
                        ticketsList = await _context.Tickets
                            .Where(t => t.FechaCreacion.Date >= inicioAnoActual.Date
                                     && t.FechaCreacion.Date <= finHoyActual.Date)
                            .ToListAsync();
                    }
                    break;
            }

            // 4. Calcular métricas usando LINQ a objetos sobre ticketsList

            ViewBag.TicketsGeneradosEsteAno = ticketsList.Count;

            var ticketsConCierre = ticketsList
                .Where(t => t.FechaCierre.HasValue)
                .ToList();
            ViewBag.TicketsAtendidosEsteAno = ticketsConCierre.Count;

            const int ESTADO_ABIERTO = 1;
            const int ESTADO_EN_PROCESO = 2;
            const int ESTADO_RESUELTO = 3;
            const int ESTADO_CERRADO = 4;
            const int ESTADO_PENDIENTE = 5;

            ViewBag.TicketsAbiertos = ticketsList.Count(t => t.EstadoID == ESTADO_ABIERTO);
            ViewBag.TicketsEnProceso = ticketsList.Count(t => t.EstadoID == ESTADO_EN_PROCESO);
            ViewBag.TicketsResueltos = ticketsList.Count(t => t.EstadoID == ESTADO_RESUELTO);
            ViewBag.TicketsCerrados = ticketsList.Count(t => t.EstadoID == ESTADO_CERRADO);
            ViewBag.TicketsPendientes = ticketsList.Count(t => t.EstadoID == ESTADO_PENDIENTE);

            if (ticketsConCierre.Any())
            {
                // Promedio de días de resolución en memoria
                var totalDias = ticketsConCierre
                    .Select(t => (t.FechaCierre.Value - t.FechaCreacion).TotalDays);
                var promedioDias = totalDias.Average();
                ViewBag.TiempoPromedioResolucion = $"{promedioDias:F1} días";
            }
            else
            {
                ViewBag.TiempoPromedioResolucion = "N/A";
            }

            // 4.5 Categoría de problema más común (en memoria)
            var grupoCategorias = ticketsList
                .GroupBy(t => t.CategoriaID)
                .Select(g => new { CategoriaID = g.Key, Cantidad = g.Count() })
                .OrderByDescending(x => x.Cantidad)
                .FirstOrDefault();

            string nombreCategoriaMasComun = "Ninguna";
            if (grupoCategorias != null && grupoCategorias.CategoriaID != 0)
            {
                var cat = await _context.Categorias.FindAsync(grupoCategorias.CategoriaID);
                nombreCategoriaMasComun = cat != null ? cat.Nombre : "Desconocida";
            }
            ViewBag.MostCommonCategory = nombreCategoriaMasComun;

            // 4.6 Mes más y menos productivo
            var ticketsPorMesGrupo = ticketsList
                .GroupBy(t => t.FechaCreacion.Month)
                .Select(g => new { Mes = g.Key, Cantidad = g.Count() })
                .ToList();

            if (ticketsPorMesGrupo.Any())
            {
                var mesMax = ticketsPorMesGrupo.OrderByDescending(x => x.Cantidad).First();
                var mesMin = ticketsPorMesGrupo.OrderBy(x => x.Cantidad).First();
                ViewBag.MesMasProductivo = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(mesMax.Mes);
                ViewBag.MesMenosProductivo = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(mesMin.Mes);
            }
            else
            {
                ViewBag.MesMasProductivo = "N/A";
                ViewBag.MesMenosProductivo = "N/A";
            }

            ViewBag.ClientApprovalPercentage = "N/A"; // No existe calificación en tu modelo

            // 5. Datos para gráficos

            var countAbiertos = ticketsList.Count(t => t.EstadoID == ESTADO_ABIERTO);
            var countEnProceso = ticketsList.Count(t => t.EstadoID == ESTADO_EN_PROCESO);
            var countCerrados = ticketsList.Count(t => t.EstadoID == ESTADO_CERRADO);
            var countResueltos = ticketsList.Count(t => t.EstadoID == ESTADO_RESUELTO);
            var countPendientes = ticketsList.Count(t => t.EstadoID == ESTADO_PENDIENTE);

            var pieData = new[]
            {
                new { label = "Abiertos",    value = countAbiertos,  color = "#FF6384" },
                new { label = "En Proceso",  value = countEnProceso, color = "#36A2EB" },
                new { label = "Resueltos",   value = countResueltos, color = "#90EE90" },
                new { label = "Cerrados",    value = countCerrados,  color = "#FFCE56" },
                new { label = "Pendientes",  value = countPendientes,color = "#FFA500" },
            };
            ViewBag.PieChartDataJson = JsonConvert.SerializeObject(pieData);

            var meses = Enumerable.Range(1, 12).ToList();
            var lineLabels = meses
                .Select(m => CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(m))
                .ToArray();

            var lineCounts = meses
                .Select(m => ticketsList.Count(t => t.FechaCreacion.Month == m))
                .ToArray();

            var lineDataset = new
            {
                label = "Tickets por Mes",
                data = lineCounts,
                borderColor = "#6A1B9A",
                backgroundColor = "rgba(106,27,154,0.2)"
            };
            var lineChartObj = new
            {
                labels = lineLabels,
                datasets = new[] { lineDataset }
            };
            ViewBag.LineChartDataJson = JsonConvert.SerializeObject(lineChartObj);

            return View();
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
