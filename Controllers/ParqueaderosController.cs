using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace UISEK_ParqueaderoMVC.Controllers
{
    public class ParqueaderosController : Controller
    {
        // GET: /Parqueaderos/Seleccionar
        public ActionResult Seleccionar()
        {
            // si no hay sesión, regresa a login
            if (Session["Rol"] == null) return RedirectToAction("Login", "Auth");

            return View();
        }

        // POST: /Parqueaderos/GuardarSeleccion
        [HttpPost]
        public ActionResult GuardarSeleccion(string espacioId)
        {
            // Guardar selección simulada
            Session["EspacioSeleccionado"] = espacioId;

            // luego lo mandas a Inicio (o Confirmación)
            return RedirectToAction("Index", "Home");
        }
}
}