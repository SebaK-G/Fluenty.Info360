using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;
namespace Fluenty.Info360.Models;

public class BD
{
    private string _connectionString = @"Server=localhost;DataBase=FluentySprint2;Integrated Security=True;TrustServerCertificate=True;";

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
        string query = "INSERT INTO Usuarios (Nombre, Apellido, IdEmpresa, Email, Dni, Telefono, Contrasena, Rol, Puesto, IdRango) VALUES (@pNombre, @pApellido, @pIdEmpresa, @pEmail, @pDni, @pTelefono, @pContrasena, @pRol, @pPuesto, @pIdRango); SELECT CAST(SCOPE_IDENTITY() as int)";
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            idUsuario = connection.QuerySingle<int>(query, new { pNombre = usuario.Nombre, pApellido = usuario.Apellido, pIdEmpresa = usuario.IdEmpresa, pEmail = usuario.Email, pDni = usuario.Dni, pTelefono = usuario.Telefono, pContrasena = usuario.Contrasena, pRol = usuario.Rol, pPuesto = usuario.Puesto, pIdRango = usuario.IdRango });
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
            connection.Execute(storedProcedure, new { IdUsuario = formulario.IdUsuario, NivelIA = formulario.NivelIA, Tareas = formulario.Tareas, Dificultades = formulario.Dificultades, Objetivos = formulario.Objetivos, TiempoXDia = formulario.TiempoXDia, DificultadPrincipal = formulario.DificultadPrincipal, MejoraEmpresa = formulario.MejoraEmpresa, FortalezasDebilidades = formulario.FortalezasDebilidades, MejoraPersonal = formulario.MejoraPersonal, RangoEdad = formulario.RangoEdad, ComodidadTecnologia = formulario.ComodidadTecnologia, ComoAprende = formulario.ComoAprende, Hobbies = formulario.Hobbies, Area = formulario.Area, DiaTipico = formulario.DiaTipico, TareaConcreta = formulario.TareaConcreta, DatosClientes = formulario.DatosClientes, PreocupacionIA = formulario.PreocupacionIA }, commandType: CommandType.StoredProcedure);
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

    // Todas las personas de la empresa (menos el dueño) con su rango y su curso
    public List<EmpleadoCurso> TraerMiembrosEmpresa(int idEmpresa){
        List<EmpleadoCurso> lista = new List<EmpleadoCurso>();
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerMiembrosEmpresa";
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
            idCurso = connection.QueryFirstOrDefault<int>(storedProcedure, new { IdEmpresa = curso.IdEmpresa, IdRango = curso.IdRango, Titulo = curso.Titulo, Descripcion = curso.Descripcion, Modelo = curso.Modelo, Nivel = curso.Nivel, Objetivos = curso.Objetivos }, commandType: CommandType.StoredProcedure);
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

    // Recalcula el porcentaje de avance de esa persona en ese curso
    public void ActualizarProgreso(int idCurso, int idUsuario){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_ActualizarProgreso";
            connection.Execute(storedProcedure, new { IdCurso = idCurso, IdUsuario = idUsuario }, commandType: CommandType.StoredProcedure);
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

    // El link de cada rango: el rango con el nombre de su empresa
    public Rangos TraerRangoPorCodigo(string codigo){
        Rangos rango = null;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerRangoPorCodigo";
            rango = connection.QueryFirstOrDefault<Rangos>(storedProcedure, new { Codigo = codigo }, commandType: CommandType.StoredProcedure);
        }
        return rango;
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

    // Las lecciones del curso (solo el contenido), para asignárselas a una persona
    public List<Lecciones> TraerLeccionesCurso(int idCurso){
        List<Lecciones> lecciones = new List<Lecciones>();
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerLeccionesCurso";
            lecciones = connection.Query<Lecciones>(storedProcedure, new { IdCurso = idCurso }, commandType: CommandType.StoredProcedure).ToList();
        }
        return lecciones;
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

    // ===================== SPRINT 2: RANGOS =====================

    public List<Rangos> TraerRangosEmpresa(int idEmpresa){
        List<Rangos> rangos = new List<Rangos>();
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerRangosEmpresa";
            rangos = connection.Query<Rangos>(storedProcedure, new { IdEmpresa = idEmpresa }, commandType: CommandType.StoredProcedure).ToList();
        }
        return rangos;
    }

    public int CrearRango(Rangos rango){
        int idRango = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_CrearRango";
            idRango = connection.QueryFirstOrDefault<int>(storedProcedure, new { IdEmpresa = rango.IdEmpresa, Nombre = rango.Nombre, IdRangoPadre = rango.IdRangoPadre, Codigo = rango.Codigo, Tareas = rango.Tareas }, commandType: CommandType.StoredProcedure);
        }
        return idRango;
    }

