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

namespace UISEK_ParqueaderoMVC.App_Start
{
    public static class AppState
    {
        // Estado del sistema (simulado)
        public static bool SistemaActivo { get; set; } = true;

        // Bitácora de contingencia en memoria (si el sistema cae)
        public static List<ContingenciaItem> Contingencia { get; } = new List<ContingenciaItem>();
    }

    public class ContingenciaItem
    {
        public int UsuarioId { get; set; }
        public DateTime FechaHora { get; set; } = DateTime.Now;
        public string Placa { get; set; }
        public string Evento { get; set; } // "ENTRADA" / "SALIDA"
        public string Zona { get; set; }
        public string Nota { get; set; }
        public string RegistradoPor { get; set; } // correo del guardia 
    }
}
