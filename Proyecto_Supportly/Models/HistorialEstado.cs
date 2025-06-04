using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Proyecto_Supportly.Models
{
    public class HistorialEstado
    {
        [Key]
        [DisplayName("ID de Historial")]
        public int HistorialID { get; set; }

        [DisplayName("Ticket")]
        [Required(ErrorMessage = "El ticket NO es opcional")]
        public int TicketID { get; set; }

        [DisplayName("Estado")]
        [Required(ErrorMessage = "El estado NO es opcional")]
        public int EstadoID { get; set; }

        [DisplayName("Fecha de Asignación")]
        [Required(ErrorMessage = "La fecha NO es opcional")]
        [DataType(DataType.DateTime)]
        public DateTime FechaAsignacion { get; set; }
    }
}
