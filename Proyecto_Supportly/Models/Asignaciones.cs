using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Proyecto_Supportly.Models
{
    public class Asignaciones
    {
        [Key]
        [DisplayName("ID de Asignación")]
        public int AsignacionID { get; set; }

        [DisplayName("Ticket")]
        [Required(ErrorMessage = "El ticket NO es opcional")]
        public int TicketID { get; set; }

        [DisplayName("Usuario Asignado")]
        [Required(ErrorMessage = "El usuario asignado NO es opcional")]
        public int UsuarioAsignadoID { get; set; }

        [DisplayName("Fecha de Asignación")]
        [Required(ErrorMessage = "La fecha de asignación NO es opcional")]
        [DataType(DataType.DateTime)]
        public DateTime FechaAsignacion { get; set; }

        [DisplayName("Responsable Principal")]
        [Required(ErrorMessage = "Indicar si es responsable principal NO es opcional")]
        public bool ResponsablePrincipal { get; set; }
    }
}
