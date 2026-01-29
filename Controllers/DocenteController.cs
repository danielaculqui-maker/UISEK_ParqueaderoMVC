using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using UISEK_ParqueaderoMVC.Models;

namespace UISEK_ParqueaderoMVC.Controllers
{
    public class DocenteController : BaseController
    {
        private string CS => ConfigurationManager.ConnectionStrings["UISEK_ParqueaderoDB"].ConnectionString;

        [HttpGet]
        public ActionResult Index(string evento, string desde, string hasta)
        {
            var correo = (Session["Correo"] as string ?? "").Trim().ToLower();
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(correo)) return RedirectToAction("Login", "Auth");
            if (rol != "DOCENTE") return RedirectToAction("Login", "Auth");

            var vm = CargarPanelPorCorreo(correo);
            CargarHistorialFiltrado(vm, evento, desde, hasta);
            vm.EstadoEnCampus = InferirEstado(vm);

            return View(vm);
        }

        [HttpGet]
        public ActionResult MiVehiculo()
        {
            var correo = (Session["Correo"] as string ?? "").Trim().ToLower();
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(correo)) return RedirectToAction("Login", "Auth");
            if (rol != "DOCENTE") return RedirectToAction("Login", "Auth");

            // ✅ Reusar la MISMA vista del estudiante
            // (si ya te funciona perfecto ahí)
            return RedirectToAction("MiVehiculo", "Estudiante");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SimularEntrada(string zona = "Garita Sur")
        {
            var correo = (Session["Correo"] as string ?? "").Trim().ToLower();
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(correo)) return RedirectToAction("Login", "Auth");
            if (rol != "DOCENTE") return RedirectToAction("Login", "Auth");

            int usuarioId = ObtenerUsuarioIdPorCorreo(correo);
            if (usuarioId <= 0) return RedirectToAction("Index");

            string ultimoEvento = ObtenerUltimoEvento(usuarioId);
            if (ultimoEvento == "ENTRADA")
            {
                TempData["MsgHistErr"] = "Ya estás DENTRO. Primero simula una SALIDA.";
                return RedirectToAction("Index");
            }

            InsertarMovimiento(usuarioId, "ENTRADA", zona, "Entrada detectada por sensor simulado", "SENSOR");
            TempData["MsgHistOk"] = "✅ ENTRADA registrada (sensor simulado).";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SimularSalida(string zona = "Garita Sur")
        {
            var correo = (Session["Correo"] as string ?? "").Trim().ToLower();
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(correo)) return RedirectToAction("Login", "Auth");
            if (rol != "DOCENTE") return RedirectToAction("Login", "Auth");

            int usuarioId = ObtenerUsuarioIdPorCorreo(correo);
            if (usuarioId <= 0) return RedirectToAction("Index");

            string ultimoEvento = ObtenerUltimoEvento(usuarioId);
            if (string.IsNullOrWhiteSpace(ultimoEvento) || ultimoEvento == "SALIDA")
            {
                TempData["MsgHistErr"] = "Ya estás FUERA (o no tienes entradas). Primero simula una ENTRADA.";
                return RedirectToAction("Index");
            }

