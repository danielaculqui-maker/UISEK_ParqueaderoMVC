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
using System.Web.Mvc;

namespace UISEK_ParqueaderoMVC.Controllers
{
    public class AccesibilidadController : Controller
    {
        private ActionResult Volver()
        {
            return Redirect(
                Request.UrlReferrer?.ToString()
                ?? Url.Action("Index", "Estudiante")
            );
        }

        // ☀ MODO CLARO (reset total)
        [HttpGet]
        public ActionResult Claro()
        {
            Session["ModoOscuro"] = false;
            Session["AltoContraste"] = false;
            Session["ModoDislexia"] = false;
            return Volver();
        }

        // 🌙 MODO OSCURO
        [HttpGet]
        public ActionResult Oscuro()
        {
            bool actual = (Session["ModoOscuro"] as bool?) ?? false;
            bool nuevo = !actual;

            Session["ModoOscuro"] = nuevo;

            // si activo oscuro, apago contraste
            if (nuevo)
                Session["AltoContraste"] = false;

            return Volver();
        }

        // 🌓 ALTO CONTRASTE
        [HttpGet]
        public ActionResult Contraste()
        {
            bool actual = (Session["AltoContraste"] as bool?) ?? false;
            bool nuevo = !actual;

            Session["AltoContraste"] = nuevo;

            // si activo contraste, apago oscuro
            if (nuevo)
                Session["ModoOscuro"] = false;

            return Volver();
        }

        // 📖 DISLEXIA (independiente)
        [HttpGet]
        public ActionResult Dislexia()
        {
            bool actual = (Session["ModoDislexia"] as bool?) ?? false;
            Session["ModoDislexia"] = !actual;
            return Volver();
        }
    }
}
