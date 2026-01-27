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
