using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Proyecto_Supportly.Models
{
    public class Usuarios
    {
        [Key]
        [DisplayName("ID de Usuario")]
        public int UsuarioID { get; set; }

        [DisplayName("Nombre")]
        [Required(ErrorMessage = "El nombre NO es opcional")]
        [StringLength(100, ErrorMessage = "La cantidad máxima de caracteres permitida es {1}")]
        public string Nombre { get; set; }

        [DisplayName("Email")]
        [Required(ErrorMessage = "El email NO es opcional")]
        [StringLength(100, ErrorMessage = "La cantidad máxima de caracteres permitida es {1}")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [DisplayName("Contraseña")]
        [Required(ErrorMessage = "La contraseña NO es opcional")]
        [StringLength(256, ErrorMessage = "La cantidad máxima de caracteres permitida es {1}")]
        [DataType(DataType.Password)]
        public string Contraseña { get; set; }

        [DisplayName("Teléfono")]
        [StringLength(20, ErrorMessage = "La cantidad máxima de caracteres permitida es {1}")]
        public string? Telefono { get; set; }

        [DisplayName("Tipo de Usuario")]
        [StringLength(20, ErrorMessage = "La cantidad máxima de caracteres permitida es {1}")]
        public string TipoUsuario { get; set; }

        [DisplayName("Empresa")]
        [StringLength(100, ErrorMessage = "La cantidad máxima de caracteres permitida es {1}")]
        public string? Empresa { get; set; }

        [DisplayName("Contacto Principal")]
        [StringLength(100, ErrorMessage = "La cantidad máxima de caracteres permitida es {1}")]
        public string? NombreContactoPrincipal { get; set; }

        [DisplayName("Rol")]
        [Required(ErrorMessage = "El rol NO es opcional")]
        public int RolID { get; set; }

        [DisplayName("Fecha de Creación")]
        [Required(ErrorMessage = "La fecha de creación NO es opcional")]
        [DataType(DataType.DateTime)]
        public DateTime FechaCreacion { get; set; }

        [DisplayName("Estado de Usuario")]
        [Required(ErrorMessage = "El estado NO es opcional")]
        public bool EstadoUsuario { get; set; }
    }
}
