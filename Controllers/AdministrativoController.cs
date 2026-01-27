using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using UISEK_ParqueaderoMVC.Models;

namespace UISEK_ParqueaderoMVC.Controllers
{
    public class AdministrativoController : Controller
    {
        private string ConnStr => ConfigurationManager.ConnectionStrings["UISEK_ParqueaderoDB"].ConnectionString;

        private bool EsAdmin()
        {
            var rol = ((string)Session["Rol"] ?? "").ToUpper();
            return rol == "ADMINISTRATIVO";
        }

        private ActionResult BloquearSiNoAdmin()
        {
            if (!EsAdmin())
                return RedirectToAction("Login", "Auth");
            return null;
        }

        public ActionResult Index()
        {
            var block = BloquearSiNoAdmin();
            if (block != null) return block;

            return View();
        }

        // ✅ LISTA DE USUARIOS (desde BD)
        public ActionResult Usuarios()
        {
            var block = BloquearSiNoAdmin();
            if (block != null) return block;

            var lista = new List<UsuarioRow>();

            using (var conn = new SqlConnection(ConnStr))
            using (var cmd = new SqlCommand(@"
                SELECT 
                    u.UsuarioId,
                    u.Correo,
                    r.Nombre AS Rol,
                    u.TieneDiscapacidad,
                    u.Activo,
                    CONVERT(varchar(19), u.FechaRegistro, 120) AS FechaRegistro,
                    (SELECT COUNT(*) FROM dbo.Vehiculos v WHERE v.UsuarioId = u.UsuarioId) AS Vehiculos
                FROM dbo.Usuarios u
                INNER JOIN dbo.Roles r ON r.RolId = u.RolId
                ORDER BY u.UsuarioId DESC;
            ", conn))
            {
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        lista.Add(new UsuarioRow
                        {
                            UsuarioId = Convert.ToInt32(rd["UsuarioId"]),
                            Correo = Convert.ToString(rd["Correo"]),
                            Rol = Convert.ToString(rd["Rol"]),
                            TieneDiscapacidad = Convert.ToBoolean(rd["TieneDiscapacidad"]),
                            Activo = Convert.ToBoolean(rd["Activo"]),
                            FechaRegistro = Convert.ToString(rd["FechaRegistro"]),
                            Vehiculos = Convert.ToInt32(rd["Vehiculos"])
                        });
                    }
                }
            }

            return View(lista);
        }

        // ✅ REPORTES (dashboard simple)
        public ActionResult Reportes()
        {
            var block = BloquearSiNoAdmin();
            if (block != null) return block;

            var dash = new ReporteDashboard();

            using (var conn = new SqlConnection(ConnStr))
            {
                conn.Open();

                // 1) Totales usuarios / activos
                using (var cmd = new SqlCommand(@"
                    SELECT 
                        COUNT(*) AS TotalUsuarios,
                        SUM(CASE WHEN Activo = 1 THEN 1 ELSE 0 END) AS UsuariosActivos
                    FROM dbo.Usuarios;
                ", conn))
                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        dash.TotalUsuarios = Convert.ToInt32(rd["TotalUsuarios"]);
                        dash.UsuariosActivos = Convert.ToInt32(rd["UsuariosActivos"]);
                    }
                }

                // 2) Total vehículos
                using (var cmd = new SqlCommand(@"SELECT COUNT(*) AS TotalVehiculos FROM dbo.Vehiculos;", conn))
                {
                    dash.TotalVehiculos = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 3) Vehículos dentro ahora (movimientos abiertos)
                using (var cmd = new SqlCommand(@"
                    SELECT COUNT(*) 
                    FROM dbo.Movimientos
                    WHERE FechaSalida IS NULL;
                ", conn))
                {
                    dash.VehiculosDentroAhora = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 4) Ingresos hoy / salidas hoy (por fecha UTC)
                using (var cmd = new SqlCommand(@"
                    SELECT
                        SUM(CASE WHEN CAST(FechaIngreso AS date) = CAST(SYSUTCDATETIME() AS date) THEN 1 ELSE 0 END) AS IngresosHoy,
                        SUM(CASE WHEN FechaSalida IS NOT NULL AND CAST(FechaSalida AS date) = CAST(SYSUTCDATETIME() AS date) THEN 1 ELSE 0 END) AS SalidasHoy
                    FROM dbo.Movimientos;
                ", conn))
                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        dash.IngresosHoy = Convert.ToInt32(rd["IngresosHoy"]);
                        dash.SalidasHoy = Convert.ToInt32(rd["SalidasHoy"]);
                    }
                }

                // 5) Serie: ingresos últimos 7 días
                using (var cmd = new SqlCommand(@"
                    SELECT TOP (7)
                        CONVERT(varchar(10), CAST(FechaIngreso AS date), 120) AS Dia,
                        COUNT(*) AS Cantidad
                    FROM dbo.Movimientos
                    GROUP BY CAST(FechaIngreso AS date)
                    ORDER BY CAST(FechaIngreso AS date) DESC;
                ", conn))
                using (var rd = cmd.ExecuteReader())
                {
                    var temp = new List<SerieDia>();
                    while (rd.Read())
                    {
                        temp.Add(new SerieDia
                        {
                            Dia = Convert.ToString(rd["Dia"]),
                            Cantidad = Convert.ToInt32(rd["Cantidad"])
                        });
                    }
                    temp.Reverse(); // para mostrar en orden ascendente
                    dash.IngresosUltimos7Dias = temp;
                }
            }

            return View(dash);
        }
    }
}