            InsertarMovimiento(usuarioId, "SALIDA", zona, "Salida detectada por sensor simulado", "SENSOR");
            TempData["MsgHistOk"] = "✅ SALIDA registrada (sensor simulado).";
            return RedirectToAction("Index");
        }

        // ============================
        // Helpers (copiados del Estudiante)
        // ============================
        private int ObtenerUsuarioIdPorCorreo(string correo)
        {
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
                SELECT TOP 1 UsuarioId
                FROM dbo.Usuarios
                WHERE LOWER(Correo) = LOWER(@correo);
            ", con))
            {
                cmd.Parameters.AddWithValue("@correo", correo);
                con.Open();
                object val = cmd.ExecuteScalar();
                if (val == null || val == DBNull.Value) return -1;
                return Convert.ToInt32(val);
            }
        }

        private PanelUsuarioVM CargarPanelPorCorreo(string correo)
        {
            var vm = new PanelUsuarioVM();
            vm.Correo = correo;
            vm.Rol = (Session["Rol"] as string ?? "DOCENTE").ToUpper();

            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
                SELECT TOP 1
                    UsuarioId, Correo, Nombres, Apellidos, Cedula,
                    TieneDiscapacidad, Activo, PermisoVigente
                FROM dbo.Usuarios
                WHERE LOWER(Correo) = LOWER(@correo);
            ", con))
            {
                cmd.Parameters.AddWithValue("@correo", correo);
                con.Open();

                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        vm.UsuarioId = Convert.ToInt32(rd["UsuarioId"]);
                        vm.Nombres = rd["Nombres"]?.ToString();
                        vm.Apellidos = rd["Apellidos"]?.ToString();
                        vm.Cedula = rd["Cedula"]?.ToString();

                        vm.TieneDiscapacidad = rd["TieneDiscapacidad"] != DBNull.Value && Convert.ToBoolean(rd["TieneDiscapacidad"]);
                        vm.EstadoActivo = rd["Activo"] != DBNull.Value && Convert.ToBoolean(rd["Activo"]);
                        vm.PermisoVigente = rd["PermisoVigente"] != DBNull.Value && Convert.ToBoolean(rd["PermisoVigente"]);
                    }
                }
            }

            try
            {
                using (var con = new SqlConnection(CS))
                using (var cmd = new SqlCommand(@"
                    SELECT TOP 1 Tipo, Placa, MarcaModelo
                    FROM dbo.Vehiculos
                    WHERE UsuarioId = @id AND Activo = 1
                    ORDER BY VehiculoId DESC;
                ", con))
                {
                    cmd.Parameters.AddWithValue("@id", vm.UsuarioId);
                    con.Open();

                    using (var rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            vm.TipoVehiculo = rd["Tipo"] == DBNull.Value ? null : rd["Tipo"].ToString();
                            vm.Placa = rd["Placa"] == DBNull.Value ? null : rd["Placa"].ToString();
                            vm.MarcaModelo = rd["MarcaModelo"] == DBNull.Value ? null : rd["MarcaModelo"].ToString();
                        }
                    }
                }
            }
            catch { /* ignorar */ }

            return vm;
        }

        private void CargarHistorialFiltrado(PanelUsuarioVM vm, string evento, string desde, string hasta)
        {
            vm.Historial.Clear();
            evento = (evento ?? "").Trim().ToUpper();

            DateTime? fDesde = null;
            DateTime? fHasta = null;

            if (!string.IsNullOrWhiteSpace(desde) && DateTime.TryParse(desde, out var d1))
                fDesde = d1.Date;

            if (!string.IsNullOrWhiteSpace(hasta) && DateTime.TryParse(hasta, out var d2))
                fHasta = d2.Date.AddDays(1).AddTicks(-1);

            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
                SELECT TOP 50
                    HistorialId, FechaEvento, Evento, Zona, Observacion, Fuente
                FROM dbo.HistorialAccesos
                WHERE UsuarioId = @id
                  AND (@evento = '' OR UPPER(LTRIM(RTRIM(Evento))) = @evento)
                  AND (@desde IS NULL OR FechaEvento >= @desde)
                  AND (@hasta IS NULL OR FechaEvento <= @hasta)
                ORDER BY FechaEvento DESC;
            ", con))
            {
                cmd.Parameters.AddWithValue("@id", vm.UsuarioId);
                cmd.Parameters.AddWithValue("@evento", evento);
                cmd.Parameters.AddWithValue("@desde", (object)fDesde ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@hasta", (object)fHasta ?? DBNull.Value);

                con.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        vm.Historial.Add(new MovimientoVM
                        {
                            HistorialId = Convert.ToInt32(rd["HistorialId"]),
                            Fecha = Convert.ToDateTime(rd["FechaEvento"]),
                            Evento = rd["Evento"]?.ToString(),
                            Zona = rd["Zona"] == DBNull.Value ? "" : rd["Zona"].ToString(),
                            Observacion = rd["Observacion"] == DBNull.Value ? "" : rd["Observacion"].ToString(),
                            Fuente = rd["Fuente"] == DBNull.Value ? "" : rd["Fuente"].ToString()
                        });
                    }
                }
            }
        }

        private string ObtenerUltimoEvento(int usuarioId)
        {
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
                SELECT TOP 1 Evento
                FROM dbo.HistorialAccesos
                WHERE UsuarioId = @id
                ORDER BY FechaEvento DESC;
            ", con))
            {
                cmd.Parameters.AddWithValue("@id", usuarioId);
                con.Open();
                object val = cmd.ExecuteScalar();
                return (val == null || val == DBNull.Value) ? "" : val.ToString().Trim().ToUpper();
            }
        }

        private void InsertarMovimiento(int usuarioId, string evento, string zona, string observacion, string fuente)
        {
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
        INSERT INTO dbo.HistorialAccesos
        (UsuarioId, Evento, FechaEvento, Zona, Observacion, Fuente)
        VALUES
        (@id, @evento, GETDATE(), @zona, @obs, @fuente);
    ", con))
            {
                cmd.Parameters.AddWithValue("@id", usuarioId);
                cmd.Parameters.AddWithValue("@evento", (evento ?? "").Trim().ToUpper());
                cmd.Parameters.AddWithValue("@zona", string.IsNullOrWhiteSpace(zona) ? (object)DBNull.Value : zona.Trim());
                cmd.Parameters.AddWithValue("@obs", string.IsNullOrWhiteSpace(observacion) ? (object)DBNull.Value : observacion.Trim());
                cmd.Parameters.AddWithValue("@fuente", string.IsNullOrWhiteSpace(fuente) ? (object)DBNull.Value : fuente.Trim().ToUpper());

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }


        private string InferirEstado(PanelUsuarioVM vm)
        {
            if (vm.Historial == null || vm.Historial.Count == 0) return "SIN REGISTROS";
            var ultimo = (vm.Historial[0].Evento ?? "").Trim().ToUpper();
            if (ultimo == "ENTRADA") return "DENTRO";
            if (ultimo == "SALIDA") return "FUERA";
            return "SIN REGISTROS";
        }
    }
}
