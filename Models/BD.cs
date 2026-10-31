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
            // Solo entran usuarios activos de empresas activas (borrado lógico)
            string query = "SELECT Usuarios.* FROM Usuarios INNER JOIN Empresas ON Empresas.IdEmpresa = Usuarios.IdEmpresa WHERE Usuarios.Email = @pEmail AND Usuarios.Contrasena = @pContrasena AND Usuarios.Activo = 1 AND Empresas.Activo = 1";
            usuario = connection.QueryFirstOrDefault<Usuarios>(query, new { pEmail = email, pContrasena = contrasena });
        }
        return usuario;
    }

    public bool ExisteUsuarioDuplicado(string email, string dni, string telefono){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string query = @"SELECT TOP 1 IdUsuario FROM Usuarios WHERE Email = @pEmail OR Dni = @pDni OR Telefono = @pTelefono";
            int? usuario = connection.QueryFirstOrDefault<int?>(query, new { pEmail = email, pDni = dni, pTelefono = telefono });
            return usuario.HasValue;
        }
    }

    // Crea el usuario y devuelve su IdUsuario (hoy lo usa el link para unirse; después, el alta desde la Landing)
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
            connection.Execute(storedProcedure, new { IdUsuario = formulario.IdUsuario, NivelIA = formulario.NivelIA, Tareas = formulario.Tareas, Dificultades = formulario.Dificultades, Objetivos = formulario.Objetivos, TiempoXDia = formulario.TiempoXDia, DificultadPrincipal = formulario.DificultadPrincipal, MejoraEmpresa = formulario.MejoraEmpresa, FortalezasDebilidades = formulario.FortalezasDebilidades, MejoraPersonal = formulario.MejoraPersonal }, commandType: CommandType.StoredProcedure);
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

    public FormulariosEmpresa TraerFormularioEmpresa(int idEmpresa){
        FormulariosEmpresa formulario = null;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerFormularioEmpresa";
            formulario = connection.QueryFirstOrDefault<FormulariosEmpresa>(storedProcedure, new { IdEmpresa = idEmpresa }, commandType: CommandType.StoredProcedure);
        }
        return formulario;
    }

    public int CrearCurso(Cursos curso){
        int idCurso = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_CrearCurso";
            idCurso = connection.QueryFirstOrDefault<int>(storedProcedure, new { IdEmpresa = curso.IdEmpresa, Titulo = curso.Titulo, Descripcion = curso.Descripcion, Modelo = curso.Modelo, Nivel = curso.Nivel, Objetivos = curso.Objetivos }, commandType: CommandType.StoredProcedure);
        }
        return idCurso;
    }

    // ===================== CURSOS ASIGNADOS A CADA PERSONA (CursoXUsuario) =====================

    public void AsignarCurso(CursoXUsuario asignacion){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_AsignarCurso";
            connection.Execute(storedProcedure, new { IdCurso = asignacion.IdCurso, IdUsuario = asignacion.IdUsuario, UnidadInicial = asignacion.UnidadInicial, MotivoNivel = asignacion.MotivoNivel }, commandType: CommandType.StoredProcedure);
        }
    }

    public List<CursoXUsuario> TraerAsignacionesCurso(int idCurso){
        List<CursoXUsuario> asignaciones = new List<CursoXUsuario>();
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerAsignacionesCurso";
            asignaciones = connection.Query<CursoXUsuario>(storedProcedure, new { IdCurso = idCurso }, commandType: CommandType.StoredProcedure).ToList();
        }
        return asignaciones;
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

    public void ActualizarProgreso(int idCurso, int idUsuario, int progreso){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_ActualizarProgreso";
            connection.Execute(storedProcedure, new { IdCurso = idCurso, IdUsuario = idUsuario, Progreso = progreso }, commandType: CommandType.StoredProcedure);
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

    public void GuardarFormularioEmpresa(FormulariosEmpresa formulario){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_GuardarFormularioEmpresa";
            connection.Execute(storedProcedure, new { IdEmpresa = formulario.IdEmpresa, Dificultades = formulario.Dificultades, Objetivos = formulario.Objetivos }, commandType: CommandType.StoredProcedure);
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

    public void ActualizarUnidad(Unidades unidad){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_ActualizarUnidad";
            connection.Execute(storedProcedure, new { IdUnidad = unidad.IdUnidad, Titulo = unidad.Titulo, Objetivo = unidad.Objetivo }, commandType: CommandType.StoredProcedure);
        }
    }

    public void BorrarLeccionesUnidad(int idUnidad){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_BorrarLeccionesUnidad";
            connection.Execute(storedProcedure, new { IdUnidad = idUnidad }, commandType: CommandType.StoredProcedure);
        }
    }

    // Devuelve el IdLeccion, para después cargar el estado de cada persona en LeccionXUsuario
    public int AgregarLeccion(Lecciones leccion){
        int idLeccion = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_AgregarLeccion";
            idLeccion = connection.QueryFirstOrDefault<int>(storedProcedure, new { IdUnidad = leccion.IdUnidad, Numero = leccion.Numero, Titulo = leccion.Titulo, Objetivo = leccion.Objetivo }, commandType: CommandType.StoredProcedure);
        }
        return idLeccion;
    }

    public void AgregarLeccionUsuario(int idLeccion, int idUsuario, string estado){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_AgregarLeccionUsuario";
            connection.Execute(storedProcedure, new { IdLeccion = idLeccion, IdUsuario = idUsuario, Estado = estado }, commandType: CommandType.StoredProcedure);
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

    // Las lecciones del curso con el estado que tienen para esa persona
    public List<Lecciones> TraerLeccionesUsuario(int idCurso, int idUsuario){
        List<Lecciones> lecciones = new List<Lecciones>();
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerLeccionesUsuario";
            lecciones = connection.Query<Lecciones>(storedProcedure, new { IdCurso = idCurso, IdUsuario = idUsuario }, commandType: CommandType.StoredProcedure).ToList();
        }
        return lecciones;
    }

    // ===================== HERRAMIENTAS (catálogo) =====================

    public List<Herramientas> TraerHerramientas(){
        List<Herramientas> herramientas = new List<Herramientas>();
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerHerramientas";
            herramientas = connection.Query<Herramientas>(storedProcedure, commandType: CommandType.StoredProcedure).ToList();
        }
        return herramientas;
    }

    public void AgregarHerramientaUsuario(int idUsuario, int idHerramienta){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_AgregarHerramientaUsuario";
            connection.Execute(storedProcedure, new { IdUsuario = idUsuario, IdHerramienta = idHerramienta }, commandType: CommandType.StoredProcedure);
        }
    }

    public void AgregarHerramientaEmpresa(int idEmpresa, int idHerramienta){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_AgregarHerramientaEmpresa";
            connection.Execute(storedProcedure, new { IdEmpresa = idEmpresa, IdHerramienta = idHerramienta }, commandType: CommandType.StoredProcedure);
        }
    }

    public List<Herramientas> TraerHerramientasUsuario(int idUsuario){
        List<Herramientas> herramientas = new List<Herramientas>();
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerHerramientasUsuario";
            herramientas = connection.Query<Herramientas>(storedProcedure, new { IdUsuario = idUsuario }, commandType: CommandType.StoredProcedure).ToList();
        }
        return herramientas;
    }

    public List<Herramientas> TraerHerramientasEmpresa(int idEmpresa){
        List<Herramientas> herramientas = new List<Herramientas>();
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerHerramientasEmpresa";
            herramientas = connection.Query<Herramientas>(storedProcedure, new { IdEmpresa = idEmpresa }, commandType: CommandType.StoredProcedure).ToList();
        }
        return herramientas;
    }

    // ===================== BORRADO LÓGICO =====================

    public void DesactivarUsuario(int idUsuario){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_DesactivarUsuario";
            connection.Execute(storedProcedure, new { IdUsuario = idUsuario }, commandType: CommandType.StoredProcedure);
        }
    }

    public void DesactivarEmpresa(int idEmpresa){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_DesactivarEmpresa";
            connection.Execute(storedProcedure, new { IdEmpresa = idEmpresa }, commandType: CommandType.StoredProcedure);
        }
    }
}
