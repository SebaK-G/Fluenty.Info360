using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;
using Fluenty.Services;

namespace Fluenty.Info360.Controllers;

// Sprint 2: los cursos son POR RANGO. Cada uno ve y crea cursos solo para los rangos que tiene abajo.
// El curso es uno para todo el rango, pero la IA nivela a cada persona (en qué unidad arranca).
public class CursoController : Controller
{
    private readonly GroqService _groqService;
    private readonly GroqFeatureToggle _groqFeatureToggle;

    public CursoController(GroqService groqService, GroqFeatureToggle groqFeatureToggle)
    {
        _groqService = groqService;
        _groqFeatureToggle = groqFeatureToggle;
    }

    // ============ Pantalla Cursos: los rangos de abajo, su gente y su curso ============
    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        List<Rangos> deAbajo = RangosDeAbajo(bd);
        if (deAbajo.Count == 0)
        {
            return RedirectToAction("Index", "Home");
        }

        // Antes de generar cursos, la empresa tiene que completar su formulario (lo hace el dueño)
        if (bd.TraerFormularioEmpresa(idEmpresa) == null)
        {
            if (EsDueno())
            {
                return RedirectToAction("FormularioEmpresa");
            }
            ViewBag.Mensaje = "El dueño todavía no completó el formulario de la empresa. Cuando lo complete, vas a poder generar los cursos.";
        }

