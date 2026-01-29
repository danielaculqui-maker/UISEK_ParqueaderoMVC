using System;
using System.Configuration;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;
using UISEK_ParqueaderoMVC.Models;

namespace UISEK_ParqueaderoMVC.Controllers
{
    public class AdministrativoController : Controller
    {
        private string CS => ConfigurationManager.ConnectionStrings["UISEK_ParqueaderoDB"].ConnectionString;

        // ✅ Dashboard
        [HttpGet]
        public ActionResult Index()
        {
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();
            if (rol != "ADMINISTRATIVO") return RedirectToAction("Login", "Auth");

            var vm = new AdminDashboardVM();
            vm.ScriptActivo = LeerScriptActivo();

            // 1) KPIs
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
                SELECT
                  (SELECT COUNT(*) FROM dbo.Usuarios WHERE Activo = 1) AS TotalUsuariosActivos,
                  (SELECT COUNT(*) FROM dbo.Vehiculos WHERE Activo = 1) AS TotalVehiculosActivos,
                  (SELECT COUNT(*) FROM dbo.HistorialAccesos
                    WHERE CONVERT(date, FechaEvento) = CONVERT(date, GETDATE())
                  ) AS MovimientosHoy;
            ", con))
            {
                con.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        vm.TotalUsuariosActivos = Convert.ToInt32(rd["TotalUsuariosActivos"]);
                        vm.TotalVehiculosActivos = Convert.ToInt32(rd["TotalVehiculosActivos"]);
                        vm.MovimientosHoy = Convert.ToInt32(rd["MovimientosHoy"]);
                    }
                }
            }

