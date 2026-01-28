using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;      
using System.Web.Mvc;


namespace UISEK_ParqueaderoMVC.Controllers
{
    public class AuthController : Controller
    {
        /* ============================
           LOGIN INSTITUCIONAL
           ============================ */

        // GET: /Auth/Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string correo, string rol, bool? tieneDiscapacidad)
        {
            correo = (correo ?? "").Trim().ToLower();
            rol = (rol ?? "").Trim().ToUpper();
            bool discapacidad = tieneDiscapacidad ?? false;

            if (string.IsNullOrWhiteSpace(correo) || !correo.EndsWith("@uisekp.edu.ec"))
            {
                ViewBag.ErrorLogin = "Debe usar un correo institucional con dominio @uisekp.edu.ec";
                return View();
            }

            if (string.IsNullOrWhiteSpace(rol))
            {
                ViewBag.ErrorLogin = "Seleccione un rol.";
                return View();
            }

            try
            {
                string connStr = ConfigurationManager
                    .ConnectionStrings["UISEK_ParqueaderoDB"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connStr))
                using (SqlCommand cmd = new SqlCommand("dbo.sp_LoginUpsertUsuario", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@correo", correo);
                    cmd.Parameters.AddWithValue("@rol", rol);
                    cmd.Parameters.AddWithValue("@tieneDiscapacidad", discapacidad);

                    conn.Open();

                    using (var rd = cmd.ExecuteReader())
                    {
                        if (!rd.Read() || Convert.ToInt32(rd["Ok"]) != 1)
                        {
                            ViewBag.ErrorLogin = rd["Mensaje"].ToString();
                            return View();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorLogin = "Error BD: " + ex.Message;
                return View();
            }

            Session["Correo"] = correo;
            Session["Rol"] = rol;
            Session["TieneDiscapacidad"] = discapacidad;

            switch (rol)
            {
                case "ESTUDIANTE": return RedirectToAction("MiVehiculo", "Estudiante");
                case "DOCENTE": return RedirectToAction("Index", "Docente");
                case "ADMINISTRATIVO": return RedirectToAction("Index", "Administrativo");
                case "GUARDIA": return RedirectToAction("Index", "Guardia");
                default:
                    Session.Clear();
                    ViewBag.ErrorLogin = "Rol no válido.";
                    return View();
            }
        }

        // POST: /Auth/RegistroVisitante
        [HttpGet]
        public ActionResult RegistroVisitante()
        {
            return View();
        }

        // POST: /Auth/RegistroVisitante
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistroVisitante(
            string nombre,
            string cedula,
            string placa,
            string tipoVehiculo,   // ✅ NUEVO
            string marcaModelo,    // ✅ NUEVO
            string correoContacto,
            string motivo,
            int duracionHoras,
            string finManual,
            bool? visitanteDiscapacidad
        )
        {
            // 0) Validaciones mínimas
            if (string.IsNullOrWhiteSpace(nombre) ||
                string.IsNullOrWhiteSpace(cedula) ||
                string.IsNullOrWhiteSpace(placa) ||
                string.IsNullOrWhiteSpace(motivo))
            {
                ViewBag.ErrorVisitante = "Completa Nombre, Cédula, Placa y Motivo.";
                return View("RegistroVisitante");
            }

            placa = placa.Trim().ToUpper();
            nombre = nombre.Trim();
            cedula = cedula.Trim();
            motivo = motivo.Trim();
            correoContacto = (correoContacto ?? "").Trim();

            // ✅ Normalizar tipo + marca
            tipoVehiculo = (tipoVehiculo ?? "AUTO").Trim().ToUpper();
            marcaModelo = (marcaModelo ?? "").Trim();

            // ✅ Validar tipo permitido
            string[] tiposValidos = { "AUTO", "MOTO", "FURGONETA", "ELECTRICO" };
            if (!tiposValidos.Contains(tipoVehiculo))
                tipoVehiculo = "AUTO";

            try
            {
                string connStr = ConfigurationManager
                    .ConnectionStrings["UISEK_ParqueaderoDB"].ConnectionString;

                using (SqlConnection cn = new SqlConnection(connStr))
                {
                    cn.Open();

                    // 1️⃣ Registrar visitante (Upsert por cédula)
                    using (SqlCommand cmd = new SqlCommand("dbo.sp_RegistrarVisitante", cn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@nombre", nombre);
                        cmd.Parameters.AddWithValue("@cedula", cedula);
                        cmd.Parameters.AddWithValue("@placa", placa);

                        // ✅ NUEVOS PARAMS
                        cmd.Parameters.AddWithValue("@tipo", tipoVehiculo);
                        cmd.Parameters.AddWithValue("@marcaModelo",
                            string.IsNullOrWhiteSpace(marcaModelo) ? (object)DBNull.Value : marcaModelo);

                        cmd.Parameters.AddWithValue("@correoContacto",
                            string.IsNullOrWhiteSpace(correoContacto) ? (object)DBNull.Value : correoContacto);

                        cmd.Parameters.AddWithValue("@tieneDiscapacidad", visitanteDiscapacidad ?? false);

                        using (var rd = cmd.ExecuteReader())
                        {
                            if (!rd.Read())
                            {
                                ViewBag.ErrorVisitante = "No se recibió respuesta del SP sp_RegistrarVisitante.";
                                return View("RegistroVisitante");
                            }

                            // Espera columnas: Ok (int) y Mensaje (string)
                            int ok = 0;
                            if (rd["Ok"] != DBNull.Value) ok = Convert.ToInt32(rd["Ok"]);

                            if (ok != 1)
                            {
                                ViewBag.ErrorVisitante = (rd["Mensaje"] ?? "No se pudo registrar.").ToString();
                                return View("RegistroVisitante");
                            }
                        }
                    }

                    // 2️⃣ Registrar INGRESO (sensor simulado)
                    using (SqlCommand cmdIng = new SqlCommand("dbo.sp_RegistrarIngresoVisitante", cn))
                    {
                        cmdIng.CommandType = CommandType.StoredProcedure;
                        cmdIng.Parameters.AddWithValue("@placa", placa);
                        cmdIng.Parameters.AddWithValue("@fuente", "SIMULADO");
                        cmdIng.Parameters.AddWithValue("@observacion", "Sensor entrada (visitante)");
                        cmdIng.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorVisitante = "Error BD: " + ex.Message;
                return View("RegistroVisitante");
            }

            // 3️⃣ Guardar datos para la confirmación
            Session["Rol"] = "VISITANTE";
            Session["NombreVisitante"] = nombre;
            Session["CedulaVisitante"] = cedula;
            Session["PlacaVisitante"] = placa;
            Session["MotivoVisitante"] = motivo;

            // ✅ NUEVO
            Session["TipoVehiculoVisitante"] = tipoVehiculo;
            Session["MarcaModeloVisitante"] = marcaModelo;

            // 4️⃣ Pantalla de confirmación
            return RedirectToAction("ConfirmacionVisitante", "Auth");
        }


        /* ============================
           CONFIRMACIÓN VISITANTE
           ============================ */

        // GET: /Auth/ConfirmacionVisitante
        [HttpGet]
        public ActionResult ConfirmacionVisitante()
        {
            if (Session["PlacaVisitante"] == null)
                return RedirectToAction("Login");

            return View();
        }

        /* ============================
           LOGOUT
           ============================ */

        // GET: /Auth/Logout
        [HttpGet]
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
