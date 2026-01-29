using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UISEK_ParqueaderoMVC.Models
{
    public class UsuarioReporteVM
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Rol { get; set; }
        public bool Activo { get; set; }
        public string Origen { get; set; } // "UISEK" / "VISITANTE"
    }
}