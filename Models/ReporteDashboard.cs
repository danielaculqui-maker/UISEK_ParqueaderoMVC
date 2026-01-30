/* ============================================================
 NOMBRE DEL PROYECTO:
 SISTEMA INTELIGENTE DE CONTROL DE PARQUEADEROS UISEK

 CREADO POR:
 Daniela Culqui
 Alberto Andrade
 Cristian Tenorio

 FECHA DE ENTREGA:
 29/01/2026
============================================================ */
using System.Collections.Generic;

namespace UISEK_ParqueaderoMVC.Models
{
    public class ReporteDashboard
    {
        public int TotalUsuarios { get; set; }
        public int UsuariosActivos { get; set; }
        public int TotalVehiculos { get; set; }
        public int VehiculosDentroAhora { get; set; }
        public int IngresosHoy { get; set; }
        public int SalidasHoy { get; set; }

        public List<SerieDia> IngresosUltimos7Dias { get; set; } = new List<SerieDia>();
    }

    public class SerieDia
    {
        public string Dia { get; set; }      // ej: 2026-01-26
        public int Cantidad { get; set; }
    }
}
