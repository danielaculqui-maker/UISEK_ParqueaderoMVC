using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using UISEK_ParqueaderoMVC.Models;


namespace UISEK_ParqueaderoMVC.DAL
{

    public class PanelDAL

    {
        public class Resp
        {
            public bool Ok { get; set; }
            public string Mensaje { get; set; }
        }

        public Resp GuardarVehiculoInstitucional(string correo, string tipo, string placa, string marcaModelo)
        {
            using (var cn = new SqlConnection(_cnn))
            using (var cmd = new SqlCommand("dbo.sp_UpsertVehiculoInstitucional", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@correo", correo ?? "");
                cmd.Parameters.AddWithValue("@placa", placa ?? "");
                cmd.Parameters.AddWithValue("@tipo", tipo ?? "AUTO");
                cmd.Parameters.AddWithValue("@marcaModelo", (object)(marcaModelo ?? (string)null) ?? DBNull.Value);

                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return new Resp { Ok = false, Mensaje = "Sin respuesta del SP." };

                    var ok = rd["Ok"] != DBNull.Value && Convert.ToInt32(rd["Ok"]) == 1;
                    var msg = rd["Mensaje"]?.ToString();

                    return new Resp { Ok = ok, Mensaje = msg };
                }
            }
        }

        private readonly string _cnn =
            ConfigurationManager.ConnectionStrings["UISEK_ParqueaderoDB"].ConnectionString;

        public PanelUsuarioVM ObtenerPanel(string correo)
        {
            using (var cn = new SqlConnection(_cnn))
            using (var cmd = new SqlCommand("dbo.sp_ObtenerPanelUsuario", cn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@correo", correo ?? "");

                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return null;

                    return new PanelUsuarioVM
                    {
                        UsuarioId = Convert.ToInt32(rd["UsuarioId"]),
                        Correo = rd["Correo"].ToString(),
                        Nombres = rd["Nombres"]?.ToString(),
                        Apellidos = rd["Apellidos"]?.ToString(),
                        NombreCompleto = rd["NombreCompleto"]?.ToString(),
                        Cedula = rd["Cedula"]?.ToString(),
                        Rol = rd["Rol"]?.ToString(),

                        EstadoActivo = Convert.ToBoolean(rd["EstadoActivo"]),
                        PermisoVigente = Convert.ToBoolean(rd["PermisoVigente"]),
                        TieneDiscapacidad = Convert.ToBoolean(rd["TieneDiscapacidad"]),

                        AreaDependencia = rd["AreaDependencia"]?.ToString(),
                        GaritaAsignada = rd["GaritaAsignada"]?.ToString(),
                        Turno = rd["Turno"]?.ToString(),

                        TipoVehiculo = rd["TipoVehiculo"]?.ToString(),
                        Placa = rd["Placa"]?.ToString(),
                        MarcaModelo = rd["MarcaModelo"]?.ToString()
                    };
                }
            }
        }
    }
}
