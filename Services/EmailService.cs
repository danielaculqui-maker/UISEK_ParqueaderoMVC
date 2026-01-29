using System;
using System.Configuration;
using System.Net;
using System.Net.Mail;

namespace UISEK_ParqueaderoMVC.Services
{
    public class EmailService
    {
        /// <summary>
        /// Envía correo. Si el destinatario es simulado (@uisekp.edu.ec),
        /// se redirige a un correo real de pruebas.
        /// </summary>
        public static void EnviarCorreo(string destinatario, string asunto, string mensajeHtml)
        {
            // 1) Leer configuración
            string correoPruebas = (ConfigurationManager.AppSettings["CorreoPruebas"] ?? "").Trim();
            string dominioSimulado = (ConfigurationManager.AppSettings["DominioSimulado"] ?? "@uisekp.edu.ec").Trim().ToLower();
            bool redirigir = (ConfigurationManager.AppSettings["RedirigirCorreosSimulados"] ?? "true")
                                .Trim().ToLower() == "true";

            // 2) Resolver destinatario final
            string destinoFinal = (destinatario ?? "").Trim();
            bool esSimulado = destinoFinal.ToLower().EndsWith(dominioSimulado);

            if (redirigir && esSimulado)
            {
                if (string.IsNullOrWhiteSpace(correoPruebas))
                    throw new Exception("Falta configurar CorreoPruebas en Web.config.");

                // Redirigir a correo real de pruebas
                destinoFinal = correoPruebas;

                // Anotar el original para evidencia académica
                mensajeHtml += "<br/><br/><hr/>" +
                               $"<small><b>Nota académica:</b> Destinatario simulado original: {destinatario}. " +
                               "Redirigido a correo real de pruebas para validación.</small>";
            }

            // 3) Construir correo
            MailMessage correo = new MailMessage();
            correo.From = new MailAddress("daniela.culqui.uelch@gmail.com"); // <-- TU GMAIL REAL (EL QUE ENVÍA)
            correo.To.Add(destinoFinal);
            correo.Subject = asunto;
            correo.Body = mensajeHtml;
            correo.IsBodyHtml = true;

            // 4) Enviar por SMTP Gmail
            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
            smtp.EnableSsl = true;  
            smtp.Credentials = new NetworkCredential(
                "daniela.culqui.uelch@gmail.com",
                "macc rtxu vefr cusa"
            );

            smtp.Send(correo);
        }
    }
}
