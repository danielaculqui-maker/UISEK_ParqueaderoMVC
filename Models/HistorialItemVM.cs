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
