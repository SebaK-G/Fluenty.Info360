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
        Cursos curso = new Cursos();
        curso.IdUsuario = IdUsuario;
        curso.Titulo = Recortar(cursoIA.Titulo, 100);
        curso.Descripcion = Recortar(cursoIA.Descripcion, 300);
        curso.Modelo = Recortar(cursoIA.Modelo, 50);
        curso.Nivel = formEmpleado.NivelIA;
        int idCurso = bd.CrearCurso(curso);

        foreach (ParteIA parteIA in cursoIA.Partes)
        {
            PartesCurso parte = new PartesCurso();
            parte.IdCurso = idCurso;
            parte.Numero = parteIA.Numero;
            parte.Titulo = Recortar(parteIA.Titulo, 100);
            parte.Contenido = Recortar(parteIA.Contenido, 4000);
            bd.AgregarParteCurso(parte);

            foreach (PreguntaIA preguntaIA in parteIA.Preguntas)
            {
                Preguntas pregunta = new Preguntas();
                pregunta.IdCurso = idCurso;
                pregunta.NumeroParte = parteIA.Numero;
                pregunta.Texto = Recortar(preguntaIA.Texto, 300);
                pregunta.OpcionA = Recortar(preguntaIA.OpcionA, 200);
                pregunta.OpcionB = Recortar(preguntaIA.OpcionB, 200);
                pregunta.OpcionC = Recortar(preguntaIA.OpcionC, 200);
                pregunta.Correcta = preguntaIA.Correcta;
                bd.AgregarPregunta(pregunta);
            }
        }

        // 4) El empleador lo revisa antes de confirmarlo
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
        ViewBag.Partes = bd.TraerPartesCurso(IdCurso);
        ViewBag.Preguntas = bd.TraerPreguntasCurso(IdCurso);
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

        if (curso == null || curso.Titulo == null || curso.Titulo == "" || curso.Partes == null || curso.Partes.Count != 5)
        {
            return null;
        }
        foreach (ParteIA parte in curso.Partes)
        {
            if (parte.Preguntas == null)
            {
                parte.Preguntas = new List<PreguntaIA>();
            }
            if ((parte.Numero == 2 || parte.Numero == 4) && parte.Preguntas.Count == 0)
            {
                return null;
            }
            foreach (PreguntaIA pregunta in parte.Preguntas)
            {
                if (pregunta.Correcta == null)
                {
                    return null;
                }
                pregunta.Correcta = pregunta.Correcta.ToUpper().Replace(" ", "");
                if (pregunta.Correcta != "A" && pregunta.Correcta != "B" && pregunta.Correcta != "C")
                {
                    return null;
                }
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
        string texto = "Sos un diseñador de cursos cortos de Fluenty. Tu trabajo es enseñarle a un empleado a usar inteligencia artificial en SU trabajo real.\n\n";

        texto = texto + "CÓMO PENSAR EL CURSO:\n";
        texto = texto + "1. Leé las 3 tareas repetitivas del empleado. Para cada una pensá cómo la IA le ahorra tiempo o le evita los errores que contó.\n";
        texto = texto + "2. Usá las herramientas que ya usa y apuntá a lo que quiere mejorar él y a los objetivos de la empresa.\n";
        texto = texto + "3. Aprovechá sus fortalezas y trabajá sus debilidades.\n";
        texto = texto + "4. Dale ejemplos de pedidos (prompts) concretos que pueda copiar y usar en ChatGPT, Gemini o la IA que use la empresa.\n";
        texto = texto + "5. Adaptá la dificultad a su nivel de IA y el largo al tiempo por día que tiene.\n\n";

        texto = texto + "ELEGÍ UNO DE ESTOS 7 MODELOS DE CURSO según el puesto: ";
        texto = texto + "Tareas de escritorio (administrativos, contadores, RR.HH.: textos, mails, planillas). ";
        texto = texto + "Oficio y campo (trabajos manuales: lecciones muy cortas; la IA sirve para consultar, diagnosticar, calcular materiales o presupuestar). ";
        texto = texto + "Ventas y atención al cliente (reclamos, propuestas, seguimiento; cuidar los datos del cliente). ";
        texto = texto + "Gestión y liderazgo (dueños, gerentes, encargados: decisiones, delegación, reuniones, números). ";
        texto = texto + "Creativo y marketing (ideas, textos, imágenes; cuidado con la originalidad y los derechos de autor). ";
        texto = texto + "Técnico y datos (programadores, analistas, soporte: más avanzado, encontrar errores, comparar soluciones). ";
        texto = texto + "Primer contacto (si es principiante y casi no usa tecnología: qué es la IA, qué no es y un primer uso fácil).\n\n";

        texto = texto + "EL CURSO TIENE EXACTAMENTE 5 PARTES:\n";
        texto = texto + "Parte 1: explicación simple y ordenada, usando sus tareas reales.\n";
        texto = texto + "Parte 2: 3 preguntas de opción múltiple (A, B, C) sobre la parte 1. Su contenido va vacío.\n";
        texto = texto + "Parte 3: cómo optimizar sus tareas con IA, con ejemplos de prompts.\n";
        texto = texto + "Parte 4: 3 preguntas de opción múltiple (A, B, C) sobre la parte 3. Su contenido va vacío.\n";
        texto = texto + "Parte 5: un ejercicio de práctica corto con una situación real de su puesto.\n\n";

        texto = texto + "REGLAS: escribí en español rioplatense, claro y sin relleno. Nunca pidas ni uses datos privados de clientes. ";
        texto = texto + "Cada contenido de parte tiene como máximo 2500 caracteres. Las partes 1, 3 y 5 llevan la lista de preguntas vacía.\n\n";

        texto = texto + "RESPONDÉ SOLO CON UN JSON CON ESTE FORMATO EXACTO:\n";
        texto = texto + "{\"modelo\": \"nombre del modelo elegido\", \"titulo\": \"titulo corto\", \"descripcion\": \"una o dos oraciones\", ";
        texto = texto + "\"partes\": [";
        texto = texto + "{\"numero\": 1, \"titulo\": \"...\", \"contenido\": \"...\", \"preguntas\": []}, ";
        texto = texto + "{\"numero\": 2, \"titulo\": \"...\", \"contenido\": \"\", \"preguntas\": [{\"texto\": \"...\", \"opcionA\": \"...\", \"opcionB\": \"...\", \"opcionC\": \"...\", \"correcta\": \"A\"}]}, ";
        texto = texto + "{\"numero\": 3, \"titulo\": \"...\", \"contenido\": \"...\", \"preguntas\": []}, ";
        texto = texto + "{\"numero\": 4, \"titulo\": \"...\", \"contenido\": \"\", \"preguntas\": [ ...3 preguntas como en la parte 2... ]}, ";
        texto = texto + "{\"numero\": 5, \"titulo\": \"...\", \"contenido\": \"...\", \"preguntas\": []}";
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
