using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace UISEK_ParqueaderoMVC.Controllers
{
    public class BaseController : Controller
    {
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            try
            {
                string cs = ConfigurationManager
                    .ConnectionStrings["UISEK_ParqueaderoDB"]
                    .ConnectionString;

                using (var con = new SqlConnection(cs))
                using (var cmd = new SqlCommand(@"
                    SELECT TOP 1 Valor
                    FROM dbo.SistemaConfig
                    WHERE Clave = 'SCRIPT_ACTIVO';
                ", con))
                {
                    con.Open();
                    var val = cmd.ExecuteScalar();

                    bool activo =
                        val != null &&
                        val != DBNull.Value &&
                        (val.ToString() == "1" ||
                         val.ToString().ToLower() == "true");

                    ViewBag.ScriptActivo = activo;
                }
            }
            catch
            {
                // fallback seguro
                ViewBag.ScriptActivo = true;
            }
        }
    }
}
