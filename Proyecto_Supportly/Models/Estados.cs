﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Proyecto_Supportly.Models
{
    public class Estados
    {
        [Key]
        [DisplayName("ID de Estado")]
        public int EstadoID { get; set; }

        [DisplayName("Nombre")]
        [Required(ErrorMessage = "El nombre NO es opcional")]
        [StringLength(50, ErrorMessage = "La cantidad máxima de caracteres permitida es {1}")]
        public string Nombre { get; set; }
    }
}