            // 2) Vehículos dentro ahora = último evento ENTRADA
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
                ;WITH Ultimo AS (
                  SELECT 
                    h.UsuarioId, h.FechaEvento, h.Evento, h.Zona, h.Fuente,
                    ROW_NUMBER() OVER(PARTITION BY h.UsuarioId ORDER BY h.FechaEvento DESC) AS rn
                  FROM dbo.HistorialAccesos h
                )
                SELECT TOP 200
                  u.UsuarioId,
                  u.Correo,
                  (ISNULL(u.Nombres,'') + ' ' + ISNULL(u.Apellidos,'')) AS Nombre,
                  v.Placa,
                  v.Tipo,
                  ul.FechaEvento AS FechaEntrada,
                  ul.Zona,
                  ul.Fuente
                FROM Ultimo ul
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = ul.UsuarioId
                LEFT JOIN dbo.Vehiculos v ON v.UsuarioId = u.UsuarioId AND v.Activo = 1
                WHERE ul.rn = 1
                  AND UPPER(LTRIM(RTRIM(ul.Evento))) = 'ENTRADA'
                ORDER BY ul.FechaEvento DESC;
            ", con))
            {
                con.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        vm.VehiculosDentro.Add(new VehiculoDentroVM
                        {
                            UsuarioId = Convert.ToInt32(rd["UsuarioId"]),
                            Correo = rd["Correo"] == DBNull.Value ? "" : rd["Correo"].ToString(),
                            Nombre = rd["Nombre"] == DBNull.Value ? "" : rd["Nombre"].ToString(),
                            Placa = rd["Placa"] == DBNull.Value ? "" : rd["Placa"].ToString(),
                            Tipo = rd["Tipo"] == DBNull.Value ? "" : rd["Tipo"].ToString(),
                            FechaEntrada = Convert.ToDateTime(rd["FechaEntrada"]),
                            Zona = rd["Zona"] == DBNull.Value ? "" : rd["Zona"].ToString(),
                            Fuente = rd["Fuente"] == DBNull.Value ? "" : rd["Fuente"].ToString()
                        });
                    }
                }
            }

            vm.DentroAhora = vm.VehiculosDentro.Count;

            // 3) Top 7 días con mayor flujo (últimos 14 días)
            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
                SELECT TOP 7
                  CONVERT(date, FechaEvento) AS Dia,
                  SUM(CASE WHEN UPPER(Evento)='ENTRADA' THEN 1 ELSE 0 END) AS Entradas,
                  SUM(CASE WHEN UPPER(Evento)='SALIDA' THEN 1 ELSE 0 END) AS Salidas,
                  COUNT(*) AS Total
                FROM dbo.HistorialAccesos
                WHERE FechaEvento >= DATEADD(day, -14, GETDATE())
                GROUP BY CONVERT(date, FechaEvento)
                ORDER BY Total DESC, Dia DESC;
            ", con))
            {
                con.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        vm.TopDiasFlujo.Add(new FlujoDiaVM
                        {
                            Dia = Convert.ToDateTime(rd["Dia"]),
                            Entradas = Convert.ToInt32(rd["Entradas"]),
                            Salidas = Convert.ToInt32(rd["Salidas"]),
                            Total = Convert.ToInt32(rd["Total"])
                        });
                    }
                }
            }

            return View(vm);
        }

        // ✅ JSON para gráfica (Chart.js)
        [HttpGet]
        public ActionResult FlujoPorDiaJson(int days = 14)
        {
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();
            if (rol != "ADMINISTRATIVO") return new HttpStatusCodeResult(403);

            var labels = new List<string>();
            var entradas = new List<int>();
            var salidas = new List<int>();

            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
                SELECT
                  CONVERT(date, FechaEvento) AS Dia,
                  SUM(CASE WHEN UPPER(Evento)='ENTRADA' THEN 1 ELSE 0 END) AS Entradas,
                  SUM(CASE WHEN UPPER(Evento)='SALIDA' THEN 1 ELSE 0 END) AS Salidas
                FROM dbo.HistorialAccesos
                WHERE FechaEvento >= DATEADD(day, -@days, GETDATE())
                GROUP BY CONVERT(date, FechaEvento)
                ORDER BY Dia ASC;
            ", con))
            {
                cmd.Parameters.AddWithValue("@days", days);
                con.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        var d = Convert.ToDateTime(rd["Dia"]);
                        labels.Add(d.ToString("dd/MM"));
                        entradas.Add(Convert.ToInt32(rd["Entradas"]));
                        salidas.Add(Convert.ToInt32(rd["Salidas"]));
                    }
                }
            }

            return Json(new { labels, entradas, salidas }, JsonRequestBehavior.AllowGet);
        }

        // ✅ JSON para refrescar "Dentro ahora" + tabla (AUTO-REFRESH)
        [HttpGet]
        public ActionResult DentroAhoraJson()
        {
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();
            if (rol != "ADMINISTRATIVO") return new HttpStatusCodeResult(403);

            var items = new List<object>();

            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
                ;WITH Ultimo AS (
                  SELECT 
                    h.UsuarioId, h.FechaEvento, h.Evento, h.Zona, h.Fuente,
                    ROW_NUMBER() OVER(PARTITION BY h.UsuarioId ORDER BY h.FechaEvento DESC) AS rn
                  FROM dbo.HistorialAccesos h
                )
                SELECT TOP 200
                  u.UsuarioId,
                  u.Correo,
                  (ISNULL(u.Nombres,'') + ' ' + ISNULL(u.Apellidos,'')) AS Nombre,
                  v.Placa,
                  v.Tipo,
                  ul.FechaEvento AS FechaEntrada,
                  ul.Zona,
                  ul.Fuente
                FROM Ultimo ul
                INNER JOIN dbo.Usuarios u ON u.UsuarioId = ul.UsuarioId
                LEFT JOIN dbo.Vehiculos v ON v.UsuarioId = u.UsuarioId AND v.Activo = 1
                WHERE ul.rn = 1
                  AND UPPER(LTRIM(RTRIM(ul.Evento))) = 'ENTRADA'
                ORDER BY ul.FechaEvento DESC;
            ", con))
            {
                con.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        var fecha = Convert.ToDateTime(rd["FechaEntrada"]);

                        items.Add(new
                        {
                            FechaEntrada = fecha.ToString("dd/MM/yyyy HH:mm"),
                            Placa = rd["Placa"] == DBNull.Value ? "" : rd["Placa"].ToString(),
                            Tipo = rd["Tipo"] == DBNull.Value ? "" : rd["Tipo"].ToString(),
                            Nombre = rd["Nombre"] == DBNull.Value ? "" : rd["Nombre"].ToString(),
                            Correo = rd["Correo"] == DBNull.Value ? "" : rd["Correo"].ToString(),
                            Zona = rd["Zona"] == DBNull.Value ? "" : rd["Zona"].ToString(),
                            Fuente = rd["Fuente"] == DBNull.Value ? "" : rd["Fuente"].ToString()
                        });
                    }
                }
            }

            return Json(new { dentroAhora = items.Count, items }, JsonRequestBehavior.AllowGet);
        }

        // ✅ Botón activar/desactivar script
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleScript()
        {
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();
            if (rol != "ADMINISTRATIVO") return RedirectToAction("Login", "Auth");

            bool actual = LeerScriptActivo();
            string nuevo = actual ? "0" : "1";

            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
                UPDATE dbo.SistemaConfig
                SET Valor = @val
                WHERE Clave = 'SCRIPT_ACTIVO';
            ", con))
            {
                cmd.Parameters.AddWithValue("@val", nuevo);
                con.Open();
                cmd.ExecuteNonQuery();
            }

            TempData["MsgOk"] = actual ? "Sistema desactivado ⛔" : "Sistema activado ✅";
            return RedirectToAction("Index");
        }

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
    }
}
