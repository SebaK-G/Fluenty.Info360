using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
namespace Fluenty.Info360.Models;

public class BD
{
    private string _connectionString = @"Server=localhost;DataBase=Fluenty;Integrated Security=True;TrustServerCertificate=True;";

    public Usuarios Login(string email, string contrasena){
        Usuarios usuario = null;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string query = "SELECT * FROM Usuarios WHERE Email = @pEmail AND Contrasena = @pContrasena";
            usuario = connection.QueryFirstOrDefault<Usuarios>(query, new { pEmail = email, pContrasena = contrasena });
        }
        return usuario;
    }

    public string ObtenerNombreEmpresaPorId(int idEmpresa){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string query = "SELECT Nombre FROM Empresas WHERE IdEmpresa = @pIdEmpresa";
            return connection.QueryFirstOrDefault<string>(query, new { pIdEmpresa = idEmpresa });
        }
    }

    public int? ObtenerIdEmpresaPorNombre(string nombreEmpresa){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string query = "SELECT IdEmpresa FROM Empresas WHERE Nombre = @pNombre";
            return connection.QueryFirstOrDefault<int?>(query, new { pNombre = nombreEmpresa.Trim() });
        }
    }

    public bool ExisteUsuarioDuplicado(string email, string dni, string telefono){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string query = @"SELECT TOP 1 IdUsuario FROM Usuarios WHERE Email = @pEmail OR Dni = @pDni OR Telefono = @pTelefono";
            int? usuario = connection.QueryFirstOrDefault<int?>(query, new { pEmail = email, pDni = dni, pTelefono = telefono });
            return usuario.HasValue;
        }
    }

       public void Signup(Usuarios usuario){
        string query = "INSERT INTO Usuarios (Nombre, Apellido, IdEmpresa, Email, IdEmpleador, IdEmpleado, Dni, Telefono, Contrasena) VALUES (@pNombre, @pApellido, @pIdEmpresa, @pEmail, @pIdEmpleador, @pIdEmpleado, @pDni, @pTelefono, @pContrasena)";
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            connection.Execute(query, new { pNombre = usuario.Nombre, pApellido = usuario.Apellido, pIdEmpresa = usuario.IdEmpresa, pEmail = usuario.Email, pIdEmpleador = usuario.IdEmpleador, pIdEmpleado = usuario.IdEmpleado, pDni = usuario.Dni, pTelefono = usuario.Telefono, pContrasena = usuario.Contrasena });
        }
    }

    public int CrearEmpresa(Empresas empresa){
        int idEmpresa = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string query = "INSERT INTO Empresas (Nombre, Email, CantidadEmpleados, Cuit) VALUES (@pNombre, @pEmail, @pCantidadEmpleados, @pCuit); SELECT CAST(SCOPE_IDENTITY() as int)";
            idEmpresa = connection.QuerySingle<int>(query, new { pNombre = empresa.Nombre, pEmail = empresa.Email, pCantidadEmpleados = empresa.CantidadEmpleados, pCuit = empresa.Cuit });
        }
        return idEmpresa;
    }

    public int CrearEmpleado(Empleados empleado){
        int idEmpleado = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string query = "INSERT INTO Empleados (Puesto, Estado) VALUES (@pPuesto, @pEstado); SELECT CAST(SCOPE_IDENTITY() as int)";
            idEmpleado = connection.QuerySingle<int>(query, new { pPuesto = empleado.Puesto, pEstado = empleado.Estado });
        }
        return idEmpleado;
    }

    public int CrearEmpleador(Empleadores empleador){
        int idEmpleador = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string query = "INSERT INTO Empleadores (Cargo, Estado) VALUES (@pCargo, @pEstado); SELECT CAST(SCOPE_IDENTITY() as int)";
            idEmpleador = connection.QuerySingle<int>(query, new { pCargo = empleador.Cargo, pEstado = empleador.Estado });
        }
        return idEmpleador;
    }

    public void ActualizarUsuarioTipo(string email, int? idEmpleado, int? idEmpleador){
        string query = @"UPDATE Usuarios SET IdEmpleado = @pIdEmpleado, IdEmpleador = @pIdEmpleador WHERE Email = @pEmail";
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            connection.Execute(query, new {
                pIdEmpleado = idEmpleado,
                pIdEmpleador = idEmpleador,
                pEmail = email
            });
        }
    }
        public void CrearInvitacion(Invitaciones invitacion){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_CrearInvitacion";
            connection.Execute(storedProcedure, new { IdEmpresa = invitacion.IdEmpresa, Nombre = invitacion.Nombre, Apellido = invitacion.Apellido, Email = invitacion.Email, Puesto = invitacion.Puesto, InfoEmpleador = invitacion.InfoEmpleador, Codigo = invitacion.Codigo }, commandType: CommandType.StoredProcedure);
        }
    }

    public Invitaciones TraerInvitacionPorCodigo(string codigo){
        Invitaciones invitacion = null;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerInvitacionPorCodigo";
            invitacion = connection.QueryFirstOrDefault<Invitaciones>(storedProcedure, new { Codigo = codigo }, commandType: CommandType.StoredProcedure);
        }
        return invitacion;
    }

    public List<Invitaciones> TraerInvitacionesEmpresa(int idEmpresa){
        List<Invitaciones> invitaciones = new List<Invitaciones>();
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerInvitacionesEmpresa";
            invitaciones = connection.Query<Invitaciones>(storedProcedure, new { IdEmpresa = idEmpresa }, commandType: CommandType.StoredProcedure).ToList();
        }
        return invitaciones;
    }

    public void CompletarInvitacion(string codigo, int idEmpleado){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_CompletarInvitacion";
            connection.Execute(storedProcedure, new { Codigo = codigo, IdEmpleado = idEmpleado }, commandType: CommandType.StoredProcedure);
        }
    }

    public void AgregarFormularioEmpleado(FormulariosEmpleado formulario){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_AgregarFormularioEmpleado";
            connection.Execute(storedProcedure, new { IdEmpleado = formulario.IdEmpleado, NivelIA = formulario.NivelIA, Tareas = formulario.Tareas, Herramientas = formulario.Herramientas, Dificultades = formulario.Dificultades, Objetivos = formulario.Objetivos, TiempoXDia = formulario.TiempoXDia }, commandType: CommandType.StoredProcedure);
        }
    }
        public string TraerNombreEmpresa(int idEmpresa){
        string nombre = "";
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerNombreEmpresa";
            nombre = connection.QueryFirstOrDefault<string>(storedProcedure, new { IdEmpresa = idEmpresa }, commandType: CommandType.StoredProcedure);
        }
        return nombre;
    }

    public int ContarEmpleadosEmpresa(int idEmpresa){
        int cantidad = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_ContarEmpleadosEmpresa";
            cantidad = connection.QueryFirstOrDefault<int>(storedProcedure, new { IdEmpresa = idEmpresa }, commandType: CommandType.StoredProcedure);
        }
        return cantidad;
    }

    public int ContarCursosEmpresa(int idEmpresa){
        int cantidad = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_ContarCursosEmpresa";
            cantidad = connection.QueryFirstOrDefault<int>(storedProcedure, new { IdEmpresa = idEmpresa }, commandType: CommandType.StoredProcedure);
        }
        return cantidad;
    }

    public int TraerMesesRestantesPlan(){
        int meses = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerMesesRestantesPlan";
            meses = connection.QueryFirstOrDefault<int>(storedProcedure, commandType: CommandType.StoredProcedure);
        }
        return meses;
    }

    public int TraerPorcentajeAhorro(){
        int porcentaje = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerPorcentajeAhorro";
            porcentaje = connection.QueryFirstOrDefault<int>(storedProcedure, commandType: CommandType.StoredProcedure);
        }
        return porcentaje;
    }

    public string TraerEstadoInvitacionPendiente(){
        string estado = "";
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerEstadoInvitacionPendiente";
            estado = connection.QueryFirstOrDefault<string>(storedProcedure, commandType: CommandType.StoredProcedure);
        }
        return estado;
    }
}