    public Cursos TraerCursoPorRango(int idRango){
        Cursos curso = null;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerCursoPorRango";
            curso = connection.QueryFirstOrDefault<Cursos>(storedProcedure, new { IdRango = idRango }, commandType: CommandType.StoredProcedure);
        }
        return curso;
    }

    // ===================== SPRINT 2: REPRODUCTOR DE LECCIONES =====================

    public LeccionUsuario TraerLeccionUsuario(int idLeccion, int idUsuario){
        LeccionUsuario leccion = null;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerLeccionUsuario";
            leccion = connection.QueryFirstOrDefault<LeccionUsuario>(storedProcedure, new { IdLeccion = idLeccion, IdUsuario = idUsuario }, commandType: CommandType.StoredProcedure);
        }
        return leccion;
    }

    public List<Tarjetas> TraerTarjetas(int idLeccionXUsuario){
        List<Tarjetas> tarjetas = new List<Tarjetas>();
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerTarjetas";
            tarjetas = connection.Query<Tarjetas>(storedProcedure, new { IdLeccionXUsuario = idLeccionXUsuario }, commandType: CommandType.StoredProcedure).ToList();
        }
        return tarjetas;
    }

    public Tarjetas TraerTarjeta(int idTarjeta){
        Tarjetas tarjeta = null;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerTarjeta";
            tarjeta = connection.QueryFirstOrDefault<Tarjetas>(storedProcedure, new { IdTarjeta = idTarjeta }, commandType: CommandType.StoredProcedure);
        }
        return tarjeta;
    }

    // Devuelve el IdTarjeta (sirve para mandarle al navegador la pregunta nueva)
    public int AgregarTarjeta(Tarjetas tarjeta){
        int idTarjeta = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_AgregarTarjeta";
            idTarjeta = connection.QueryFirstOrDefault<int>(storedProcedure, new { IdLeccionXUsuario = tarjeta.IdLeccionXUsuario, Orden = tarjeta.Orden, Version = tarjeta.Version, Tipo = tarjeta.Tipo, Titulo = tarjeta.Titulo, ParaQue = tarjeta.ParaQue, Texto = tarjeta.Texto, Ejemplo = tarjeta.Ejemplo, Contexto = tarjeta.Contexto, PedidoMalo = tarjeta.PedidoMalo, RespuestaMala = tarjeta.RespuestaMala, PedidoBueno = tarjeta.PedidoBueno, RespuestaBuena = tarjeta.RespuestaBuena, OpcionA = tarjeta.OpcionA, OpcionB = tarjeta.OpcionB, OpcionC = tarjeta.OpcionC, Correcta = tarjeta.Correcta, Explicacion = tarjeta.Explicacion }, commandType: CommandType.StoredProcedure);
        }
        return idTarjeta;
    }

    public void ActualizarTextoTarjeta(int idTarjeta, string titulo, string texto){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_ActualizarTextoTarjeta";
            connection.Execute(storedProcedure, new { IdTarjeta = idTarjeta, Titulo = titulo, Texto = texto }, commandType: CommandType.StoredProcedure);
        }
    }

    // Devuelve el IdUsuario dueño de la tarjeta (0 si no existe)
    public int TraerDuenoTarjeta(int idTarjeta){
        int idUsuario = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerDuenoTarjeta";
            idUsuario = connection.QueryFirstOrDefault<int>(storedProcedure, new { IdTarjeta = idTarjeta }, commandType: CommandType.StoredProcedure);
        }
        return idUsuario;
    }

    public void CompletarLeccion(int idLeccionXUsuario){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_CompletarLeccion";
            connection.Execute(storedProcedure, new { IdLeccionXUsuario = idLeccionXUsuario }, commandType: CommandType.StoredProcedure);
        }
    }

    public void ActualizarXPRacha(int idUsuario, int xp, int racha, DateTime ultimoDiaActivo){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_ActualizarXPRacha";
            connection.Execute(storedProcedure, new { IdUsuario = idUsuario, XP = xp, Racha = racha, UltimoDiaActivo = ultimoDiaActivo }, commandType: CommandType.StoredProcedure);
        }
    }

    // ===================== SPRINT 2: PREGUNTAS, NOTAS Y TOPE DE PEDIDOS =====================

    public int TraerConfiguracion(string clave){
        int valor = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerConfiguracion";
            valor = connection.QueryFirstOrDefault<int>(storedProcedure, new { Clave = clave }, commandType: CommandType.StoredProcedure);
        }
        return valor;
    }

    public void ResponderTarjeta(int idTarjeta, string resultado){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_ResponderTarjeta";
            connection.Execute(storedProcedure, new { IdTarjeta = idTarjeta, Resultado = resultado }, commandType: CommandType.StoredProcedure);
        }
    }

    public void ActualizarPractica(int idTarjeta, string resultado){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_ActualizarPractica";
            connection.Execute(storedProcedure, new { IdTarjeta = idTarjeta, Resultado = resultado }, commandType: CommandType.StoredProcedure);
        }
    }

    public void SumarReexplicacion(int idTarjeta){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_SumarReexplicacion";
            connection.Execute(storedProcedure, new { IdTarjeta = idTarjeta }, commandType: CommandType.StoredProcedure);
        }
    }

    public void ReiniciarTarjeta(int idTarjeta){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_ReiniciarTarjeta";
            connection.Execute(storedProcedure, new { IdTarjeta = idTarjeta }, commandType: CommandType.StoredProcedure);
        }
    }

    public void BorrarTarjetas(int idLeccionXUsuario){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_BorrarTarjetas";
            connection.Execute(storedProcedure, new { IdLeccionXUsuario = idLeccionXUsuario }, commandType: CommandType.StoredProcedure);
        }
    }

    public void GuardarNotaLeccion(int idLeccionXUsuario, int nota){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_GuardarNotaLeccion";
            connection.Execute(storedProcedure, new { IdLeccionXUsuario = idLeccionXUsuario, Nota = nota }, commandType: CommandType.StoredProcedure);
        }
    }

    // Devuelve true si todavía tenía pedidos a la IA hoy (y le descuenta uno); false si ya llegó al tope
    public bool UsarPedidoIA(int idUsuario, int tope){
        int pudo = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_UsarPedidoIA";
            pudo = connection.QueryFirstOrDefault<int>(storedProcedure, new { IdUsuario = idUsuario, Tope = tope }, commandType: CommandType.StoredProcedure);
        }
        return pudo == 1;
    }

    public int TraerPedidosIAHoy(int idUsuario){
        int cantidad = 0;
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_TraerPedidosIAHoy";
            cantidad = connection.QueryFirstOrDefault<int>(storedProcedure, new { IdUsuario = idUsuario }, commandType: CommandType.StoredProcedure);
        }
        return cantidad;
    }

    // ===================== SPRINT 2: EL EMPLEADO QUE NO TIENE GANAS =====================

    public void ActualizarTareasRango(int idRango, string tareas){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_ActualizarTareasRango";
            connection.Execute(storedProcedure, new { IdRango = idRango, Tareas = tareas }, commandType: CommandType.StoredProcedure);
        }
    }

    public void ActualizarTareaConcreta(int idUsuario, string tareaConcreta){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_ActualizarTareaConcreta";
            connection.Execute(storedProcedure, new { IdUsuario = idUsuario, TareaConcreta = tareaConcreta }, commandType: CommandType.StoredProcedure);
        }
    }

    public void SaltarAlExamen(int idCurso, int idUsuario, int numeroUnidad){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_SaltarAlExamen";
            connection.Execute(storedProcedure, new { IdCurso = idCurso, IdUsuario = idUsuario, NumeroUnidad = numeroUnidad }, commandType: CommandType.StoredProcedure);
        }
    }

    public void BajarUnidad(int idCurso, int idUsuario, int numeroUnidad){
        using (SqlConnection connection = new SqlConnection(_connectionString)){
            string storedProcedure = "sp_BajarUnidad";
            connection.Execute(storedProcedure, new { IdCurso = idCurso, IdUsuario = idUsuario, NumeroUnidad = numeroUnidad }, commandType: CommandType.StoredProcedure);
        }
    }
}
