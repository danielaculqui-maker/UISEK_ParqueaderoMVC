using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace UISEK_ParqueaderoMVC.Controllers
{
    public class AccesibilidadController :  Controller
    {
        [HttpPost]
        public ActionResult ToggleDislexia()
        {
            bool actual = (Session["ModoDislexia"] as bool?) ?? false;
            Session["ModoDislexia"] = !actual;
            return Json(new { ok = true, value = !actual });
        }

        [HttpPost]
        public ActionResult ToggleOscuro()
        {
            bool actual = (Session["ModoOscuro"] as bool?) ?? false;
            Session["ModoOscuro"] = !actual;
            return Json(new { ok = true, value = !actual });
        }

        [HttpPost]
        public ActionResult ToggleAltoContraste()
        {
            bool actual = (Session["AltoContraste"] as bool?) ?? false;
            Session["AltoContraste"] = !actual;
            return Json(new { ok = true, value = !actual });
        }
    }
}