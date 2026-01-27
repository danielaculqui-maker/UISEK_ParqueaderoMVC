using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace UISEK_ParqueaderoMVC.Controllers
{
    public class AuthController : Controller
    {
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

            // ✅ Validación dominio institucional
            if (string.IsNullOrWhiteSpace(correo) || !correo.EndsWith("@uisekp.edu.ec"))
            {
                ViewBag.ErrorLogin = "Debe usar un correo institucional simulado con dominio @uisekp.edu.ec";
                return View();
            }

            if (string.IsNullOrWhiteSpace(rol))
            {
                ViewBag.ErrorLogin = "Seleccione un rol.";
                return View();
            }

            // ✅ Guardar en BD (Enfoque fuerte)
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["UISEK_ParqueaderoDB"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connStr))
                using (SqlCommand cmd = new SqlCommand("dbo.sp_LoginUpsertUsuario", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@correo", correo);
                    cmd.Parameters.AddWithValue("@rol", rol);
                    cmd.Parameters.AddWithValue("@tieneDiscapacidad", discapacidad);

                    conn.Open();

                    // El SP devuelve una fila con Ok y Mensaje (SELECT)
                    using (var rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            int ok = Convert.ToInt32(rd["Ok"]);
                            string msg = Convert.ToString(rd["Mensaje"]);

                            if (ok != 1)
                            {
                                ViewBag.ErrorLogin = msg;
                                return View();
                            }
                        }
                        else
                        {
                            ViewBag.ErrorLogin = "No se pudo validar el login en la base de datos.";
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

            // ✅ Sesión
            Session["Correo"] = correo;
            Session["Rol"] = rol;
            Session["TieneDiscapacidad"] = discapacidad;

            // ✅ Redirección por rol
            switch (rol)
            {
                case "ESTUDIANTE":
                    return RedirectToAction("Index", "Estudiante");
                case "DOCENTE":
                    return RedirectToAction("Index", "Docente");
                case "ADMINISTRATIVO":
                    return RedirectToAction("Index", "Administrativo");
                case "GUARDIA":
                    return RedirectToAction("Index", "Guardia");
                default:
                    // Si llega algo raro
                    Session.Clear();
                    ViewBag.ErrorLogin = "Rol no válido.";
                    return View();
            }
        }

        // GET: /Auth/RegistroVisitante
        [HttpGet]
        public ActionResult RegistroVisitante()
        {
            return View();
        }

        // POST: /Auth/RegistroVisitante ✅ SIN GEO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistroVisitante(
            string nombre,
            string cedula,
            string placa,
            string correoContacto,
            string motivo,
            int duracionHoras,
            string finManual
        )
        {
            if (string.IsNullOrWhiteSpace(nombre) ||
                string.IsNullOrWhiteSpace(cedula) ||
                string.IsNullOrWhiteSpace(placa) ||
                string.IsNullOrWhiteSpace(motivo))
            {
                ViewBag.ErrorVisitante = "Completa Nombre, Cédula, Placa y Motivo.";
                return View();
            }

            placa = placa.Trim().ToUpper();
            nombre = nombre.Trim();
            cedula = cedula.Trim();
            correoContacto = (correoContacto ?? "").Trim();

            DateTime inicio = DateTime.Now;
            DateTime fin;

            if (duracionHoras > 0)
            {
                fin = inicio.AddHours(duracionHoras);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(finManual) || !DateTime.TryParse(finManual, out fin))
                {
                    ViewBag.ErrorVisitante = "Si eliges “Otro”, debes ingresar Fecha y hora fin.";
                    return View();
                }

                if (fin <= inicio)
                {
                    ViewBag.ErrorVisitante = "La fecha/hora fin debe ser mayor a la hora actual.";
                    return View();
                }
            }

            // Crear sesión VISITANTE (rol automático)
            Session["Rol"] = "VISITANTE";
            Session["Correo"] = $"visitante_{cedula}@visitante.uisekp.edu.ec";
            Session["TieneDiscapacidad"] = false;

            // Datos del visitante
            Session["NombreVisitante"] = nombre;
            Session["CedulaVisitante"] = cedula;
            Session["PlacaVisitante"] = placa;
            Session["CorreoContactoVisitante"] = correoContacto;
            Session["MotivoVisitante"] = motivo;

            Session["InicioVisita"] = inicio.ToString("yyyy-MM-dd HH:mm");
            Session["FinVisita"] = fin.ToString("yyyy-MM-dd HH:mm");

            // ✅ DIRECTO A SELECCIONAR PARQUEADERO (foto)
            return RedirectToAction("Seleccionar", "Parqueaderos");
        }

        // GET: /Auth/Logout
        [HttpGet]
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
