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

        // ==========================================================
        // ✅ DASHBOARD ADMINISTRATIVO
        // (NO incluye visitantes en KPIs, como decidiste)
        // ==========================================================
        [HttpGet]
        public ActionResult Index()
        {
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();
            if (rol != "ADMINISTRATIVO") return RedirectToAction("Login", "Auth");

            var vm = new AdminDashboardVM();
            vm.ScriptActivo = LeerScriptActivo();

            // 1) KPIs (solo sistema institucional)
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

        // ✅ JSON auto-refresh: Dentro ahora + tabla
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

        // ==========================================================
        // ✅ GESTIONAR PERSONAS (Usuarios + Visitantes) - LISTA
        // (Aquí sí aparecen visitantes, como lo dejaste)
        // ==========================================================
        [HttpGet]
        public ActionResult Usuarios()
        {
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();
            if (rol != "ADMINISTRATIVO") return RedirectToAction("Login", "Auth");

            var lista = new List<AdminUsuarioListVM>();

            using (var con = new SqlConnection(CS))
            using (var cmd = new SqlCommand(@"
                SELECT * FROM (
                    -- 👤 USUARIOS UISEK
                    SELECT 
                        u.UsuarioId AS Id,
                        (ISNULL(u.Nombres,'') + ' ' + ISNULL(u.Apellidos,'')) AS Nombre,
                        ISNULL(u.Correo,'') AS Correo,
                        'UISEK' AS Tipo,
                        u.Activo
                    FROM dbo.Usuarios u

                    UNION ALL

                    -- 🚶 VISITANTES
                    SELECT
                        v.VisitanteId AS Id,
                        v.Nombre,
                        ISNULL(v.CorreoContacto,'') AS Correo,
                        'VISITANTE' AS Tipo,
                        v.Activo
                    FROM dbo.Visitantes v
                ) X
                ORDER BY Tipo, Activo DESC, Nombre ASC;
            ", con))
            {
                con.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        lista.Add(new AdminUsuarioListVM
                        {
                            Id = Convert.ToInt32(rd["Id"]),
                            Nombre = rd["Nombre"].ToString(),
                            Correo = rd["Correo"].ToString(),
                            Tipo = rd["Tipo"].ToString(),
                            Activo = Convert.ToBoolean(rd["Activo"])
                        });
                    }
                }
            }

            return View(lista);
        }

        // ==========================================================
        // 👁 DETALLES (UISEK + VISITANTE)
        // ==========================================================
        [HttpGet]
        public ActionResult UsuarioDetalles(int? id, string tipo = "UISEK")
        {
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();
            if (rol != "ADMINISTRATIVO") return RedirectToAction("Login", "Auth");

            if (!id.HasValue) return RedirectToAction("Usuarios");
            tipo = (tipo ?? "UISEK").Trim().ToUpper();

            var vm = new AdminUsuarioDetallesVM { Id = id.Value, Tipo = tipo };

            using (var con = new SqlConnection(CS))
            {
                con.Open();

                if (tipo == "VISITANTE")
                {
                    using (var cmd = new SqlCommand(@"
                SELECT TOP 1 Nombre, CorreoContacto, Cedula, Placa, TipoVehiculo, MarcaModelo, Activo
                FROM dbo.Visitantes
                WHERE VisitanteId = @id;
            ", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id.Value);
                        using (var rd = cmd.ExecuteReader())
                        {
                            if (!rd.Read()) return HttpNotFound();

                            vm.Nombre = rd["Nombre"]?.ToString();
                            vm.Correo = rd["CorreoContacto"]?.ToString();
                            vm.Cedula = rd["Cedula"]?.ToString();
                            vm.Placa = rd["Placa"]?.ToString();
                            vm.TipoVehiculo = rd["TipoVehiculo"]?.ToString();
                            vm.MarcaModelo = rd["MarcaModelo"]?.ToString();
                            vm.Activo = Convert.ToBoolean(rd["Activo"]);
                        }
                    }
                }
                else // UISEK
                {
                    using (var cmd = new SqlCommand(@"
                SELECT TOP 1 Nombres, Apellidos, Correo, Cedula, Activo
                FROM dbo.Usuarios
                WHERE UsuarioId = @id;
            ", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id.Value);
                        using (var rd = cmd.ExecuteReader())
                        {
                            if (!rd.Read()) return HttpNotFound();

                            var n = rd["Nombres"]?.ToString() ?? "";
                            var a = rd["Apellidos"]?.ToString() ?? "";
                            vm.Nombre = (n + " " + a).Trim();
                            vm.Correo = rd["Correo"]?.ToString();
                            vm.Cedula = rd["Cedula"]?.ToString();
                            vm.Activo = Convert.ToBoolean(rd["Activo"]);
                        }
                    }
                }
            }

            return View(vm);
        }


        // ==========================================================
        // ✏️ EDITAR (GET) UISEK + VISITANTE
        // ==========================================================
        [HttpGet]
        public ActionResult UsuarioEditar(int? id, string tipo = "UISEK")
        {
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();
            if (rol != "ADMINISTRATIVO") return RedirectToAction("Login", "Auth");

            if (!id.HasValue) return RedirectToAction("Usuarios");
            tipo = (tipo ?? "UISEK").Trim().ToUpper();

            var vm = new AdminUsuarioDetallesVM { Id = id.Value, Tipo = tipo };

            using (var con = new SqlConnection(CS))
            {
                con.Open();

                if (tipo == "VISITANTE")
                {
                    using (var cmd = new SqlCommand(@"
                SELECT TOP 1 Nombre, CorreoContacto, Cedula, Placa, TipoVehiculo, MarcaModelo, Activo
                FROM dbo.Visitantes
                WHERE VisitanteId = @id;
            ", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id.Value);
                        using (var rd = cmd.ExecuteReader())
                        {
                            if (!rd.Read()) return RedirectToAction("Usuarios");

                            vm.Nombre = rd["Nombre"]?.ToString();
                            vm.Correo = rd["CorreoContacto"]?.ToString();
                            vm.Cedula = rd["Cedula"]?.ToString();
                            vm.Placa = rd["Placa"]?.ToString();
                            vm.TipoVehiculo = rd["TipoVehiculo"]?.ToString();
                            vm.MarcaModelo = rd["MarcaModelo"]?.ToString();
                            vm.Activo = Convert.ToBoolean(rd["Activo"]);
                        }
                    }
                }
                else // UISEK
                {
                    using (var cmd = new SqlCommand(@"
                SELECT TOP 1 Nombres, Apellidos, Correo, Cedula, Activo
                FROM dbo.Usuarios
                WHERE UsuarioId = @id;
            ", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id.Value);
                        using (var rd = cmd.ExecuteReader())
                        {
                            if (!rd.Read()) return RedirectToAction("Usuarios");

                            var n = rd["Nombres"]?.ToString() ?? "";
                            var a = rd["Apellidos"]?.ToString() ?? "";
                            vm.Nombre = (n + " " + a).Trim();
                            vm.Correo = rd["Correo"]?.ToString();
                            vm.Cedula = rd["Cedula"]?.ToString();
                            vm.Activo = Convert.ToBoolean(rd["Activo"]);
                        }
                    }
                }
            }

            return View(vm);
        }


        // ==========================================================
        // ✏️ EDITAR (POST) UISEK + VISITANTE
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UsuarioEditar(AdminUsuarioDetallesVM vm)
        {
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();
            if (rol != "ADMINISTRATIVO") return RedirectToAction("Login", "Auth");

            vm.Tipo = (vm.Tipo ?? "UISEK").Trim().ToUpper();

            if (vm.Id <= 0) return RedirectToAction("Usuarios");

            using (var con = new SqlConnection(CS))
            {
                con.Open();

                if (vm.Tipo == "VISITANTE")
                {
                    using (var cmd = new SqlCommand(@"
                UPDATE dbo.Visitantes
                SET Nombre = @nombre,
                    Cedula = @cedula,
                    Placa = @placa,
                    TipoVehiculo = @tipoVehiculo,
                    MarcaModelo = @marcaModelo,
                    CorreoContacto = @correo,
                    Activo = @activo
                WHERE VisitanteId = @id;
            ", con))
                    {
                        cmd.Parameters.AddWithValue("@nombre", (vm.Nombre ?? "").Trim());
                        cmd.Parameters.AddWithValue("@cedula", (vm.Cedula ?? "").Trim());
                        cmd.Parameters.AddWithValue("@placa", (vm.Placa ?? "").Trim().ToUpper());
                        cmd.Parameters.AddWithValue("@tipoVehiculo", (vm.TipoVehiculo ?? "").Trim().ToUpper());
                        cmd.Parameters.AddWithValue("@marcaModelo", (vm.MarcaModelo ?? "").Trim());
                        cmd.Parameters.AddWithValue("@correo", (vm.Correo ?? "").Trim());
                        cmd.Parameters.AddWithValue("@activo", vm.Activo);
                        cmd.Parameters.AddWithValue("@id", vm.Id);

                        cmd.ExecuteNonQuery();
                    }
                }
                else // UISEK
                {
                    // Separar nombre en Nombres/Apellidos (simple)
                    var full = (vm.Nombre ?? "").Trim();
                    var nombres = full;
                    var apellidos = "";

                    if (full.Contains(" "))
                    {
                        var parts = full.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        nombres = parts.Length > 0 ? parts[0] : full;
                        apellidos = parts.Length > 1 ? parts[1] : "";
                    }

                    using (var cmd = new SqlCommand(@"
                UPDATE dbo.Usuarios
                SET Nombres = @nombres,
                    Apellidos = @apellidos,
                    Cedula = @cedula,
                    Correo = @correo,
                    Activo = @activo
                WHERE UsuarioId = @id;
            ", con))
                    {
                        cmd.Parameters.AddWithValue("@nombres", (nombres ?? "").Trim());
                        cmd.Parameters.AddWithValue("@apellidos", (apellidos ?? "").Trim());
                        cmd.Parameters.AddWithValue("@cedula", (vm.Cedula ?? "").Trim());
                        cmd.Parameters.AddWithValue("@correo", (vm.Correo ?? "").Trim());
                        cmd.Parameters.AddWithValue("@activo", vm.Activo);
                        cmd.Parameters.AddWithValue("@id", vm.Id);

                        cmd.ExecuteNonQuery();
                    }
                }
            }

            return RedirectToAction("Usuarios");
        }


        // ==========================================================
        // 🗑 ELIMINAR / DESACTIVAR (POST) UISEK + VISITANTE
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UsuarioEliminar(int id, string tipo)
        {
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();
            if (rol != "ADMINISTRATIVO") return RedirectToAction("Login", "Auth");

            tipo = (tipo ?? "UISEK").Trim().ToUpper();

            using (var con = new SqlConnection(CS))
            {
                con.Open();

                if (tipo == "VISITANTE")
                {
                    using (var cmd = new SqlCommand("UPDATE dbo.Visitantes SET Activo=0 WHERE VisitanteId=@id;", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var cmd = new SqlCommand("UPDATE dbo.Usuarios SET Activo=0 WHERE UsuarioId=@id;", con))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            return RedirectToAction("Usuarios");
        }
        // ==========================================================
        // ✅ LISTADO DE VEHÍCULOS (UISEK + VISITANTES)
        // Ruta: /Administrativo/Vehiculos
        // ==========================================================
        [HttpGet]
        public ActionResult Vehiculos()
        {
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();
            if (rol != "ADMINISTRATIVO") return RedirectToAction("Login", "Auth");

            var lista = new List<VehiculoAdminDetallesVM>();

            using (var con = new SqlConnection(CS))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
                    SELECT * FROM (
                        -- 🚗 Vehículos de Usuarios UISEK
                        SELECT 
                            v.VehiculoId AS Id, 
                            v.Placa, 
                            v.Tipo, 
                            v.MarcaModelo, 
                            v.Activo, 
                            'UISEK' AS Origen,
                            (u.Nombres + ' ' + u.Apellidos) AS Propietario
                        FROM dbo.Vehiculos v
                        INNER JOIN dbo.Usuarios u ON v.UsuarioId = u.UsuarioId

                        UNION ALL

                        -- 🚗 Vehículos de Visitantes
                        SELECT 
                            vv.VehiculoVisitanteId AS Id, 
                            vv.Placa, 
                            vv.Tipo, 
                            vv.MarcaModelo, 
                            vv.Activo, 
                            'VISITANTE' AS Origen,
                            vi.Nombre AS Propietario
                        FROM dbo.VehiculosVisitante vv
                        INNER JOIN dbo.Visitantes vi ON vv.VisitanteId = vi.VisitanteId
                    ) X
                    ORDER BY Activo DESC, Placa ASC;
                ", con))
                {
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            lista.Add(new VehiculoAdminDetallesVM
                            {
                                Id = Convert.ToInt32(rd["Id"]),
                                Placa = rd["Placa"].ToString(),
                                Tipo = rd["Tipo"].ToString(),
                                MarcaModelo = rd["MarcaModelo"]?.ToString(),
                                Activo = Convert.ToBoolean(rd["Activo"]),
                                Origen = rd["Origen"].ToString(),
                                Propietario = rd["Propietario"]?.ToString()
                            });
                        }
                    }
                }
            }

            return View(lista);
        }

        // ==========================================================
        // ✅ DETALLES DE VEHÍCULO (UISEK + VISITANTE)
        // Ruta: /Administrativo/VehiculoDetalles/1
        // ==========================================================
        [HttpGet]
        public ActionResult VehiculoDetalles(int id)
        {
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();
            if (rol != "ADMINISTRATIVO") return RedirectToAction("Login", "Auth");

            VehiculoAdminDetallesVM model = null;

            using (var con = new SqlConnection(CS))
            {
                con.Open();
                using (var cmd = new SqlCommand(@"
                    SELECT TOP 1 * FROM (
                        -- 🚗 UISEK
                        SELECT 
                            v.VehiculoId AS Id,
                            v.Placa,
                            v.Tipo,
                            v.MarcaModelo,
                            v.Activo,
                            'UISEK' AS Origen,
                            (u.Nombres + ' ' + u.Apellidos) AS Propietario
                        FROM dbo.Vehiculos v
                        INNER JOIN dbo.Usuarios u ON v.UsuarioId = u.UsuarioId
                        WHERE v.VehiculoId = @id

                        UNION ALL

                        -- 🚗 VISITANTE
                        SELECT 
                            vv.VehiculoVisitanteId AS Id,
                            vv.Placa,
                            vv.Tipo,
                            vv.MarcaModelo,
                            vv.Activo,
                            'VISITANTE' AS Origen,
                            vi.Nombre AS Propietario
                        FROM dbo.VehiculosVisitante vv
                        INNER JOIN dbo.Visitantes vi ON vv.VisitanteId = vi.VisitanteId
                        WHERE vv.VehiculoVisitanteId = @id
                    ) X;
                ", con))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    using (var rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            model = new VehiculoAdminDetallesVM
                            {
                                Id = Convert.ToInt32(rd["Id"]),
                                Placa = rd["Placa"].ToString(),
                                Tipo = rd["Tipo"].ToString(),
                                MarcaModelo = rd["MarcaModelo"]?.ToString(),
                                Activo = Convert.ToBoolean(rd["Activo"]),
                                Origen = rd["Origen"].ToString(),
                                Propietario = rd["Propietario"]?.ToString()
                            };
                        }
                    }
                }
            }

            if (model == null) return HttpNotFound("No existe vehículo con id=" + id);

            return View(model);
        }

        // ==========================================================
        // ✅ EDITAR VEHÍCULO (GET)
        // /Administrativo/VehiculoEditar?id=1&origen=UISEK
        // ==========================================================
        [HttpGet]
        public ActionResult VehiculoEditar(int id, string origen)
        {
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();
            if (rol != "ADMINISTRATIVO") return RedirectToAction("Login", "Auth");

            origen = (origen ?? "").Trim().ToUpper();

            VehiculoAdminDetallesVM model = null;

            using (var con = new SqlConnection(CS))
            {
                con.Open();

                string sql = (origen == "VISITANTE")
                    ? @"
                        SELECT TOP 1
                            vv.VehiculoVisitanteId AS Id,
                            vv.Placa,
                            vv.Tipo,
                            vv.MarcaModelo,
                            vv.Activo,
                            'VISITANTE' AS Origen,
                            vi.Nombre AS Propietario
                        FROM dbo.VehiculosVisitante vv
                        INNER JOIN dbo.Visitantes vi ON vv.VisitanteId = vi.VisitanteId
                        WHERE vv.VehiculoVisitanteId = @id;"
                    : @"
                        SELECT TOP 1
                            v.VehiculoId AS Id,
                            v.Placa,
                            v.Tipo,
                            v.MarcaModelo,
                            v.Activo,
                            'UISEK' AS Origen,
                            (u.Nombres + ' ' + u.Apellidos) AS Propietario
                        FROM dbo.Vehiculos v
                        INNER JOIN dbo.Usuarios u ON v.UsuarioId = u.UsuarioId
                        WHERE v.VehiculoId = @id;";

                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);

                    using (var rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            model = new VehiculoAdminDetallesVM
                            {
                                Id = Convert.ToInt32(rd["Id"]),
                                Placa = rd["Placa"].ToString(),
                                Tipo = rd["Tipo"].ToString(),
                                MarcaModelo = rd["MarcaModelo"]?.ToString(),
                                Activo = Convert.ToBoolean(rd["Activo"]),
                                Origen = rd["Origen"].ToString(),
                                Propietario = rd["Propietario"]?.ToString()
                            };
                        }
                    }
                }
            }

            if (model == null) return HttpNotFound("No existe vehículo con id=" + id);

            return View(model);
        }

        // ==========================================================
        // ✅ EDITAR VEHÍCULO (POST)
        // ==========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VehiculoEditar(int id, string origen, string placa, string tipo, string marcaModelo, bool? activo)
        {
            var rol = (Session["Rol"] as string ?? "").Trim().ToUpper();
            if (rol != "ADMINISTRATIVO") return RedirectToAction("Login", "Auth");

            origen = (origen ?? "").Trim().ToUpper();
            placa = (placa ?? "").Trim().ToUpper();
            tipo = (tipo ?? "").Trim().ToUpper();
            marcaModelo = (marcaModelo ?? "").Trim();
            bool act = activo ?? false;

            if (string.IsNullOrWhiteSpace(placa) || string.IsNullOrWhiteSpace(tipo))
            {
                ViewBag.Error = "Placa y Tipo son obligatorios.";
                return View(new VehiculoAdminDetallesVM
                {
                    Id = id,
                    Origen = origen,
                    Placa = placa,
                    Tipo = tipo,
                    MarcaModelo = marcaModelo,
                    Activo = act
                });
            }

            using (var con = new SqlConnection(CS))
            {
                con.Open();

                string sql = (origen == "VISITANTE")
                    ? @"
                        UPDATE dbo.VehiculosVisitante
                        SET Placa = @placa,
                            Tipo = @tipo,
                            MarcaModelo = @marca,
                            Activo = @activo
                        WHERE VehiculoVisitanteId = @id;"
                    : @"
                        UPDATE dbo.Vehiculos
                        SET Placa = @placa,
                            Tipo = @tipo,
                            MarcaModelo = @marca,
                            Activo = @activo
                        WHERE VehiculoId = @id;";

                using (var cmd = new SqlCommand(sql, con))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@placa", placa);
                    cmd.Parameters.AddWithValue("@tipo", tipo);
                    cmd.Parameters.AddWithValue("@marca", (object)marcaModelo ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@activo", act);

                    cmd.ExecuteNonQuery();
                }
            }

            // ✅ vuelve a detalles del vehículo editado (no pierde el hilo)
            return RedirectToAction("VehiculoDetalles", "Administrativo", new { id = id });
        }
    }
}







