using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Proyecto_Supportly.Models
{
    public class Notificaciones
    {
        [Key]
        [DisplayName("ID de Notificación")]
        public int NotificacionID { get; set; }

        [DisplayName("Ticket")]
        [Required(ErrorMessage = "El ticket NO es opcional")]
        public int TicketID { get; set; }

        [DisplayName("Usuario Destinatario")]
        [Required(ErrorMessage = "El usuario destinatario NO es opcional")]
        public int UsuarioDestinatarioID { get; set; }

        [DisplayName("Tipo")]
        [Required(ErrorMessage = "El tipo NO es opcional")]
        [StringLength(50, ErrorMessage = "La cantidad máxima de caracteres permitida es {1}")]
        public string Tipo { get; set; }

        [DisplayName("Mensaje")]
        [Required(ErrorMessage = "El mensaje NO es opcional")]
        [StringLength(500, ErrorMessage = "La cantidad máxima de caracteres permitida es {1}")]
        public string Mensaje { get; set; }

        [DisplayName("Fecha de Envío")]
        [Required(ErrorMessage = "La fecha de envío NO es opcional")]
        [DataType(DataType.DateTime)]
        public DateTime FechaEnvio { get; set; }
    }
}
