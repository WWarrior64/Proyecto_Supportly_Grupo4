using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Proyecto_Supportly.Models
{
    public class Categorias
    {
        [Key]
        [DisplayName("ID de Categoría")]
        public int CategoriaID { get; set; }

        [DisplayName("Nombre")]
        [Required(ErrorMessage = "El nombre NO es opcional")]
        [StringLength(50, ErrorMessage = "La cantidad máxima de caracteres permitida es {1}")]
        public string Nombre { get; set; }
    }
}
