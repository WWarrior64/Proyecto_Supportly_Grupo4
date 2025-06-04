using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Proyecto_Supportly.Models
{
    public class Tickets
    {
        [Key]
        [DisplayName("ID de Ticket")]
        public int TicketID { get; set; }

        [DisplayName("Título")]
        [Required(ErrorMessage = "El título NO es opcional")]
        [StringLength(200, ErrorMessage = "La cantidad máxima de caracteres permitida es {1}")]
        public string Titulo { get; set; }

        [DisplayName("Descripción")]
        [Required(ErrorMessage = "La descripción NO es opcional")]
        public string Descripcion { get; set; }

        [DisplayName("Fecha de Creación")]
        [Required(ErrorMessage = "La fecha de creación NO es opcional")]
        [DataType(DataType.DateTime)]
        public DateTime FechaCreacion { get; set; }

        [DisplayName("Fecha de Cierre")]
        [DataType(DataType.DateTime)]
        public DateTime? FechaCierre { get; set; }

        [DisplayName("Prioridad")]
        [Required(ErrorMessage = "La prioridad NO es opcional")]
        [StringLength(20, ErrorMessage = "La cantidad máxima de caracteres permitida es {1}")]
        public string Prioridad { get; set; }

        [DisplayName("Categoría")]
        [Required(ErrorMessage = "La categoría NO es opcional")]
        public int CategoriaID { get; set; }

        [DisplayName("Usuario Creador")]
        [Required(ErrorMessage = "El usuario creador NO es opcional")]
        public int UsuarioCreadorID { get; set; }

        [DisplayName("Estado")]
        [Required(ErrorMessage = "El estado NO es opcional")]
        public int EstadoID { get; set; }
    }
}
