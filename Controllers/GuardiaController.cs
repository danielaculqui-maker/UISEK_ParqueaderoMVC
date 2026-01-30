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
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using UISEK_ParqueaderoMVC.App_Start;

namespace UISEK_ParqueaderoMVC.Controllers
{
    public class GuardiaController : Controller
    {
        private string CS => ConfigurationManager.ConnectionStrings["UISEK_ParqueaderoDB"].ConnectionString;

        private bool EsGuardia()
        {
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();
            return rol == "GUARDIA";
        }

        // GET: /Guardia/Index
        [HttpGet]
        public ActionResult Index()
        {
            if (!EsGuardia()) return RedirectToAction("Login", "Auth");

            ViewBag.ScriptActivo = LeerScriptActivo(); // ✅ usa tu mismo config
            ViewBag.Contingencia = AppState.Contingencia;
            return View();
        }

        // ===========================
        // REGISTRO NORMAL (solo si SCRIPT_ACTIVO=1)
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registrar(string placa, string evento, string zona)
        {
            if (!EsGuardia()) return RedirectToAction("Login", "Auth");

            bool activo = LeerScriptActivo();
            if (!activo)
            {
                TempData["Err"] = "Sistema desactivado ⛔ Usa registro de CONTINGENCIA.";
                return RedirectToAction("Index");
            }

            placa = (placa ?? "").Trim().ToUpper();
            evento = (evento ?? "").Trim().ToUpper();
            zona = (zona ?? "").Trim();

            if (string.IsNullOrWhiteSpace(placa) || (evento != "ENTRADA" && evento != "SALIDA"))
            {
                TempData["Err"] = "Placa y evento válido (ENTRADA/SALIDA) son obligatorios.";
                return RedirectToAction("Index");
            }

            int? usuarioId = BuscarUsuarioIdPorPlaca(placa);
            if (!usuarioId.HasValue)
            {
                TempData["Err"] = "No existe vehículo activo con esa placa (UISEK).";
                return RedirectToAction("Index");
            }

            InsertarHistorialAcceso(usuarioId.Value, evento, zona, "SISTEMA"); // fuente normal
            TempData["MsgOk"] = $"{evento} registrada ✅";
            return RedirectToAction("Index");
        }

        // ===========================
        // CONTINGENCIA (solo si SCRIPT_ACTIVO=0)
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarContingencia(string placa, string evento, string zona, string nota)
        {
            if (!EsGuardia()) return RedirectToAction("Login", "Auth");

            bool activo = LeerScriptActivo();
            if (activo)
            {
                TempData["Err"] = "El sistema está activo ✅ Usa registro normal.";
                return RedirectToAction("Index");
            }

            placa = (placa ?? "").Trim().ToUpper();
            evento = (evento ?? "").Trim().ToUpper();
            zona = (zona ?? "").Trim();
            nota = (nota ?? "").Trim();

            if (string.IsNullOrWhiteSpace(placa) || (evento != "ENTRADA" && evento != "SALIDA"))
            {
                TempData["Err"] = "Placa y evento válido (ENTRADA/SALIDA) son obligatorios.";
                return RedirectToAction("Index");
            }

            int? usuarioId = BuscarUsuarioIdPorPlaca(placa);
            if (!usuarioId.HasValue)
            {
                TempData["Err"] = "No existe vehículo activo con esa placa (UISEK).";
                return RedirectToAction("Index");
            }

            AppState.Contingencia.Add(new ContingenciaItem
            {
                UsuarioId = usuarioId.Value,
                Placa = placa,
                Evento = evento,
                Zona = zona,
                Nota = nota,
                RegistradoPor = (string)Session["Correo"]
            });


            TempData["MsgOk"] = "Guardado en CONTINGENCIA (manual) ✅";
            return RedirectToAction("Index");
        }

        // ===========================
        // CONSOLIDAR (cuando vuelve a activo)
        // ===========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConsolidarContingencia()
        {
            if (!EsGuardia()) return RedirectToAction("Login", "Auth");

            bool activo = LeerScriptActivo();
            if (!activo)
            {
                TempData["Err"] = "Activa el sistema primero ✅ para consolidar.";
                return RedirectToAction("Index");
            }

            foreach (var item in AppState.Contingencia)
            {
                InsertarHistorialAcceso(item.UsuarioId, item.Evento, item.Zona, "MANUAL"); // fuente manual
            }

            AppState.Contingencia.Clear();
            TempData["MsgOk"] = "Contingencia consolidada en BD ✅";
            return RedirectToAction("Index");
        }

        // ===========================
        // Helpers (reusando tu config)
        // ===========================
        private bool LeerScriptActivo()
        {
            try
            {
                using (var con = new SqlConnection(CS))
                using (var cmd = new SqlCommand(@"
                    SELECT TOP 1 Valor
                    FROM dbo.SistemaConfig
                    WHERE Clave = 'SCRIPT_ACTIVO';
                ", con))
                {
                    con.Open();
                    var val = cmd.ExecuteScalar();
                    var s = (val == null || val == DBNull.Value) ? "1" : val.ToString().Trim();
                    return s == "1" || s.Equals("true", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { return true; }
        }

        private int? BuscarUsuarioIdPorPlaca(string placa)
        {
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
                SELECT TOP 1 UsuarioId
                FROM dbo.Vehiculos
                WHERE Activo = 1 AND UPPER(LTRIM(RTRIM(Placa))) = @placa;
            ", con))
            {
                cmd.Parameters.AddWithValue("@placa", placa);
                con.Open();
                var o = cmd.ExecuteScalar();
                if (o == null || o == DBNull.Value) return null;
                return Convert.ToInt32(o);
            }
        }

        private void InsertarHistorialAcceso(int usuarioId, string evento, string zona, string fuente)
        {
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
                INSERT INTO dbo.HistorialAccesos (UsuarioId, FechaEvento, Evento, Zona, Fuente)
                VALUES (@uid, GETDATE(), @evento, @zona, @fuente);
            ", con))
            {
                cmd.Parameters.AddWithValue("@uid", usuarioId);
                cmd.Parameters.AddWithValue("@evento", evento);
                cmd.Parameters.AddWithValue("@zona", (object)zona ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@fuente", (object)fuente ?? DBNull.Value);

                con.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
