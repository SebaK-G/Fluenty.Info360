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

    // Crea el usuario (empleador o empleado) y devuelve su IdUsuario
    public int Signup(Usuarios usuario){
        int idUsuario = 0;
        string query = "INSERT INTO Usuarios (Nombre, Apellido, IdEmpresa, Email, Dni, Telefono, Contrasena, Rol, Puesto) VALUES (@pNombre, @pApellido, @pIdEmpresa, @pEmail, @pDni, @pTelefono, @pContrasena, @pRol, @pPuesto); SELECT CAST(SCOPE_IDENTITY() as int)";
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            idUsuario = connection.QuerySingle<int>(query, new { pNombre = usuario.Nombre, pApellido = usuario.Apellido, pIdEmpresa = usuario.IdEmpresa, pEmail = usuario.Email, pDni = usuario.Dni, pTelefono = usuario.Telefono, pContrasena = usuario.Contrasena, pRol = usuario.Rol, pPuesto = usuario.Puesto });
        }
        return idUsuario;
    }

    public int CrearEmpresa(Empresas empresa){
        int idEmpresa = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string query = "INSERT INTO Empresas (Nombre, Email, CantidadEmpleados, Cuit) VALUES (@pNombre, @pEmail, @pCantidadEmpleados, @pCuit); SELECT CAST(SCOPE_IDENTITY() as int)";
            idEmpresa = connection.QuerySingle<int>(query, new { pNombre = empresa.Nombre, pEmail = empresa.Email, pCantidadEmpleados = empresa.CantidadEmpleados, pCuit = empresa.Cuit });
        }
        return idEmpresa;
    }

    public void AgregarFormularioEmpleado(FormulariosEmpleado formulario){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_AgregarFormularioEmpleado";
            connection.Execute(storedProcedure, new { IdUsuario = formulario.IdUsuario, NivelIA = formulario.NivelIA, Tareas = formulario.Tareas, Herramientas = formulario.Herramientas, Dificultades = formulario.Dificultades, Objetivos = formulario.Objetivos, TiempoXDia = formulario.TiempoXDia, DificultadPrincipal = formulario.DificultadPrincipal, MejoraEmpresa = formulario.MejoraEmpresa, FortalezasDebilidades = formulario.FortalezasDebilidades, MejoraPersonal = formulario.MejoraPersonal }, commandType: CommandType.StoredProcedure);
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

        // ===================== CURSOS =====================

    public List<EmpleadoCurso> TraerEmpleadosConCurso(int idEmpresa){
        List<EmpleadoCurso> lista = new List<EmpleadoCurso>();
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerEmpleadosConCurso";
            lista = connection.Query<EmpleadoCurso>(storedProcedure, new { IdEmpresa = idEmpresa }, commandType: CommandType.StoredProcedure).ToList();
        }
        return lista;
    }

    public Usuarios TraerEmpleado(int idUsuario){
        Usuarios empleado = null;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerEmpleado";
            empleado = connection.QueryFirstOrDefault<Usuarios>(storedProcedure, new { IdUsuario = idUsuario }, commandType: CommandType.StoredProcedure);
        }
        return empleado;
    }

    public FormulariosEmpleado TraerFormularioEmpleado(int idUsuario){
        FormulariosEmpleado formulario = null;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerFormularioEmpleado";
            formulario = connection.QueryFirstOrDefault<FormulariosEmpleado>(storedProcedure, new { IdUsuario = idUsuario }, commandType: CommandType.StoredProcedure);
        }
        return formulario;
    }

    public FormulariosEmpleador TraerFormularioEmpleadorEmpresa(int idEmpresa){
        FormulariosEmpleador formulario = null;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerFormularioEmpleadorEmpresa";
            formulario = connection.QueryFirstOrDefault<FormulariosEmpleador>(storedProcedure, new { IdEmpresa = idEmpresa }, commandType: CommandType.StoredProcedure);
        }
        return formulario;
    }

    public int CrearCurso(Cursos curso){
        int idCurso = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_CrearCurso";
            idCurso = connection.QueryFirstOrDefault<int>(storedProcedure, new { IdUsuario = curso.IdUsuario, Titulo = curso.Titulo, Descripcion = curso.Descripcion, Modelo = curso.Modelo, Nivel = curso.Nivel, Objetivos = curso.Objetivos, UnidadInicial = curso.UnidadInicial, MotivoNivel = curso.MotivoNivel }, commandType: CommandType.StoredProcedure);
        }
        return idCurso;
    }

    public void AgregarParteCurso(PartesCurso parte){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_AgregarParteCurso";
            connection.Execute(storedProcedure, new { IdCurso = parte.IdCurso, Numero = parte.Numero, Titulo = parte.Titulo, Contenido = parte.Contenido }, commandType: CommandType.StoredProcedure);
        }
    }

    public void AgregarPregunta(Preguntas pregunta){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_AgregarPregunta";
            connection.Execute(storedProcedure, new { IdCurso = pregunta.IdCurso, NumeroParte = pregunta.NumeroParte, Texto = pregunta.Texto, OpcionA = pregunta.OpcionA, OpcionB = pregunta.OpcionB, OpcionC = pregunta.OpcionC, Correcta = pregunta.Correcta }, commandType: CommandType.StoredProcedure);
        }
    }

    public Cursos TraerCurso(int idCurso){
        Cursos curso = null;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerCurso";
            curso = connection.QueryFirstOrDefault<Cursos>(storedProcedure, new { IdCurso = idCurso }, commandType: CommandType.StoredProcedure);
        }
        return curso;
    }

    public Cursos TraerCursoPorEmpleado(int idUsuario){
        Cursos curso = null;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerCursoPorEmpleado";
            curso = connection.QueryFirstOrDefault<Cursos>(storedProcedure, new { IdUsuario = idUsuario }, commandType: CommandType.StoredProcedure);
        }
        return curso;
    }

    public List<PartesCurso> TraerPartesCurso(int idCurso){
        List<PartesCurso> partes = new List<PartesCurso>();
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerPartesCurso";
            partes = connection.Query<PartesCurso>(storedProcedure, new { IdCurso = idCurso }, commandType: CommandType.StoredProcedure).ToList();
        }
        return partes;
    }

    public List<Preguntas> TraerPreguntasCurso(int idCurso){
        List<Preguntas> preguntas = new List<Preguntas>();
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerPreguntasCurso";
            preguntas = connection.Query<Preguntas>(storedProcedure, new { IdCurso = idCurso }, commandType: CommandType.StoredProcedure).ToList();
        }
        return preguntas;
    }

    public void ActualizarProgreso(int idCurso, int progreso){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_ActualizarProgreso";
            connection.Execute(storedProcedure, new { IdCurso = idCurso, Progreso = progreso }, commandType: CommandType.StoredProcedure);
        }
    }

    // ===================== NUEVO FLUJO: link, formulario del empleador y confirmación =====================

    public string TraerCodigoEmpresa(int idEmpresa){
        string codigo = "";
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerCodigoEmpresa";
            codigo = connection.QueryFirstOrDefault<string>(storedProcedure, new { IdEmpresa = idEmpresa }, commandType: CommandType.StoredProcedure);
        }
        return codigo;
    }

    public void GuardarCodigoEmpresa(int idEmpresa, string codigo){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_GuardarCodigoEmpresa";
            connection.Execute(storedProcedure, new { IdEmpresa = idEmpresa, Codigo = codigo }, commandType: CommandType.StoredProcedure);
        }
    }

    public Empresas TraerEmpresaPorCodigo(string codigo){
        Empresas empresa = null;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerEmpresaPorCodigo";
            empresa = connection.QueryFirstOrDefault<Empresas>(storedProcedure, new { Codigo = codigo }, commandType: CommandType.StoredProcedure);
        }
        return empresa;
    }

    public void GuardarFormularioEmpleador(FormulariosEmpleador formulario){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_GuardarFormularioEmpleador";
            connection.Execute(storedProcedure, new { IdUsuario = formulario.IdUsuario, Dificultades = formulario.Dificultades, Herramientas = formulario.Herramientas, Objetivos = formulario.Objetivos }, commandType: CommandType.StoredProcedure);
        }
    }

    public void ConfirmarCurso(int idCurso){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_ConfirmarCurso";
            connection.Execute(storedProcedure, new { IdCurso = idCurso }, commandType: CommandType.StoredProcedure);
        }
    }

    public void BorrarCurso(int idCurso){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_BorrarCurso";
            connection.Execute(storedProcedure, new { IdCurso = idCurso }, commandType: CommandType.StoredProcedure);
        }
    }

    public List<ActividadCurso> TraerActividadesEmpresa(int idEmpresa){
        List<ActividadCurso> actividades = new List<ActividadCurso>();
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerActividadesEmpresa";
            actividades = connection.Query<ActividadCurso>(storedProcedure, new { IdEmpresa = idEmpresa }, commandType: CommandType.StoredProcedure).ToList();
        }
        return actividades;
    }

    // ===================== CURSO POR NIVELES: unidades y lecciones =====================

    public int AgregarUnidad(Unidades unidad){
        int idUnidad = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_AgregarUnidad";
            idUnidad = connection.QueryFirstOrDefault<int>(storedProcedure, new { IdCurso = unidad.IdCurso, Numero = unidad.Numero, Titulo = unidad.Titulo, Objetivo = unidad.Objetivo }, commandType: CommandType.StoredProcedure);
        }
        return idUnidad;
    }

    public void AgregarLeccion(Lecciones leccion){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_AgregarLeccion";
            connection.Execute(storedProcedure, new { IdUnidad = leccion.IdUnidad, Numero = leccion.Numero, Titulo = leccion.Titulo, Objetivo = leccion.Objetivo, Estado = leccion.Estado }, commandType: CommandType.StoredProcedure);
        }
    }

    public List<Unidades> TraerUnidadesCurso(int idCurso){
        List<Unidades> unidades = new List<Unidades>();
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerUnidadesCurso";
            unidades = connection.Query<Unidades>(storedProcedure, new { IdCurso = idCurso }, commandType: CommandType.StoredProcedure).ToList();
        }
        return unidades;
    }

    public List<Lecciones> TraerLeccionesCurso(int idCurso){
        List<Lecciones> lecciones = new List<Lecciones>();
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerLeccionesCurso";
            lecciones = connection.Query<Lecciones>(storedProcedure, new { IdCurso = idCurso }, commandType: CommandType.StoredProcedure).ToList();
        }
        return lecciones;
    }
}
