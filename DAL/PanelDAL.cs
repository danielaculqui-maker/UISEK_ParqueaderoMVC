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
using System;
using System.Collections.Generic;

namespace UISEK_ParqueaderoMVC.Models
{
    public class PanelDAL

    {
        public int UsuarioId { get; set; }

        public string Correo { get; set; }

        public string NombreCompleto { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }

        public bool EstadoActivo { get; set; }
        public bool PermisoVigente { get; set; }
        public bool TieneDiscapacidad { get; set; }

        public string TipoVehiculo { get; set; }
        public string Placa { get; set; }
        public string MarcaModelo { get; set; }

        public List<MovimientoVM> Historial { get; set; } = new List<MovimientoVM>();
    }

    public class MovimientoVM
    {
        public int HistorialId { get; set; }
        public DateTime Fecha { get; set; }
        public string Evento { get; set; }
        public string Zona { get; set; }
        public string Observacion { get; set; }
        public string Fuente { get; set; }
    }
}
