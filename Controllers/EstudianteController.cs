using System.Web.Mvc;
using UISEK_ParqueaderoMVC.DAL;

namespace UISEK_ParqueaderoMVC.Controllers
{
    public class EstudianteController : Controller
    {
        private readonly PanelDAL _dal = new PanelDAL();

        public ActionResult Index()
        {
            var correo = (string)Session["Correo"];
            var rol = (string)Session["Rol"];

            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(rol))
                return RedirectToAction("Login", "Auth");

            if (rol != "ESTUDIANTE")
                return RedirectToAction("Index", "Home");

            var vm = _dal.ObtenerPanel(correo);
            return View(vm);
        }

        [HttpGet]
        public ActionResult MiVehiculo()
        {
            var correo = (string)Session["Correo"];
            var rol = (string)Session["Rol"];

            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(rol))
                return RedirectToAction("Login", "Auth");

            if (rol != "ESTUDIANTE")
                return RedirectToAction("Index", "Home");

            var vm = _dal.ObtenerPanel(correo);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MiVehiculo(string tipoVehiculo, string placa, string marcaModelo)
        {
            var correo = (string)Session["Correo"];
            var rol = (string)Session["Rol"];

            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(rol))
                return RedirectToAction("Login", "Auth");

            if (rol != "ESTUDIANTE")
                return RedirectToAction("Index", "Home");

            // Guardar en BD
            var resp = _dal.GuardarVehiculoInstitucional(correo, tipoVehiculo, placa, marcaModelo);

            TempData["MsgOk"] = resp.Ok ? resp.Mensaje : null;
            TempData["MsgError"] = resp.Ok ? null : resp.Mensaje;

            return RedirectToAction("MiVehiculo");
        }
    }
}
