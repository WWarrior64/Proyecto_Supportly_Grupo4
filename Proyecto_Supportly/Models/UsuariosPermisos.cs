using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Proyecto_Supportly.Models
{
    public class UsuariosPermisos
    {
        [DisplayName("ID de Usuario")]
        public int UsuarioID { get; set; }

        [DisplayName("ID de Permiso")]
        public int PermisoID { get; set; }

    }
}
