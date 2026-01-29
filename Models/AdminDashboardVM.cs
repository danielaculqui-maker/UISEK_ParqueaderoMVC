using System;
using System.Collections.Generic;

namespace UISEK_ParqueaderoMVC.Models
{
    public class VehiculoDentroVM
    {
        public int UsuarioId { get; set; }
        public string Correo { get; set; }
        public string Nombre { get; set; }
        public string Placa { get; set; }
        public string Tipo { get; set; }
        public DateTime FechaEntrada { get; set; }
        public string Zona { get; set; }
        public string Fuente { get; set; }
    }

    public class FlujoDiaVM
    {
        public DateTime Dia { get; set; }
        public int Entradas { get; set; }
        public int Salidas { get; set; }
        public int Total { get; set; }
    }

    public class AdminDashboardVM
    {
        public int TotalUsuariosActivos { get; set; }
        public int TotalVehiculosActivos { get; set; }
        public int DentroAhora { get; set; }
        public int MovimientosHoy { get; set; }

        public bool ScriptActivo { get; set; }

        public List<VehiculoDentroVM> VehiculosDentro { get; set; } = new List<VehiculoDentroVM>();
        public List<FlujoDiaVM> TopDiasFlujo { get; set; } = new List<FlujoDiaVM>();
    }
}
