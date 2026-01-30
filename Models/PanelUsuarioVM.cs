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
    public class PanelUsuarioVM
    {
        public string EstadoEnCampus { get; set; }  // DENTRO / FUERA / SIN REGISTROS

        // Identidad
        public int UsuarioId { get; set; }
        public string Correo { get; set; }
        public string Rol { get; set; }

        // Nombre
        public string NombreCompleto { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Cedula { get; set; }

        // Flags
        public bool EstadoActivo { get; set; }
        public bool PermisoVigente { get; set; }
        public bool TieneDiscapacidad { get; set; }

        // Vehículo
        public string TipoVehiculo { get; set; }
        public string Placa { get; set; }
        public string MarcaModelo { get; set; }

        // ==========================
        // FILTROS PARA HISTORIAL
        // ==========================
        // Historial
        public List<MovimientoVM> Historial { get; set; } = new List<MovimientoVM>();
        
    }
}
