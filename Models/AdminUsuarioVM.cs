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
    public class AdminUsuarioVM
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Rol { get; set; }
        public bool Activo { get; set; }
    }

    public class AdminUsuarioListVM
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Tipo { get; set; }
        public bool Activo { get; set; }
    }
    public class AdminUsuarioDetallesVM
    {
        public int Id { get; set; }
        public string Tipo { get; set; } // UISEK / VISITANTE

        public string Nombre { get; set; }
        public string Correo { get; set; }
        public string Cedula { get; set; }

        // Vehículo / datos extra
        public string Placa { get; set; }
        public string TipoVehiculo { get; set; }
        public string MarcaModelo { get; set; }

        public bool Activo { get; set; }
    }


}
