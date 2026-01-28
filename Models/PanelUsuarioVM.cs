using System;
using System.Collections.Generic;

namespace UISEK_ParqueaderoMVC.Models
{
    public class PanelUsuarioVM
    {
        public int UsuarioId { get; set; }
        public string Correo { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string NombreCompleto { get; set; }
        public string Cedula { get; set; }

        public string Rol { get; set; }
        public bool EstadoActivo { get; set; }
        public bool PermisoVigente { get; set; }
        public bool TieneDiscapacidad { get; set; }

        public string AreaDependencia { get; set; }
        public string GaritaAsignada { get; set; }
        public string Turno { get; set; }

        public string TipoVehiculo { get; set; }
        public string Placa { get; set; }
        public string MarcaModelo { get; set; }

        public List<MovimientoVM> Historial { get; set; } = new List<MovimientoVM>();

        public class MovimientoVM
        {
            public DateTime Fecha { get; set; }
            public string Evento { get; set; }
            public string Zona { get; set; }
            public string Observacion { get; set; }
        }
    }
}
