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
namespace UISEK_ParqueaderoMVC.Models
{
    public class UsuarioRow
    {
        public int UsuarioId { get; set; }
        public string Correo { get; set; }
        public string Rol { get; set; }
        public bool TieneDiscapacidad { get; set; }
        public bool Activo { get; set; }
        public string FechaRegistro { get; set; }
        public int Vehiculos { get; set; }
    }
}
