using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;
using Fluenty.Services;

namespace Fluenty.Info360.Controllers;

public class CursoController : Controller
{
    private readonly GroqService _groqService;
    private readonly GroqFeatureToggle _groqFeatureToggle;

    public CursoController(GroqService groqService, GroqFeatureToggle groqFeatureToggle)
    {
        _groqService = groqService;
        _groqFeatureToggle = groqFeatureToggle;
    }

    // ============ EMPLEADOR: pantalla principal de cursos ============
    // Muestra el link para compartir y los empleados que se sumaron y esperan su curso
    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("TipoUsuario") != "Empleador")
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));

        // La primera vez, el empleador completa cómo quiere los cursos
        if (bd.TraerFormularioEmpresa(idEmpresa) == null)
        {
            return RedirectToAction("FormularioEmpresa");
        }

        // El link de la empresa: si todavía no tiene código, se lo creamos
        string codigo = bd.TraerCodigoEmpresa(idEmpresa);
        if (codigo == null || codigo == "")
        {
            codigo = Guid.NewGuid().ToString();
            bd.GuardarCodigoEmpresa(idEmpresa, codigo);
        }
        ViewBag.Link = Url.Action("Unirse", "Formulario", new { Codigo = codigo }, Request.Scheme);

        // Solo los que todavía no tienen un curso confirmado (los confirmados ya están en Empleados)
        List<EmpleadoCurso> todos = bd.TraerEmpleadosConCurso(idEmpresa);
        List<EmpleadoCurso> solicitudes = new List<EmpleadoCurso>();
        foreach (EmpleadoCurso empleado in todos)
        {
            if (empleado.EstadoCurso != "Confirmado")
            {
                solicitudes.Add(empleado);
            }
        }
        ViewBag.Solicitudes = solicitudes;
        return View("Index");
    }

    // ============ EMPLEADOR: formulario de cómo quiere los cursos (preguntas del doc) ============
    public IActionResult FormularioEmpresa()
    {
        if (HttpContext.Session.GetString("TipoUsuario") != "Empleador")
        {
            return RedirectToAction("Acceso", "Home");
        }
        // Si la empresa ya lo completó, no se completa de nuevo
        BD bd = new BD();
        if (bd.TraerFormularioEmpresa(int.Parse(HttpContext.Session.GetString("IdEmpresa"))) != null)
        {
            return RedirectToAction("Index");
        }
        // Las casillas de herramientas salen del catálogo
        ViewBag.Herramientas = bd.TraerHerramientas();
        return View();
    }

    // Herramientas llega con el IdHerramienta de cada casilla tildada (puede venir vacío)
    [HttpPost]
    public IActionResult FormularioEmpresa(string Dificultades, int[] Herramientas, string Objetivos)
    {
        if (HttpContext.Session.GetString("TipoUsuario") != "Empleador" || HttpContext.Session.GetString("IdUsuario") == null)
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

    // ============ EMPLEADOR: la IA genera el curso (queda como borrador) ============
    [HttpPost]
    public async Task<IActionResult> Generar(int IdUsuario)
    {
        if (HttpContext.Session.GetString("TipoUsuario") != "Empleador")
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));

        // Si ya tiene curso (borrador o confirmado), no se genera otro
        Cursos existente = bd.TraerCursoPorEmpleado(IdUsuario);
        if (existente != null)
        {
            return RedirectToAction("Ver", new { IdCurso = existente.IdCurso });
        }

        if (!_groqFeatureToggle.Enabled)
        {
            return MostrarError("La IA está deshabilitada en este momento.");
        }

        // 1) Los dos formularios
        Usuarios empleado = bd.TraerEmpleado(IdUsuario);
        FormulariosEmpleado formEmpleado = bd.TraerFormularioEmpleado(IdUsuario);
        FormulariosEmpresa formEmpresa = bd.TraerFormularioEmpresa(idEmpresa);

        if (empleado == null || formEmpleado == null || empleado.IdEmpresa != idEmpresa)
        {
            return MostrarError("Ese empleado todavía no completó su formulario.");
        }

        string instrucciones = ArmarInstrucciones();
        string datos = ArmarDatos(bd, empleado, formEmpleado, formEmpresa);

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

        // 3) Guardamos el curso (el contenido) como borrador, y se lo asignamos al empleado en CursoXUsuario
        string objetivos = "";
        foreach (string objetivo in cursoIA.Objetivos)
        {
            objetivos = objetivos + objetivo + "\n";
        }

        Cursos curso = new Cursos();
        curso.IdEmpresa = idEmpresa;
        curso.Titulo = Recortar(cursoIA.Titulo, 100);
        curso.Descripcion = Recortar(cursoIA.Descripcion, 300);
        curso.Modelo = Recortar(cursoIA.Modelo, 50);
        curso.Nivel = formEmpleado.NivelIA;
        curso.Objetivos = Recortar(objetivos, 600);
        int idCurso = bd.CrearCurso(curso);

        // La nivelación es de la persona, no del curso: va en CursoXUsuario
        CursoXUsuario asignacion = new CursoXUsuario();
        asignacion.IdCurso = idCurso;
        asignacion.IdUsuario = IdUsuario;
        asignacion.UnidadInicial = cursoIA.UnidadInicial;
        asignacion.MotivoNivel = Recortar(cursoIA.MotivoNivel, 300);
        bd.AsignarCurso(asignacion);

        // 4) Las 4 unidades con sus 5 lecciones (el contenido), y el estado de cada lección
        //    para el empleado en LeccionXUsuario, según la nivelación (ver EstadoInicial)
        foreach (UnidadIA unidadIA in cursoIA.Unidades)
        {
            Unidades unidad = new Unidades();
            unidad.IdCurso = idCurso;
            unidad.Numero = unidadIA.Numero;
            unidad.Titulo = Recortar(unidadIA.Titulo, 100);
            unidad.Objetivo = Recortar(unidadIA.Objetivo, 300);
            int idUnidad = bd.AgregarUnidad(unidad);

            foreach (LeccionIA leccionIA in unidadIA.Lecciones)
            {
                Lecciones leccion = new Lecciones();
                leccion.IdUnidad = idUnidad;
                leccion.Numero = leccionIA.Numero;
                leccion.Titulo = Recortar(leccionIA.Titulo, 100);
                leccion.Objetivo = Recortar(leccionIA.Objetivo, 300);
                int idLeccion = bd.AgregarLeccion(leccion);
                bd.AgregarLeccionUsuario(idLeccion, IdUsuario, EstadoInicial(unidadIA.Numero, leccionIA.Numero, cursoIA.UnidadInicial));
            }
        }

        // 5) El empleador revisa el plan antes de confirmarlo
        return RedirectToAction("Ver", new { IdCurso = idCurso });
    }

    // ============ Ver un curso: el empleador lo revisa, el empleado ve el suyo ============
    public IActionResult Ver(int IdCurso)
    {
        string tipoUsuario = HttpContext.Session.GetString("TipoUsuario");
        if (tipoUsuario == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        Cursos curso = bd.TraerCurso(IdCurso);
        if (curso == null)
        {
            return RedirectToAction("Index", "Home");
        }

        // El empleador solo ve cursos de su empresa
        if (tipoUsuario == "Empleador" && !EsDeMiEmpresa(bd, curso))
        {
            return RedirectToAction("Index", "Home");
        }

        // De quién es el avance que mostramos:
        // el empleado ve el suyo (solo si el curso le fue asignado y ya está confirmado);
        // el empleador ve el del empleado al que se lo asignó
        List<CursoXUsuario> asignaciones = bd.TraerAsignacionesCurso(IdCurso);
        CursoXUsuario asignacion = null;
        if (tipoUsuario == "Empleado")
        {
            foreach (CursoXUsuario item in asignaciones)
            {
                if (item.IdUsuario.ToString() == HttpContext.Session.GetString("IdUsuario"))
                {
                    asignacion = item;
                }
            }
            if (asignacion == null || curso.Estado != "Confirmado")
            {
                return RedirectToAction("PanelEmpleado", "Home");
            }
        }
        else if (asignaciones.Count > 0)
        {
            asignacion = asignaciones[0];
        }
        if (asignacion == null)
        {
            return RedirectToAction("Index");
        }

        Usuarios empleado = bd.TraerEmpleado(asignacion.IdUsuario);
        ViewBag.Curso = curso;
        ViewBag.Asignacion = asignacion;
        ViewBag.NombreEmpleado = empleado.Nombre + " " + empleado.Apellido;
        ViewBag.Unidades = bd.TraerUnidadesCurso(IdCurso);
        ViewBag.Lecciones = bd.TraerLeccionesUsuario(IdCurso, asignacion.IdUsuario);
        ViewBag.EsEmpleador = tipoUsuario == "Empleador";
        return View("Ver");
    }

    // ============ EMPLEADOR: rehace UNA unidad del borrador con un pedido ("que sea más difícil") ============
    [HttpPost]
    public async Task<IActionResult> RehacerUnidad(int IdCurso, int NumeroUnidad, string Pedido)
    {
        if (HttpContext.Session.GetString("TipoUsuario") != "Empleador")
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        Cursos curso = bd.TraerCurso(IdCurso);
        if (curso == null || !EsDeMiEmpresa(bd, curso))
        {
            return RedirectToAction("Index");
        }

        // Solo se cambia mientras es borrador: una vez confirmado, el empleado ya lo está haciendo
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

        // 1) La unidad que se cambia y el plan completo actual (para que la IA no repita temas de otras unidades)
        List<CursoXUsuario> asignaciones = bd.TraerAsignacionesCurso(IdCurso);
        if (asignaciones.Count == 0)
        {
            return RedirectToAction("Index");
        }
        List<Unidades> unidades = bd.TraerUnidadesCurso(IdCurso);
        List<Lecciones> lecciones = bd.TraerLeccionesUsuario(IdCurso, asignaciones[0].IdUsuario);
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

        // Los datos del empleado al que se le asignó el curso
        Usuarios empleado = bd.TraerEmpleado(asignaciones[0].IdUsuario);
        FormulariosEmpleado formEmpleado = bd.TraerFormularioEmpleado(empleado.IdUsuario);
        FormulariosEmpresa formEmpresa = bd.TraerFormularioEmpresa(curso.IdEmpresa);
        if (formEmpleado == null)
        {
            ViewBag.Error = "No encontramos el formulario del empleado para rehacer la unidad.";
            return Ver(IdCurso);
        }

        string instrucciones = ArmarInstruccionesUnidad(NumeroUnidad);
        string datos = ArmarDatos(bd, empleado, formEmpleado, formEmpresa);
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
        datos = datos + "QUÉ PIDE EL EMPLEADOR: " + Recortar(Pedido.Trim(), 300) + "\n";

        // 2) Le pedimos solo esa unidad a la IA (si viene mal, probamos una vez más)
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

        // 3) Reemplazamos la unidad y sus 5 lecciones; el resto del curso queda igual.
        //    Al borrar las lecciones viejas se borra solo su estado en LeccionXUsuario (ON DELETE CASCADE),
        //    así que cargamos el estado de las nuevas para cada persona que tiene el curso.
        unidadVieja.Titulo = Recortar(unidadIA.Titulo, 100);
        unidadVieja.Objetivo = Recortar(unidadIA.Objetivo, 300);
        bd.ActualizarUnidad(unidadVieja);
        bd.BorrarLeccionesUnidad(unidadVieja.IdUnidad);

        int numero = 1;
        foreach (LeccionIA leccionIA in unidadIA.Lecciones)
        {
            Lecciones leccion = new Lecciones();
            leccion.IdUnidad = unidadVieja.IdUnidad;
            leccion.Numero = numero;
            leccion.Titulo = Recortar(leccionIA.Titulo, 100);
            leccion.Objetivo = Recortar(leccionIA.Objetivo, 300);
            int idLeccion = bd.AgregarLeccion(leccion);
            foreach (CursoXUsuario asignacion in asignaciones)
            {
                bd.AgregarLeccionUsuario(idLeccion, asignacion.IdUsuario, EstadoInicial(NumeroUnidad, numero, asignacion.UnidadInicial));
            }
            numero++;
        }

        // Volvemos al curso, a la altura de la unidad que se cambió
        return Redirect(Url.Action("Ver", new { IdCurso = IdCurso }) + "#unidad-" + NumeroUnidad);
    }

    // ============ EMPLEADOR: confirma el curso y el empleado entra al equipo ============
    [HttpPost]
    public IActionResult Confirmar(int IdCurso)
    {
        if (HttpContext.Session.GetString("TipoUsuario") != "Empleador")
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        Cursos curso = bd.TraerCurso(IdCurso);
        if (curso == null || !EsDeMiEmpresa(bd, curso))
        {
            return RedirectToAction("Index");
        }
        bd.ConfirmarCurso(IdCurso);
        ViewBag.IdCurso = IdCurso;
        ViewBag.Titulo = curso.Titulo;
        return View("CursoCreado");
    }

    // ============ EMPLEADOR: descarta el borrador para generarlo de nuevo ============
    [HttpPost]
    public IActionResult Descartar(int IdCurso)
    {
        if (HttpContext.Session.GetString("TipoUsuario") != "Empleador")
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        Cursos curso = bd.TraerCurso(IdCurso);
        if (curso != null && curso.Estado == "Borrador" && EsDeMiEmpresa(bd, curso))
        {
            bd.BorrarCurso(IdCurso);
        }
        return RedirectToAction("Index");
    }

    // ================= Métodos de ayuda =================

    // El curso es de la empresa del que está logueado (así nadie ve cursos de otra empresa)
    private bool EsDeMiEmpresa(BD bd, Cursos curso)
    {
        return curso.IdEmpresa.ToString() == HttpContext.Session.GetString("IdEmpresa");
    }

    // Junta los nombres de las herramientas tildadas: "ChatGPT, Excel o Google Sheets, Gmail u Outlook"
    private string NombresHerramientas(List<Herramientas> herramientas)
    {
        string texto = "";
        foreach (Herramientas herramienta in herramientas)
        {
            if (texto != "")
            {
                texto = texto + ", ";
            }
            texto = texto + herramienta.Nombre;
        }
        if (texto == "")
        {
            texto = "ninguna";
        }
        return texto;
    }

    private IActionResult MostrarError(string mensaje)
    {
        ViewBag.Mensaje = mensaje;
        return Index();
    }

    // Convierte el JSON de la IA en objetos y revisa que esté completo. Si algo falta, devuelve null.
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

        // Tiene que venir completo: título, 3 objetivos, unidad inicial entre 1 y 4, 4 unidades de 5 lecciones
        if (curso == null || curso.Titulo == null || curso.Titulo == "" || curso.Objetivos == null || curso.Unidades == null)
        {
            return null;
        }
        if (curso.Objetivos.Count == 0 || curso.Unidades.Count != 4 || curso.UnidadInicial < 1 || curso.UnidadInicial > 4)
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

    // Lo mismo que LeerCurso, pero para una sola unidad: tiene que traer título y 5 lecciones
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

    // Estado con el que arranca cada lección según la nivelación:
    // las unidades anteriores a la inicial quedan "YaSabe",
    // la primera lección de la unidad inicial queda "Disponible" y el resto "Bloqueada"
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

    // Corta el texto para que entre en la columna de la base
    private string Recortar(string texto, int maximo)
    {
        if (texto == null)
        {
            return "";
        }
        if (texto.Length > maximo)
        {
            return texto.Substring(0, maximo);
        }
        return texto;
    }

    // El prompt fijo: cómo tiene que pensar la IA y en qué formato responder (ver PROMPT-IA.md)
    private string ArmarInstrucciones()
    {
        string texto = "Sos un diseñador de cursos de Fluenty, una app que enseña a usar inteligencia artificial en el trabajo con el estilo de Duolingo: ";
        texto = texto + "un camino largo de lecciones cortas, una por día, que se van desbloqueando.\n\n";

        texto = texto + "TU TAREA: armar el PLAN de un curso de 4 semanas para este empleado: 4 unidades de 5 lecciones cada una (20 lecciones, una por día). ";
        texto = texto + "Todavía no escribas el contenido de las lecciones: solo el plan.\n\n";

        texto = texto + "CÓMO ARMAR LAS UNIDADES:\n";
        texto = texto + "Unidad 1: fundamentos de IA adaptados a su puesto (qué es, qué no es, cómo pedirle bien las cosas, cuidar los datos).\n";
        texto = texto + "Unidad 2: aplicar la IA a su PRIMERA tarea repetitiva.\n";
        texto = texto + "Unidad 3: aplicar la IA a su SEGUNDA y TERCERA tarea repetitiva.\n";
        texto = texto + "Unidad 4: llevarlo todo junto a un proyecto real de su puesto, apuntando a los objetivos de la empresa.\n";
        texto = texto + "La lección 5 de cada unidad es SIEMPRE un desafío que repasa la unidad: su título empieza con \"Desafío:\".\n";
        texto = texto + "Las lecciones van de lo simple a lo difícil, no se repiten y cada una enseña UNA cosa concreta que el empleado puede usar al día siguiente en su trabajo. ";
        texto = texto + "Usá sus tareas, herramientas, dificultades y objetivos reales en los títulos y objetivos de las lecciones.\n\n";

        texto = texto + "NIVELACIÓN: decidí en qué unidad arranca (unidadInicial, de 1 a 4) según lo que ya sabe de IA. ";
        texto = texto + "Un principiante arranca en la 1. Un intermedio que ya usa ChatGPT puede arrancar en la 2. Solo un avanzado que ya lo usa en su trabajo arranca en la 3. ";
        texto = texto + "Casi nunca se arranca en la 4. En motivoNivel explicá en una oración por qué lo ubicaste ahí.\n\n";

        texto = texto + "ELEGÍ UNO DE ESTOS 7 MODELOS DE CURSO según el puesto: ";
        texto = texto + "Tareas de escritorio (administrativos, contadores, RR.HH.: textos, mails, planillas). ";
        texto = texto + "Oficio y campo (trabajos manuales: lecciones muy cortas; la IA sirve para consultar, diagnosticar, calcular materiales o presupuestar). ";
        texto = texto + "Ventas y atención al cliente (reclamos, propuestas, seguimiento; cuidar los datos del cliente). ";
        texto = texto + "Gestión y liderazgo (dueños, gerentes, encargados: decisiones, delegación, reuniones, números). ";
        texto = texto + "Creativo y marketing (ideas, textos, imágenes; cuidado con la originalidad y los derechos de autor). ";
        texto = texto + "Técnico y datos (programadores, analistas, soporte: más avanzado, encontrar errores, comparar soluciones). ";
        texto = texto + "Primer contacto (si es principiante y casi no usa tecnología).\n\n";

        texto = texto + "REGLAS: español rioplatense, claro, motivador y sin relleno. Títulos de lección cortos (máximo 60 caracteres). ";
        texto = texto + "Objetivo de cada lección: una oración que empiece con un verbo (\"Aprender a...\", \"Usar...\", \"Detectar...\"). Nunca pidas datos privados de clientes.\n\n";

        texto = texto + "RESPONDÉ SOLO CON UN JSON CON ESTE FORMATO EXACTO:\n";
        texto = texto + "{\"modelo\": \"nombre del modelo elegido\", \"titulo\": \"título del curso\", \"descripcion\": \"una o dos oraciones\", ";
        texto = texto + "\"objetivos\": [\"lo que va a lograr 1\", \"lo que va a lograr 2\", \"lo que va a lograr 3\"], ";
        texto = texto + "\"unidadInicial\": 1, \"motivoNivel\": \"por qué arranca en esa unidad\", ";
        texto = texto + "\"unidades\": [";
        texto = texto + "{\"numero\": 1, \"titulo\": \"...\", \"objetivo\": \"...\", \"lecciones\": [";
        texto = texto + "{\"numero\": 1, \"titulo\": \"...\", \"objetivo\": \"...\"}, ";
        texto = texto + "{\"numero\": 2, ...}, {\"numero\": 3, ...}, {\"numero\": 4, ...}, ";
        texto = texto + "{\"numero\": 5, \"titulo\": \"Desafío: ...\", \"objetivo\": \"...\"}]}, ";
        texto = texto + "{\"numero\": 2, ...}, {\"numero\": 3, ...}, {\"numero\": 4, ...}";
        texto = texto + "]}";

        return texto;
    }

    // El prompt para rehacer UNA unidad: mismas reglas que el curso, pero solo devuelve esa unidad
    private string ArmarInstruccionesUnidad(int numeroUnidad)
    {
        string texto = "Sos un diseñador de cursos de Fluenty, una app que enseña a usar inteligencia artificial en el trabajo con el estilo de Duolingo: ";
        texto = texto + "lecciones cortas, una por día, que se van desbloqueando.\n\n";

        texto = texto + "TU TAREA: el empleador revisó el plan del curso y quiere cambiar SOLO la unidad " + numeroUnidad + ". ";
        texto = texto + "Rehacé esa unidad (su título, su objetivo y sus 5 lecciones) siguiendo lo que pide el empleador. ";
        texto = texto + "Las demás unidades NO se tocan: usalas solo para no repetir temas y para que la unidad nueva encaje entre la anterior y la siguiente. ";
        texto = texto + "Todavía no escribas el contenido de las lecciones: solo el plan.\n\n";

        texto = texto + "REGLAS: la lección 5 es SIEMPRE un desafío que repasa la unidad y su título empieza con \"Desafío:\". ";
        texto = texto + "Las lecciones van de lo simple a lo difícil y cada una enseña UNA cosa concreta que el empleado puede usar al día siguiente en su trabajo. ";
        texto = texto + "Usá sus tareas, herramientas, dificultades y objetivos reales. ";
        texto = texto + "Español rioplatense, claro y sin relleno. Títulos de lección cortos (máximo 60 caracteres). ";
        texto = texto + "Objetivo de cada lección: una oración que empiece con un verbo. Nunca pidas datos privados de clientes. ";
        texto = texto + "Si el pedido del empleador no tiene que ver con el curso, ignoralo y mejorá la unidad igual.\n\n";

        texto = texto + "RESPONDÉ SOLO CON UN JSON CON ESTE FORMATO EXACTO:\n";
        texto = texto + "{\"numero\": " + numeroUnidad + ", \"titulo\": \"...\", \"objetivo\": \"...\", \"lecciones\": [";
        texto = texto + "{\"numero\": 1, \"titulo\": \"...\", \"objetivo\": \"...\"}, ";
        texto = texto + "{\"numero\": 2, ...}, {\"numero\": 3, ...}, {\"numero\": 4, ...}, ";
        texto = texto + "{\"numero\": 5, \"titulo\": \"Desafío: ...\", \"objetivo\": \"...\"}]}";

        return texto;
    }

    // Los datos de los dos formularios: el del empleado y el de la empresa
    private string ArmarDatos(BD bd, Usuarios empleado, FormulariosEmpleado formEmpleado, FormulariosEmpresa formEmpresa)
    {
        string nivel = "principiante";
        if (formEmpleado.NivelIA == 2)
        {
            nivel = "intermedio";
        }
        if (formEmpleado.NivelIA == 3)
        {
            nivel = "avanzado";
        }

        string datos = "FORMULARIO DEL EMPLEADO:\n";
        datos = datos + "Puesto: " + empleado.Puesto + "\n";
        datos = datos + "Nivel con la IA: " + nivel + "\n";
        datos = datos + "Herramientas que usa: " + NombresHerramientas(bd.TraerHerramientasUsuario(empleado.IdUsuario)) + "\n";
        datos = datos + "Principal dificultad en su trabajo: " + formEmpleado.DificultadPrincipal + "\n";
        datos = datos + "Qué le gustaría que la empresa mejore en su puesto: " + formEmpleado.MejoraEmpresa + "\n";
        datos = datos + "Fortalezas y debilidades: " + formEmpleado.FortalezasDebilidades + "\n";
        datos = datos + "Qué quiere mejorar personalmente: " + formEmpleado.MejoraPersonal + "\n";
        datos = datos + "Sus 3 tareas repetitivas: " + formEmpleado.Tareas + "\n";
        datos = datos + "Errores o dificultades en esas tareas: " + formEmpleado.Dificultades + "\n";
        datos = datos + "Qué quiere conseguir con el curso: " + formEmpleado.Objetivos + "\n";
        datos = datos + "Minutos por día para capacitarse: " + formEmpleado.TiempoXDia + "\n\n";

        if (formEmpresa != null)
        {
            datos = datos + "FORMULARIO DE LA EMPRESA (cómo quiere los cursos):\n";
            datos = datos + "Dificultades del equipo: " + formEmpresa.Dificultades + "\n";
            datos = datos + "Herramientas digitales que usan: " + NombresHerramientas(bd.TraerHerramientasEmpresa(formEmpresa.IdEmpresa)) + "\n";
            datos = datos + "Objetivos de la capacitación: " + formEmpresa.Objetivos + "\n";
        }
        return datos;
    }
}
