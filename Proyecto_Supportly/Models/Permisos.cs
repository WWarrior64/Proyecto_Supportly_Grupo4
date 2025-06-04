using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Proyecto_Supportly.Models
{
    public class Permisos
    {
        [Key]
        [DisplayName("ID de Permiso")]
        public int PermisoID { get; set; }

        [DisplayName("Nombre")]
        [Required(ErrorMessage = "El nombre NO es opcional")]
        [StringLength(50, ErrorMessage = "La cantidad máxima de caracteres permitida es {1}")]
        public string Nombre { get; set; }

        [DisplayName("Descripción")]
        [StringLength(200, ErrorMessage = "La cantidad máxima de caracteres permitida es {1}")]
        public string? Descripcion { get; set; }
    }
}
