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

namespace UISEK_ParqueaderoMVC.Models
{
    public class HistorialItemVM
    {
        public int HistorialId { get; set; }
        public string Evento { get; set; }      // ENTRADA / SALIDA
        public DateTime FechaEvento { get; set; }
        public string Fuente { get; set; }
        public string Nota { get; set; }
    }
}
