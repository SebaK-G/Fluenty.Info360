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
        if (bd.TraerFormularioEmpleadorEmpresa(idEmpresa) == null)
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
        return View();
    }

    [HttpPost]
    public IActionResult FormularioEmpresa(string Dificultades, string Herramientas, string Objetivos)
    {
        if (HttpContext.Session.GetString("TipoUsuario") != "Empleador" || HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        FormulariosEmpleador formulario = new FormulariosEmpleador();
        formulario.IdUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        formulario.Dificultades = Dificultades;
        formulario.Herramientas = Herramientas;
        formulario.Objetivos = Objetivos;

        BD bd = new BD();
        bd.GuardarFormularioEmpleador(formulario);
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
        FormulariosEmpleador formEmpleador = bd.TraerFormularioEmpleadorEmpresa(idEmpresa);

        if (empleado == null || formEmpleado == null)
        {
            return MostrarError("Ese empleado todavía no completó su formulario.");
        }

        string instrucciones = ArmarInstrucciones();
        string datos = ArmarDatos(empleado, formEmpleado, formEmpleador);

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

        // 3) Guardamos el curso como borrador, asignado al empleado por IdUsuario
        string objetivos = "";
        foreach (string objetivo in cursoIA.Objetivos)
        {
            objetivos = objetivos + objetivo + "\n";
        }

        Cursos curso = new Cursos();
        curso.IdUsuario = IdUsuario;
        curso.Titulo = Recortar(cursoIA.Titulo, 100);
        curso.Descripcion = Recortar(cursoIA.Descripcion, 300);
        curso.Modelo = Recortar(cursoIA.Modelo, 50);
        curso.Nivel = formEmpleado.NivelIA;
        curso.Objetivos = Recortar(objetivos, 600);
        curso.UnidadInicial = cursoIA.UnidadInicial;
        curso.MotivoNivel = Recortar(cursoIA.MotivoNivel, 300);
        int idCurso = bd.CrearCurso(curso);

        // 4) Las 4 unidades con sus 5 lecciones. La nivelación decide el estado de cada lección:
        //    las unidades anteriores a la inicial quedan como "YaSabe",
        //    la primera lección de la unidad inicial queda "Disponible" y el resto "Bloqueada"
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
                leccion.Estado = "Bloqueada";
                if (unidadIA.Numero < cursoIA.UnidadInicial)
                {
                    leccion.Estado = "YaSabe";
                }
                if (unidadIA.Numero == cursoIA.UnidadInicial && leccionIA.Numero == 1)
                {
                    leccion.Estado = "Disponible";
                }
                bd.AgregarLeccion(leccion);
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

        // Un empleado solo puede ver su propio curso, y solo si ya está confirmado
        if (tipoUsuario == "Empleado")
        {
            if (curso.IdUsuario.ToString() != HttpContext.Session.GetString("IdUsuario") || curso.Estado != "Confirmado")
            {
                return RedirectToAction("PanelEmpleado", "Home");
            }
        }

        ViewBag.Curso = curso;
        ViewBag.Unidades = bd.TraerUnidadesCurso(IdCurso);
        ViewBag.Lecciones = bd.TraerLeccionesCurso(IdCurso);
        ViewBag.EsEmpleador = tipoUsuario == "Empleador";
        return View();
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
        bd.ConfirmarCurso(IdCurso);
        Cursos curso = bd.TraerCurso(IdCurso);
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
        if (curso != null && curso.Estado == "Borrador")
        {
            bd.BorrarCurso(IdCurso);
        }
        return RedirectToAction("Index");
    }

    // ================= Métodos de ayuda =================

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

    // Los datos de los dos formularios: el del empleado y el de la empresa
    private string ArmarDatos(Usuarios empleado, FormulariosEmpleado formEmpleado, FormulariosEmpleador formEmpleador)
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
        datos = datos + "Herramientas que usa: " + formEmpleado.Herramientas + "\n";
        datos = datos + "Principal dificultad en su trabajo: " + formEmpleado.DificultadPrincipal + "\n";
        datos = datos + "Qué le gustaría que la empresa mejore en su puesto: " + formEmpleado.MejoraEmpresa + "\n";
        datos = datos + "Fortalezas y debilidades: " + formEmpleado.FortalezasDebilidades + "\n";
        datos = datos + "Qué quiere mejorar personalmente: " + formEmpleado.MejoraPersonal + "\n";
        datos = datos + "Sus 3 tareas repetitivas: " + formEmpleado.Tareas + "\n";
        datos = datos + "Errores o dificultades en esas tareas: " + formEmpleado.Dificultades + "\n";
        datos = datos + "Qué quiere conseguir con el curso: " + formEmpleado.Objetivos + "\n";
        datos = datos + "Minutos por día para capacitarse: " + formEmpleado.TiempoXDia + "\n\n";

        if (formEmpleador != null)
        {
            datos = datos + "FORMULARIO DE LA EMPRESA (cómo quiere los cursos):\n";
            datos = datos + "Dificultades del equipo: " + formEmpleador.Dificultades + "\n";
            datos = datos + "Herramientas digitales que usan: " + formEmpleador.Herramientas + "\n";
            datos = datos + "Objetivos de la capacitación: " + formEmpleador.Objetivos + "\n";
        }
        return datos;
    }
}
