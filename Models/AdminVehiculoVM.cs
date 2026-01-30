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
    public class AdminVehiculoVM
    {
        public int VehiculoId { get; set; }
        public string Placa { get; set; }
        public string TipoVehiculo { get; set; }
        public string MarcaModelo { get; set; }
        public string CorreoPropietario { get; set; }
        public bool Activo { get; set; }
    }
}