        List<EmpleadoCurso> miembros = bd.TraerMiembrosEmpresa(idEmpresa);
        List<GrupoRango> grupos = new List<GrupoRango>();
        foreach (Rangos rango in deAbajo)
        {
            GrupoRango grupo = new GrupoRango();
            grupo.Rango = rango;
            grupo.Curso = bd.TraerCursoPorRango(rango.IdRango);
            foreach (EmpleadoCurso miembro in miembros)
            {
                if (miembro.IdRango == rango.IdRango)
                {
                    grupo.Miembros.Add(miembro);
                    if (miembro.TieneFormulario)
                    {
                        grupo.ConFormulario++;
                        if (grupo.Curso != null && miembro.IdCurso != grupo.Curso.IdCurso)
                        {
                            grupo.SinAsignar.Add(miembro);
                        }
                    }
                }
            }
            grupos.Add(grupo);
        }
        ViewBag.Grupos = grupos;
        ViewBag.PuedeGenerar = bd.TraerFormularioEmpresa(idEmpresa) != null;
        return View("Index");
    }

    // ============ DUEÑO: formulario de cómo quiere los cursos (preguntas del doc) ============
    public IActionResult FormularioEmpresa()
    {
        if (!EsDueno())
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        if (bd.TraerFormularioEmpresa(int.Parse(HttpContext.Session.GetString("IdEmpresa"))) != null)
        {
            return RedirectToAction("Index");
        }
        ViewBag.Herramientas = bd.TraerHerramientas();
        return View();
    }

    [HttpPost]
    public IActionResult FormularioEmpresa(string Dificultades, int[] Herramientas, string Objetivos)
    {
        if (!EsDueno())
        {
            return RedirectToAction("Acceso", "Home");
        }
        FormulariosEmpresa formulario = new FormulariosEmpresa();
        formulario.IdEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        formulario.Dificultades = Dificultades;
        formulario.Objetivos = Objetivos;

        BD bd = new BD();
        if (bd.TraerFormularioEmpresa(formulario.IdEmpresa) == null)
        {
            bd.GuardarFormularioEmpresa(formulario);
            if (Herramientas != null)
            {
                foreach (int idHerramienta in Herramientas)
                {
                    bd.AgregarHerramientaEmpresa(formulario.IdEmpresa, idHerramienta);
                }
            }
        }
        return RedirectToAction("Index");
    }

    // ============ La IA genera UN curso para todo el rango (queda como borrador) ============
    // Publico = true: además aparece en el buscador para todas las empresas. En ese caso la IA arma
    // el temario solo con el Tema y la Categoria, SIN datos de la empresa ni de las personas.
    [HttpPost]
    public async Task<IActionResult> Generar(int IdRango, bool Publico = false, string Tema = "", string Categoria = "")
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        Rangos rango = RangoDeAbajo(bd, IdRango);
        if (rango == null)
        {
            return RedirectToAction("Index");
        }

        // Si el rango ya tiene curso, no se genera otro
        Cursos existente = bd.TraerCursoPorRango(IdRango);
        if (existente != null)
        {
            return RedirectToAction("Ver", new { IdCurso = existente.IdCurso });
        }
        if (!_groqFeatureToggle.Enabled)
        {
            return MostrarError("La IA está deshabilitada en este momento.");
        }

        // 1) La gente del rango que completó el formulario (para la IA, como mucho 8)
        List<Usuarios> personas = new List<Usuarios>();
        List<FormulariosEmpleado> formularios = new List<FormulariosEmpleado>();
        foreach (EmpleadoCurso miembro in bd.TraerMiembrosEmpresa(idEmpresa))
        {
            if (miembro.IdRango == IdRango && miembro.TieneFormulario)
            {
                personas.Add(bd.TraerEmpleado(miembro.IdUsuario));
                formularios.Add(bd.TraerFormularioEmpleado(miembro.IdUsuario));
            }
        }
        if (personas.Count == 0)
        {
            return MostrarError("Nadie del rango " + rango.Nombre + " completó el formulario todavía.");
        }
        FormulariosEmpresa formEmpresa = bd.TraerFormularioEmpresa(idEmpresa);

        if (Tema == null)
        {
            Tema = "";
        }
        if (Publico && (Tema.Trim().Length < 5 || Array.IndexOf(Cursos.Categorias, Categoria) < 0))
        {
            return MostrarError("Para un curso público contá de qué trata (el tema) y elegí una categoría.");
        }

        // El prompt y los datos los arma GeneradorCursos (Models)
        string instrucciones = GeneradorCursos.InstruccionesCurso(Publico);
        string datos;
        if (Publico)
        {
            datos = GeneradorCursos.DatosPublico(Tema.Trim(), Categoria, personas, formularios);
        }
        else
        {
            datos = GeneradorCursos.DatosGrupo(bd, rango, personas, formularios, formEmpresa);
        }

        // 2) Le pedimos el curso a la IA (si viene mal, probamos una vez más)
        CursoIA cursoIA = null;
        try
        {
            int intento = 1;
            while (intento <= 2 && cursoIA == null)
            {
                string respuesta = await _groqService.PreguntarAsync(instrucciones, datos, true);
                cursoIA = GeneradorCursos.LeerCurso(respuesta);
                intento++;
            }
        }
        catch (Exception ex)
        {
            return MostrarError(ex.Message);
        }
        if (cursoIA == null)
        {
            return MostrarError("La IA respondió con un formato incorrecto. Probá de nuevo.");
        }

        // 3) Guardamos el contenido: el curso del rango, sus 4 unidades y 20 lecciones
        string objetivos = "";
        foreach (string objetivo in cursoIA.Objetivos)
        {
            objetivos = objetivos + objetivo + "\n";
        }
        int nivelGrupo = 1;
        foreach (FormulariosEmpleado form in formularios)
        {
            if (form.NivelIA > nivelGrupo)
            {
                nivelGrupo = form.NivelIA;
            }
        }
        // El curso no guarda su rango ni su empresa: se saben por las personas a las que se les asigna (abajo)
        Cursos curso = new Cursos();
        curso.Titulo = DatosIA.Recortar(cursoIA.Titulo, 100);
        curso.Descripcion = DatosIA.Recortar(cursoIA.Descripcion, 300);
        curso.Modelo = DatosIA.Recortar(cursoIA.Modelo, 50);
        curso.Nivel = nivelGrupo;
        curso.Objetivos = DatosIA.Recortar(objetivos, 600);
        curso.Publico = Publico;
        curso.Categoria = Publico ? Categoria : DatosIA.Recortar(cursoIA.Categoria, 50);
        curso.DuracionHoras = cursoIA.DuracionHoras > 0 ? cursoIA.DuracionHoras : 5;
        curso.Herramientas = DatosIA.Recortar(string.Join(" · ", cursoIA.Herramientas), 200);
        int idCurso = bd.CrearCurso(curso);

        foreach (UnidadIA unidadIA in cursoIA.Unidades)
        {
            Unidades unidad = new Unidades();
            unidad.IdCurso = idCurso;
            unidad.Numero = unidadIA.Numero;
            unidad.Titulo = DatosIA.Recortar(unidadIA.Titulo, 100);
            unidad.Objetivo = DatosIA.Recortar(unidadIA.Objetivo, 300);
            int idUnidad = bd.AgregarUnidad(unidad);

            foreach (LeccionIA leccionIA in unidadIA.Lecciones)
            {
                Lecciones leccion = new Lecciones();
                leccion.IdUnidad = idUnidad;
                leccion.Numero = leccionIA.Numero;
                leccion.Titulo = DatosIA.Recortar(leccionIA.Titulo, 100);
                leccion.Objetivo = DatosIA.Recortar(leccionIA.Objetivo, 300);
                bd.AgregarLeccion(leccion);
            }
        }

        // 4) Se lo asignamos a cada persona, con SU nivelación (si la IA no la mandó bien, se usa su nivel de IA)
        List<Lecciones> lecciones = bd.TraerLeccionesCurso(idCurso);
        for (int i = 0; i < personas.Count; i++)
        {
            int unidadInicial = DatosIA.UnidadPorNivel(formularios[i].NivelIA);
            string motivo = "Arranca según su experiencia con la IA.";
            foreach (NivelacionIA nivelacion in cursoIA.Nivelacion)
            {
                if (nivelacion.IdUsuario == personas[i].IdUsuario && nivelacion.UnidadInicial >= 1 && nivelacion.UnidadInicial <= 4)
                {
                    unidadInicial = nivelacion.UnidadInicial;
                    motivo = nivelacion.Motivo;
                }
            }
            Asignar(bd, idCurso, lecciones, personas[i].IdUsuario, unidadInicial, motivo);
        }

        // 5) Quien lo generó lo revisa antes de confirmarlo
        return RedirectToAction("Ver", new { IdCurso = idCurso });
    }

    // ============ Sumar al curso del rango a alguien que se unió después ============
    [HttpPost]
    public async Task<IActionResult> Sumar(int IdCurso, int IdUsuario)
    {
        BD bd = new BD();
        Cursos curso = bd.TraerCurso(IdCurso);
        if (curso == null || RangoDeAbajo(bd, curso.IdRango) == null)
        {
            return RedirectToAction("Index");
        }
        Usuarios persona = bd.TraerEmpleado(IdUsuario);
        FormulariosEmpleado form = bd.TraerFormularioEmpleado(IdUsuario);
        if (persona == null || form == null || persona.IdRango != curso.IdRango)
        {
            return RedirectToAction("Index");
        }
        foreach (Inscripciones asignacion in bd.TraerAsignacionesCurso(IdCurso))
        {
            if (asignacion.IdUsuario == IdUsuario)
            {
                return RedirectToAction("Ver", new { IdCurso = IdCurso, IdUsuario = IdUsuario });
            }
        }

        // La IA decide en qué unidad arranca esta persona (si falla, se usa su nivel de IA)
        int unidadInicial = DatosIA.UnidadPorNivel(form.NivelIA);
        string motivo = "Arranca según su experiencia con la IA.";
        if (_groqFeatureToggle.Enabled)
        {
            try
            {
                string instrucciones = GeneradorCursos.InstruccionesNivelacion();
                string datos = GeneradorCursos.DatosNivelacion(bd, curso, bd.TraerUnidadesCurso(IdCurso), persona, form);
                string respuesta = await _groqService.PreguntarAsync(instrucciones, datos, true);
                NivelacionIA nivelacion = GeneradorCursos.LeerNivelacion(respuesta);
                if (nivelacion != null)
                {
                    unidadInicial = nivelacion.UnidadInicial;
                    motivo = nivelacion.Motivo;
                }
            }
            catch (Exception)
            {
                // Si la IA falla, queda la nivelación por su nivel de IA
            }
        }
        Asignar(bd, IdCurso, bd.TraerLeccionesCurso(IdCurso), IdUsuario, unidadInicial, motivo);

        // Si el curso ya está confirmado, se le deja preparada su primera lección
        if (curso.Estado == "Confirmado" && _groqFeatureToggle.Enabled)
        {
            GeneradorLecciones.PrepararEnSegundoPlano(_groqService, IdUsuario, GeneradorLecciones.Siguientes(bd, IdCurso, IdUsuario, 0, 1));
        }
        return RedirectToAction("Ver", new { IdCurso = IdCurso, IdUsuario = IdUsuario });
    }

    // ============ Ver un curso ============
    // - Como alumno: tu propio curso (confirmado), con las lecciones para hacer.
    // - Como responsable: el curso de un rango de abajo, mirando el camino de cada persona.
    public IActionResult Ver(int IdCurso, int IdUsuario = 0)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        Cursos curso = bd.TraerCurso(IdCurso);
        if (curso == null)
        {
            return RedirectToAction("Index", "Home");
        }
        int idUsuarioSesion = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        List<Inscripciones> asignaciones = bd.TraerAsignacionesCurso(IdCurso);

        Inscripciones propia = null;
        foreach (Inscripciones item in asignaciones)
        {
            if (item.IdUsuario == idUsuarioSesion)
            {
                propia = item;
            }
        }
        bool esResponsable = RangoDeAbajo(bd, curso.IdRango) != null;

        Inscripciones asignacion = null;
        bool modoAlumno = false;
        if (IdUsuario == 0 && propia != null && curso.Estado == "Confirmado")
        {
            asignacion = propia;
            modoAlumno = true;
        }
        else if (esResponsable)
        {
            foreach (Inscripciones item in asignaciones)
            {
                // El responsable mira solo a su gente (no a los que se sumaron desde el buscador)
                if (asignacion == null && item.Origen != "Publico" && (IdUsuario == 0 || item.IdUsuario == IdUsuario))
                {
                    asignacion = item;
                }
            }
        }
        else
        {
            return RedirectToAction("Index", "Home");
        }

        // Las personas del equipo que tienen el curso, para elegir de quién mirar el camino
        // (los que se inscribieron desde el buscador son de otras empresas: no se muestran)
        List<Usuarios> personas = new List<Usuarios>();
        foreach (Inscripciones item in asignaciones)
        {
            if (item.Origen != "Publico")
            {
                personas.Add(bd.TraerEmpleado(item.IdUsuario));
            }
        }

        ViewBag.Curso = curso;
        ViewBag.Asignacion = asignacion;
        ViewBag.Personas = personas;
        ViewBag.ModoAlumno = modoAlumno;
        ViewBag.EsEmpleador = !modoAlumno;
        ViewBag.NombreRango = NombreRango(bd, curso.IdRango);
        // Si se sumó desde el buscador, el curso no es de su rango: es un curso público
        ViewBag.EsInscripcionPublica = modoAlumno && asignacion.Origen == "Publico";
        ViewBag.NombreEmpleado = "";
        ViewBag.Unidades = bd.TraerUnidadesCurso(IdCurso);
        ViewBag.Lecciones = new List<Lecciones>();
        if (asignacion != null)
        {
            Usuarios persona = bd.TraerEmpleado(asignacion.IdUsuario);
            ViewBag.NombreEmpleado = persona.Nombre + " " + persona.Apellido;
            ViewBag.Lecciones = bd.TraerLeccionesUsuario(IdCurso, asignacion.IdUsuario);
            ViewBag.XP = persona.XP;
            ViewBag.Racha = persona.Racha;
        }
        return View("Ver");
    }

    // ============ Rehacer UNA unidad del borrador con un pedido ("que sea más difícil") ============
    [HttpPost]
    public async Task<IActionResult> RehacerUnidad(int IdCurso, int NumeroUnidad, string Pedido)
    {
        BD bd = new BD();
        Cursos curso = bd.TraerCurso(IdCurso);
        if (curso == null || RangoDeAbajo(bd, curso.IdRango) == null)
        {
            return RedirectToAction("Index");
        }
        if (curso.Estado != "Borrador")
        {
            ViewBag.Error = "Este curso ya está confirmado: solo se pueden cambiar unidades de un borrador.";
            return Ver(IdCurso);
        }
        if (Pedido == null || Pedido.Trim() == "")
        {
            ViewBag.Error = "Escribí qué querés cambiar de la unidad " + NumeroUnidad + ".";
            return Ver(IdCurso);
        }
        if (!_groqFeatureToggle.Enabled)
        {
            ViewBag.Error = "La IA está deshabilitada en este momento.";
            return Ver(IdCurso);
        }

        List<Inscripciones> asignaciones = bd.TraerAsignacionesCurso(IdCurso);
        List<Unidades> unidades = bd.TraerUnidadesCurso(IdCurso);
        List<Lecciones> lecciones = bd.TraerLeccionesCurso(IdCurso);
        Unidades unidadVieja = null;
        foreach (Unidades unidad in unidades)
        {
            if (unidad.Numero == NumeroUnidad)
            {
                unidadVieja = unidad;
            }
        }
        if (unidadVieja == null)
        {
            return RedirectToAction("Ver", new { IdCurso = IdCurso });
        }

        // Los datos del grupo: la gente que tiene el curso
        List<Usuarios> personas = new List<Usuarios>();
        List<FormulariosEmpleado> formularios = new List<FormulariosEmpleado>();
        foreach (Inscripciones item in asignaciones)
        {
            FormulariosEmpleado form = bd.TraerFormularioEmpleado(item.IdUsuario);
            if (form != null)
            {
                personas.Add(bd.TraerEmpleado(item.IdUsuario));
                formularios.Add(form);
            }
        }
        // El prompt y los datos los arma GeneradorCursos (Models)
        Rangos rango = RangoDeAbajo(bd, curso.IdRango);
        string instrucciones = GeneradorCursos.InstruccionesUnidad(NumeroUnidad, curso.Publico);
        string datosBase;
        if (curso.Publico)
        {
            // Curso público: la unidad se rehace sin datos de la empresa ni de las personas
            datosBase = "CURSO PÚBLICO de la categoría " + curso.Categoria + ".\n";
        }
        else
        {
            datosBase = GeneradorCursos.DatosGrupo(bd, rango, personas, formularios, bd.TraerFormularioEmpresa(curso.IdEmpresa));
        }
        string datos = GeneradorCursos.DatosRehacerUnidad(datosBase, curso, unidades, lecciones, NumeroUnidad, Pedido);

        UnidadIA unidadIA = null;
        try
        {
            int intento = 1;
            while (intento <= 2 && unidadIA == null)
            {
                string respuesta = await _groqService.PreguntarAsync(instrucciones, datos, true);
                unidadIA = GeneradorCursos.LeerUnidad(respuesta);
                intento++;
            }
        }
        catch (Exception ex)
        {
            ViewBag.Error = ex.Message;
            return Ver(IdCurso);
        }
        if (unidadIA == null)
        {
            ViewBag.Error = "La IA respondió con un formato incorrecto. Probá de nuevo.";
            return Ver(IdCurso);
        }

        // Reemplazamos la unidad y sus 5 lecciones; al borrar las viejas se borra solo su estado (ON DELETE CASCADE)
        unidadVieja.Titulo = DatosIA.Recortar(unidadIA.Titulo, 100);
        unidadVieja.Objetivo = DatosIA.Recortar(unidadIA.Objetivo, 300);
        bd.ActualizarUnidad(unidadVieja);
        bd.BorrarLeccionesUnidad(unidadVieja.IdUnidad);

        int numero = 1;
        foreach (LeccionIA leccionIA in unidadIA.Lecciones)
        {
            Lecciones leccion = new Lecciones();
            leccion.IdUnidad = unidadVieja.IdUnidad;
            leccion.Numero = numero;
            leccion.Titulo = DatosIA.Recortar(leccionIA.Titulo, 100);
            leccion.Objetivo = DatosIA.Recortar(leccionIA.Objetivo, 300);
            int idLeccion = bd.AgregarLeccion(leccion);
            foreach (Inscripciones item in asignaciones)
            {
                bd.AgregarLeccionUsuario(idLeccion, item.IdInscripcion, Lecciones.EstadoInicial(NumeroUnidad, numero, item.UnidadInicial));
            }
            numero++;
        }
        return Redirect(Url.Action("Ver", new { IdCurso = IdCurso }) + "#unidad-" + NumeroUnidad);
    }

    // ============ Confirmar: el curso le llega a todo el rango ============
    [HttpPost]
    public IActionResult Confirmar(int IdCurso)
    {
        BD bd = new BD();
        Cursos curso = bd.TraerCurso(IdCurso);
        if (curso == null || RangoDeAbajo(bd, curso.IdRango) == null)
        {
            return RedirectToAction("Index");
        }
        bd.ConfirmarCurso(IdCurso);

        // Se le deja preparada a cada persona su primera lección, así no espera cuando la abre.
        // Es una sola por persona para no gastar créditos en lecciones que capaz nadie abre;
        // mientras hace una, se preparan las 2 siguientes.
        if (_groqFeatureToggle.Enabled)
        {
            foreach (Inscripciones asignacion in bd.TraerAsignacionesCurso(IdCurso))
            {
                GeneradorLecciones.PrepararEnSegundoPlano(_groqService, asignacion.IdUsuario, GeneradorLecciones.Siguientes(bd, IdCurso, asignacion.IdUsuario, 0, 1));
            }
        }
        ViewBag.IdCurso = IdCurso;
        ViewBag.Titulo = curso.Titulo;
        return View("CursoCreado");
    }

    // ============ Descartar el borrador para generarlo de nuevo ============
    [HttpPost]
    public IActionResult Descartar(int IdCurso)
    {
        BD bd = new BD();
        Cursos curso = bd.TraerCurso(IdCurso);
        if (curso != null && curso.Estado == "Borrador" && RangoDeAbajo(bd, curso.IdRango) != null)
        {
            bd.BorrarCurso(IdCurso);
        }
        return RedirectToAction("Index");
    }

    // ================= Métodos de ayuda =================

    private bool EsDueno()
    {
        return HttpContext.Session.GetString("TipoUsuario") == "Empleador";
    }

    // Los rangos que el usuario logueado tiene abajo (los únicos para los que puede crear cursos)
    private List<Rangos> RangosDeAbajo(BD bd)
    {
        string idRangoTexto = HttpContext.Session.GetString("IdRango");
        if (idRangoTexto == null || idRangoTexto == "")
        {
            return new List<Rangos>();
        }
        List<Rangos> todos = bd.TraerRangosEmpresa(int.Parse(HttpContext.Session.GetString("IdEmpresa")));
        return Arbol.Debajo(todos, int.Parse(idRangoTexto));
    }

    // Devuelve el rango si está abajo del usuario logueado; si no, null (así nadie toca rangos de arriba o de otra empresa)
    private Rangos RangoDeAbajo(BD bd, int idRango)
    {
        foreach (Rangos rango in RangosDeAbajo(bd))
        {
            if (rango.IdRango == idRango)
            {
                return rango;
            }
        }
        return null;
    }

    private string NombreRango(BD bd, int idRango)
    {
        foreach (Rangos rango in bd.TraerRangosEmpresa(int.Parse(HttpContext.Session.GetString("IdEmpresa"))))
        {
            if (rango.IdRango == idRango)
            {
                return rango.Nombre;
            }
        }
        return "";
    }

    // Le asigna el curso a una persona: Inscripciones con su nivelación y el estado de cada lección
    private void Asignar(BD bd, int idCurso, List<Lecciones> lecciones, int idUsuario, int unidadInicial, string motivo)
    {
        Inscripciones asignacion = new Inscripciones();
        asignacion.IdCurso = idCurso;
        asignacion.IdUsuario = idUsuario;
        asignacion.UnidadInicial = unidadInicial;
        asignacion.MotivoNivel = DatosIA.Recortar(motivo, 300);
        asignacion.Origen = "Equipo";
        int idInscripcion = bd.AsignarCurso(asignacion);
        foreach (Lecciones leccion in lecciones)
        {
            bd.AgregarLeccionUsuario(leccion.IdLeccion, idInscripcion, Lecciones.EstadoInicial(leccion.NumeroUnidad, leccion.Numero, unidadInicial));
        }
        bd.ActualizarProgreso(idCurso, idUsuario);
    }

    private IActionResult MostrarError(string mensaje)
    {
        ViewBag.Mensaje = mensaje;
        return Index();
    }
}
