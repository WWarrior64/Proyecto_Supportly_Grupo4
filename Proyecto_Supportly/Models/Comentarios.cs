using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Proyecto_Supportly.Models
{
    public class Comentarios
    {
        [Key]
        [DisplayName("ID de Comentario")]
        public int ComentarioID { get; set; }

        [DisplayName("Ticket")]
        [Required(ErrorMessage = "El ticket NO es opcional")]
        public int TicketID { get; set; }

        [DisplayName("Usuario")]
        [Required(ErrorMessage = "El usuario NO es opcional")]
        public int UsuarioID { get; set; }

        [DisplayName("Contenido")]
        [Required(ErrorMessage = "El contenido NO es opcional")]
        public string Contenido { get; set; }

        [DisplayName("Fecha de Comentario")]
        [Required(ErrorMessage = "La fecha de comentario NO es opcional")]
        [DataType(DataType.DateTime)]
        public DateTime FechaComentario { get; set; }
    }
}
