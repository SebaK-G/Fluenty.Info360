using System.Text.Json;
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
    [HttpPost]
    public async Task<IActionResult> Generar(int IdRango)
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

        string instrucciones = ArmarInstrucciones();
        string datos = ArmarDatosGrupo(bd, rango, personas, formularios, formEmpresa);

        // 2) Le pedimos el curso a la IA (si viene mal, probamos una vez más)
        CursoIA cursoIA = null;
        try
        {
            int intento = 1;
            while (intento <= 2 && cursoIA == null)
            {
                string respuesta = await _groqService.PreguntarAsync(instrucciones, datos, true);
                cursoIA = LeerCurso(respuesta);
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
        Cursos curso = new Cursos();
        curso.IdEmpresa = idEmpresa;
        curso.IdRango = IdRango;
        curso.Titulo = DatosIA.Recortar(cursoIA.Titulo, 100);
        curso.Descripcion = DatosIA.Recortar(cursoIA.Descripcion, 300);
        curso.Modelo = DatosIA.Recortar(cursoIA.Modelo, 50);
        curso.Nivel = nivelGrupo;
        curso.Objetivos = DatosIA.Recortar(objetivos, 600);
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
        foreach (CursoXUsuario asignacion in bd.TraerAsignacionesCurso(IdCurso))
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
                string plan = "";
                foreach (Unidades unidad in bd.TraerUnidadesCurso(IdCurso))
                {
                    plan = plan + "Unidad " + unidad.Numero + ": " + unidad.Titulo + " (" + unidad.Objetivo + ")\n";
                }
                string instrucciones = "Sos el que nivela a las personas en los cursos de Fluenty (4 unidades: la 1 son fundamentos y la 4 es lo más avanzado). ";
                instrucciones = instrucciones + "Decidí en qué unidad arranca esta persona según lo que ya sabe de IA: si nunca la usó o solo la probó, en la 1; si la usa a veces, puede ir a la 2; si la usa seguido en su trabajo, a la 3. Casi nunca a la 4. ";
                instrucciones = instrucciones + "Respondé SOLO con un JSON: {\"unidadInicial\": 1, \"motivo\": \"una oración en español rioplatense\"}";
                string datos = "PLAN DEL CURSO \"" + curso.Titulo + "\":\n" + plan + "\nLA PERSONA:\n" + DatosIA.Persona(bd, persona, form);
                string respuesta = await _groqService.PreguntarAsync(instrucciones, datos, true);
                JsonSerializerOptions opciones = new JsonSerializerOptions();
                opciones.PropertyNameCaseInsensitive = true;
                NivelacionIA nivelacion = JsonSerializer.Deserialize<NivelacionIA>(respuesta, opciones);
                if (nivelacion != null && nivelacion.UnidadInicial >= 1 && nivelacion.UnidadInicial <= 4)
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
        List<CursoXUsuario> asignaciones = bd.TraerAsignacionesCurso(IdCurso);

        CursoXUsuario propia = null;
        foreach (CursoXUsuario item in asignaciones)
        {
            if (item.IdUsuario == idUsuarioSesion)
            {
                propia = item;
            }
        }
        bool esResponsable = RangoDeAbajo(bd, curso.IdRango) != null;

        CursoXUsuario asignacion = null;
        bool modoAlumno = false;
        if (IdUsuario == 0 && propia != null && curso.Estado == "Confirmado")
        {
            asignacion = propia;
            modoAlumno = true;
        }
        else if (esResponsable)
        {
            foreach (CursoXUsuario item in asignaciones)
            {
                if (asignacion == null && (IdUsuario == 0 || item.IdUsuario == IdUsuario))
                {
                    asignacion = item;
                }
            }
        }
        else
        {
            return RedirectToAction("Index", "Home");
        }

        // Las personas del curso, para elegir de quién mirar el camino
        List<Usuarios> personas = new List<Usuarios>();
        foreach (CursoXUsuario item in asignaciones)
        {
            personas.Add(bd.TraerEmpleado(item.IdUsuario));
        }

        ViewBag.Curso = curso;
        ViewBag.Asignacion = asignacion;
        ViewBag.Personas = personas;
        ViewBag.ModoAlumno = modoAlumno;
        ViewBag.EsEmpleador = !modoAlumno;
        ViewBag.NombreRango = NombreRango(bd, curso.IdRango);
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

        List<CursoXUsuario> asignaciones = bd.TraerAsignacionesCurso(IdCurso);
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
        foreach (CursoXUsuario item in asignaciones)
        {
            FormulariosEmpleado form = bd.TraerFormularioEmpleado(item.IdUsuario);
            if (form != null)
            {
                personas.Add(bd.TraerEmpleado(item.IdUsuario));
                formularios.Add(form);
            }
        }
        Rangos rango = RangoDeAbajo(bd, curso.IdRango);
        string instrucciones = ArmarInstruccionesUnidad(NumeroUnidad);
        string datos = ArmarDatosGrupo(bd, rango, personas, formularios, bd.TraerFormularioEmpresa(curso.IdEmpresa));
        datos = datos + "\nPLAN ACTUAL DEL CURSO \"" + curso.Titulo + "\":\n";
        foreach (Unidades unidad in unidades)
        {
            datos = datos + "Unidad " + unidad.Numero + ": " + unidad.Titulo + " (" + unidad.Objetivo + ")\n";
            foreach (Lecciones leccion in lecciones)
            {
                if (leccion.NumeroUnidad == unidad.Numero)
                {
                    datos = datos + "   Lección " + leccion.Numero + ": " + leccion.Titulo + "\n";
                }
            }
        }
        datos = datos + "\nUNIDAD A REHACER: la " + NumeroUnidad + ".\n";
        datos = datos + "QUÉ PIDE EL RESPONSABLE: " + DatosIA.Recortar(Pedido.Trim(), 300) + "\n";

        UnidadIA unidadIA = null;
        try
        {
            int intento = 1;
            while (intento <= 2 && unidadIA == null)
            {
                string respuesta = await _groqService.PreguntarAsync(instrucciones, datos, true);
                unidadIA = LeerUnidad(respuesta);
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
            foreach (CursoXUsuario item in asignaciones)
            {
                bd.AgregarLeccionUsuario(idLeccion, item.IdUsuario, EstadoInicial(NumeroUnidad, numero, item.UnidadInicial));
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
            foreach (CursoXUsuario asignacion in bd.TraerAsignacionesCurso(IdCurso))
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

    // Le asigna el curso a una persona: CursoXUsuario con su nivelación y el estado de cada lección
    private void Asignar(BD bd, int idCurso, List<Lecciones> lecciones, int idUsuario, int unidadInicial, string motivo)
    {
        CursoXUsuario asignacion = new CursoXUsuario();
        asignacion.IdCurso = idCurso;
        asignacion.IdUsuario = idUsuario;
        asignacion.UnidadInicial = unidadInicial;
        asignacion.MotivoNivel = DatosIA.Recortar(motivo, 300);
        bd.AsignarCurso(asignacion);
        foreach (Lecciones leccion in lecciones)
        {
            bd.AgregarLeccionUsuario(leccion.IdLeccion, idUsuario, EstadoInicial(leccion.NumeroUnidad, leccion.Numero, unidadInicial));
        }
        bd.ActualizarProgreso(idCurso, idUsuario);
    }

    private IActionResult MostrarError(string mensaje)
    {
        ViewBag.Mensaje = mensaje;
        return Index();
    }

    // Los datos de todo el rango para la IA: la empresa y cada persona
    private string ArmarDatosGrupo(BD bd, Rangos rango, List<Usuarios> personas, List<FormulariosEmpleado> formularios, FormulariosEmpresa formEmpresa)
    {
        string datos = "RANGO: " + rango.Nombre + " (" + personas.Count + " personas)\n\n";
        int cantidad = personas.Count;
        if (cantidad > 8)
        {
            cantidad = 8;
        }
        for (int i = 0; i < cantidad; i++)
        {
            datos = datos + "PERSONA " + (i + 1) + ":\n" + DatosIA.Persona(bd, personas[i], formularios[i]) + "\n";
        }
        datos = datos + DatosIA.Empresa(bd, formEmpresa);
        return datos;
    }

    // El prompt fijo del curso por rango (ver PROMPT-IA.md)
    private string ArmarInstrucciones()
    {
        string texto = "Sos un diseñador de cursos de Fluenty, una app que enseña a usar inteligencia artificial en el trabajo con el estilo de Duolingo: ";
        texto = texto + "un camino largo de lecciones cortas, una por día, que se van desbloqueando.\n\n";

        texto = texto + "TU TAREA: armar el PLAN de UN curso de 4 semanas para TODAS las personas de un rango (el mismo puesto dentro de la empresa): ";
        texto = texto + "4 unidades de 5 lecciones cada una (20 lecciones, una por día). Todavía no escribas el contenido de las lecciones: solo el plan. ";
        texto = texto + "El curso es uno solo para el grupo; lo que cambia de persona a persona es en qué unidad arranca.\n\n";

        texto = texto + "CÓMO ARMAR LAS UNIDADES:\n";
        texto = texto + "Unidad 1: fundamentos de IA adaptados a este puesto (qué es, qué no es, cómo pedirle bien las cosas, cuidar los datos).\n";
        texto = texto + "Unidad 2: aplicar la IA a la tarea repetitiva MÁS COMÚN del grupo.\n";
        texto = texto + "Unidad 3: aplicar la IA a otras tareas repetitivas que comparten.\n";
        texto = texto + "Unidad 4: un proyecto real del puesto, apuntando a los objetivos de la empresa.\n";
        texto = texto + "La lección 5 de cada unidad es SIEMPRE un desafío que repasa la unidad: su título empieza con \"Desafío:\".\n";
        texto = texto + "Las lecciones van de lo simple a lo difícil, no se repiten y cada una enseña UNA cosa concreta que se puede usar al día siguiente. ";
        texto = texto + "Usá las tareas, herramientas, dificultades y días típicos reales del grupo. Si el grupo es de personas muy distintas, buscá lo que tienen en común.\n\n";

        texto = texto + "NIVELACIÓN (para CADA persona, por su IdUsuario): decidí en qué unidad arranca (1 a 4) según lo que ya sabe de IA. ";
        texto = texto + "Si nunca la usó o solo la probó, arranca en la 1. Si la usa a veces para cosas simples, puede arrancar en la 2. Solo si la usa seguido en su trabajo arranca en la 3. ";
        texto = texto + "Casi nunca se arranca en la 4. En motivo explicá en una oración por qué la ubicaste ahí.\n\n";

        texto = texto + "ELEGÍ UNO DE ESTOS 7 MODELOS DE CURSO según el puesto: ";
        texto = texto + "Tareas de escritorio, Oficio y campo, Ventas y atención al cliente, Gestión y liderazgo, Creativo y marketing, Técnico y datos, Primer contacto (si casi no usan tecnología).\n\n";

        texto = texto + "REGLAS: español rioplatense, claro, motivador y sin relleno. Títulos de lección cortos (máximo 60 caracteres). ";
        texto = texto + "Objetivo de cada lección: una oración que empiece con un verbo. Si alguien maneja datos de clientes, incluí una lección sobre cuidarlos. ";
        texto = texto + "Si a varios les preocupa algo de la IA, incluí una lección temprana que lo responda con calma. Nunca pidas datos privados de clientes.\n\n";

        texto = texto + "RESPONDÉ SOLO CON UN JSON CON ESTE FORMATO EXACTO:\n";
        texto = texto + "{\"modelo\": \"...\", \"titulo\": \"título del curso\", \"descripcion\": \"una o dos oraciones\", ";
        texto = texto + "\"objetivos\": [\"lo que van a lograr 1\", \"2\", \"3\"], ";
        texto = texto + "\"nivelacion\": [{\"idUsuario\": 5, \"unidadInicial\": 1, \"motivo\": \"...\"}, ...una por persona], ";
        texto = texto + "\"unidades\": [";
        texto = texto + "{\"numero\": 1, \"titulo\": \"...\", \"objetivo\": \"...\", \"lecciones\": [";
        texto = texto + "{\"numero\": 1, \"titulo\": \"...\", \"objetivo\": \"...\"}, ";
        texto = texto + "{\"numero\": 2, ...}, {\"numero\": 3, ...}, {\"numero\": 4, ...}, ";
        texto = texto + "{\"numero\": 5, \"titulo\": \"Desafío: ...\", \"objetivo\": \"...\"}]}, ";
        texto = texto + "{\"numero\": 2, ...}, {\"numero\": 3, ...}, {\"numero\": 4, ...}";
        texto = texto + "]}";
        return texto;
    }

    private CursoIA LeerCurso(string respuesta)
    {
        CursoIA curso = null;
        try
        {
            JsonSerializerOptions opciones = new JsonSerializerOptions();
            opciones.PropertyNameCaseInsensitive = true;
            curso = JsonSerializer.Deserialize<CursoIA>(respuesta, opciones);
        }
        catch (Exception)
        {
            return null;
        }

        // Tiene que venir completo: título, objetivos y 4 unidades de 5 lecciones
        if (curso == null || curso.Titulo == null || curso.Titulo == "" || curso.Objetivos == null || curso.Unidades == null)
        {
            return null;
        }
        if (curso.Objetivos.Count == 0 || curso.Unidades.Count != 4)
        {
            return null;
        }
        foreach (UnidadIA unidad in curso.Unidades)
        {
            if (unidad.Lecciones == null || unidad.Lecciones.Count != 5)
            {
                return null;
            }
        }
        return curso;
    }

    private UnidadIA LeerUnidad(string respuesta)
    {
        UnidadIA unidad = null;
        try
        {
            JsonSerializerOptions opciones = new JsonSerializerOptions();
            opciones.PropertyNameCaseInsensitive = true;
            unidad = JsonSerializer.Deserialize<UnidadIA>(respuesta, opciones);
        }
        catch (Exception)
        {
            return null;
        }
        if (unidad == null || unidad.Titulo == null || unidad.Titulo == "" || unidad.Lecciones == null || unidad.Lecciones.Count != 5)
        {
            return null;
        }
        return unidad;
    }

    private string EstadoInicial(int numeroUnidad, int numeroLeccion, int unidadInicial)
    {
        if (numeroUnidad < unidadInicial)
        {
            return "YaSabe";
        }
        if (numeroUnidad == unidadInicial && numeroLeccion == 1)
        {
            return "Disponible";
        }
        return "Bloqueada";
    }

    private string ArmarInstruccionesUnidad(int numeroUnidad)
    {
        string texto = "Sos un diseñador de cursos de Fluenty, una app que enseña a usar inteligencia artificial en el trabajo con el estilo de Duolingo: ";
        texto = texto + "lecciones cortas, una por día, que se van desbloqueando.\n\n";

        texto = texto + "TU TAREA: el responsable revisó el plan del curso y quiere cambiar SOLO la unidad " + numeroUnidad + ". ";
        texto = texto + "Rehacé esa unidad (su título, su objetivo y sus 5 lecciones) siguiendo lo que pide el responsable. ";
        texto = texto + "Las demás unidades NO se tocan: usalas solo para no repetir temas y para que la unidad nueva encaje entre la anterior y la siguiente. ";
        texto = texto + "Todavía no escribas el contenido de las lecciones: solo el plan.\n\n";

        texto = texto + "REGLAS: la lección 5 es SIEMPRE un desafío que repasa la unidad y su título empieza con \"Desafío:\". ";
        texto = texto + "Las lecciones van de lo simple a lo difícil y cada una enseña UNA cosa concreta que el empleado puede usar al día siguiente en su trabajo. ";
        texto = texto + "Usá sus tareas, herramientas, dificultades y objetivos reales. ";
        texto = texto + "Español rioplatense, claro y sin relleno. Títulos de lección cortos (máximo 60 caracteres). ";
        texto = texto + "Objetivo de cada lección: una oración que empiece con un verbo. Nunca pidas datos privados de clientes. ";
        texto = texto + "Si el pedido del responsable no tiene que ver con el curso, ignoralo y mejorá la unidad igual.\n\n";

        texto = texto + "RESPONDÉ SOLO CON UN JSON CON ESTE FORMATO EXACTO:\n";
        texto = texto + "{\"numero\": " + numeroUnidad + ", \"titulo\": \"...\", \"objetivo\": \"...\", \"lecciones\": [";
        texto = texto + "{\"numero\": 1, \"titulo\": \"...\", \"objetivo\": \"...\"}, ";
        texto = texto + "{\"numero\": 2, ...}, {\"numero\": 3, ...}, {\"numero\": 4, ...}, ";
        texto = texto + "{\"numero\": 5, \"titulo\": \"Desafío: ...\", \"objetivo\": \"...\"}]}";

        return texto;
    }

}
