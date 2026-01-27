using System.Web.Mvc;

namespace UISEK_ParqueaderoMVC.Controllers
{
    public class AccesibilidadController : Controller
    {
        [HttpGet]
        public ActionResult Oscuro()
        {
            bool actual = (Session["ModoOscuro"] as bool?) ?? false;
            Session["ModoOscuro"] = !actual;
            return Redirect(Request.UrlReferrer?.ToString() ?? Url.Action("Index", "Home"));
        }

        [HttpGet]
        public ActionResult Dislexia()
        {
            bool actual = (Session["ModoDislexia"] as bool?) ?? false;
            Session["ModoDislexia"] = !actual;
            return Redirect(Request.UrlReferrer?.ToString() ?? Url.Action("Index", "Home"));
        }

        [HttpGet]
        public ActionResult Contraste()
        {
            bool actual = (Session["AltoContraste"] as bool?) ?? false;
            Session["AltoContraste"] = !actual;
            return Redirect(Request.UrlReferrer?.ToString() ?? Url.Action("Index", "Home"));
        }
    }
}
