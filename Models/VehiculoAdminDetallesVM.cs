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
    public class VehiculoAdminDetallesVM
    {
        public int Id { get; set; }
        public string Placa { get; set; }
        public string Tipo { get; set; }
        public string MarcaModelo { get; set; }
        public bool Activo { get; set; }
        public string Origen { get; set; }        // "UISEK" o "VISITANTE"
        public string Propietario { get; set; }
    }
}