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
using System.Linq;
using System.Web;

namespace UISEK_ParqueaderoMVC.Models
{
    public class EventoMapa
    {
        public int Id { get; set; }
        public string Titulo { get; set; }       // "Parqueadero BLOQUEADO", "Cupo disponible Zona A"
        public string Tipo { get; set; }         // "BLOQUEO", "DISPONIBLE", "ALERTA"
        public string Descripcion { get; set; }  // texto corto
        public DateTime Fecha { get; set; }
        public decimal Latitud { get; set; }
        public decimal Longitud { get; set; }
    }
}