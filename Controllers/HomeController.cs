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
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace UISEK_ParqueaderoMVC.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            // Si no hay sesión, manda al login
            if (Session["Rol"] == null)
                return RedirectToAction("Login", "Auth");

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}