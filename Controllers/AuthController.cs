using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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
            // Validación de dominio institucional simulado
            if (string.IsNullOrWhiteSpace(correo) ||
                !correo.Trim().ToLower().EndsWith("@uisekp.edu.ec"))
            {
                ViewBag.ErrorLogin = "Debe usar un correo institucional simulado con dominio @uisekp.edu.ec";
                return View();
            }

            if (string.IsNullOrWhiteSpace(rol))
            {
                ViewBag.ErrorLogin = "Seleccione un rol.";
                return View();
            }

            Session["Correo"] = correo.Trim();
            Session["Rol"] = rol.Trim().ToUpper();
            Session["TieneDiscapacidad"] = tieneDiscapacidad ?? false;

            return RedirectToAction("Index", "Home");
        }

        // GET: /Auth/RegistroVisitante
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
    string correoContacto,
    string motivo,
    int duracionHoras,
    string finManual,
    string geoLat,
    string geoLng,
    string geoPrecision,
    string geoFuente
)

        {
            // Validación mínima (simulada)
            if (string.IsNullOrWhiteSpace(nombre) ||
                string.IsNullOrWhiteSpace(cedula) ||
                string.IsNullOrWhiteSpace(placa) ||
                string.IsNullOrWhiteSpace(motivo))
            {
                ViewBag.ErrorVisitante = "Completa Nombre, Cédula, Placa y Motivo.";
                return View();
            }

            // Limpieza
            placa = placa.Trim().ToUpper();
            nombre = nombre.Trim();
            cedula = cedula.Trim();
            correoContacto = (correoContacto ?? "").Trim();

            // Calcular inicio y fin
            DateTime inicio = DateTime.Now;
            DateTime fin;

            if (duracionHoras > 0)
            {
                fin = inicio.AddHours(duracionHoras);
            }
            else
            {
                // duracionHoras == 0 => se usa finManual
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

            // ✅ Validación suave de geolocalización (no bloquea, pero registra)
            // Si no llega nada, queda como NINGUNA (simulado)
            geoFuente = string.IsNullOrWhiteSpace(geoFuente) ? "NINGUNA" : geoFuente.Trim().ToUpper();

            // Crear sesión VISITANTE (rol automático)
            Session["Rol"] = "VISITANTE";
            Session["Correo"] = $"visitante_{cedula}@visitante.uisekp.edu.ec"; // identificador simulado
            Session["TieneDiscapacidad"] = false;

            // Datos del visitante
            Session["NombreVisitante"] = nombre;
            Session["CedulaVisitante"] = cedula;
            Session["PlacaVisitante"] = placa;
            Session["CorreoContactoVisitante"] = correoContacto;
            Session["MotivoVisitante"] = motivo;

            Session["InicioVisita"] = inicio.ToString("yyyy-MM-dd HH:mm");
            Session["FinVisita"] = fin.ToString("yyyy-MM-dd HH:mm");

            // ✅ Geolocalización (para sugerencias y auditoría simulada)
            Session["GeoLat"] = (geoLat ?? "").Trim();
            Session["GeoLng"] = (geoLng ?? "").Trim();
            Session["GeoPrecision"] = (geoPrecision ?? "").Trim();
            Session["GeoFuente"] = geoFuente;

            // Siguiente paso (luego lo haremos): seleccionar parqueadero
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