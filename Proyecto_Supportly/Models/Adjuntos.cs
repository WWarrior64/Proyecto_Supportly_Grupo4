using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Proyecto_Supportly.Models
{
    public class Adjuntos
    {
        [Key]
        [DisplayName("ID de Adjunto")]
        public int AdjuntoID { get; set; }

        [DisplayName("Ticket")]
        [Required(ErrorMessage = "El ticket NO es opcional")]
        public int TicketID { get; set; }

        [DisplayName("Enlace Drive")]
        [Required(ErrorMessage = "El enlace NO es opcional")]
        [StringLength(500, ErrorMessage = "La cantidad máxima de caracteres permitida es {1}")]
        public string EnlaceDrive { get; set; }

        [DisplayName("Nombre de Archivo")]
        [Required(ErrorMessage = "El nombre de archivo NO es opcional")]
        [StringLength(200, ErrorMessage = "La cantidad máxima de caracteres permitida es {1}")]
        public string NombreArchivo { get; set; }

        [DisplayName("Fecha de Creación")]
        [Required(ErrorMessage = "La fecha NO es opcional")]
        [DataType(DataType.DateTime)]
        public DateTime FechaCreacion { get; set; }
    }
}
