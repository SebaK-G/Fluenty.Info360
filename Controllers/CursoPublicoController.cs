using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;
using Fluenty.Services;

namespace Fluenty.Info360.Controllers;

// Sprint 3: el buscador de cursos públicos.
// Cualquier persona logueada (de cualquier empresa) puede buscar un curso público, ver su temario e inscribirse.
// Al inscribirse queda en Inscripciones con Origen = 'Publico' y el curso aparece en "Mis cursos".
public class CursoPublicoController : Controller
{
    private readonly GroqService _groqService;
    private readonly GroqFeatureToggle _groqFeatureToggle;

    public CursoPublicoController(GroqService groqService, GroqFeatureToggle groqFeatureToggle)
    {
        _groqService = groqService;
        _groqFeatureToggle = groqFeatureToggle;
    }

    // ============ Buscador: texto, categoría, nivel y orden ============
    public IActionResult Buscar(string Texto = "", string Categoria = "", int Nivel = 0, string Orden = "recientes")
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        if (Texto == null)
        {
            Texto = "";
        }
        if (Categoria == null)
        {
            Categoria = "";
        }
        if (Orden != "populares")
        {
            Orden = "recientes";
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        BD bd = new BD();

        // Los cursos que ya tiene, para marcarlos como "Ya lo tenés"
        List<int> misCursos = new List<int>();
        foreach (CursoInscripto curso in bd.TraerCursosUsuario(idUsuario))
        {
            misCursos.Add(curso.IdCurso);
        }

        ViewBag.Cursos = bd.BuscarCursosPublicos(Funciones.Limpiar(Texto), Categoria, Nivel, Orden);
        ViewBag.MisCursos = misCursos;
        ViewBag.Categorias = Cursos.Categorias;
        ViewBag.Texto = Texto;
        ViewBag.Categoria = Categoria;
        ViewBag.Nivel = Nivel;
        ViewBag.Orden = Orden;
        return View("Buscar");
    }

    // ============ Detalle de un curso público: descripción, herramientas y temario ============
    public IActionResult Ver(int IdCurso)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        BD bd = new BD();
        Cursos curso = bd.TraerCurso(IdCurso);
        if (curso == null || !curso.Publico || curso.Estado != "Confirmado")
        {
            return RedirectToAction("Buscar");
        }

        bool yaInscripto = false;
        List<Inscripciones> asignaciones = bd.TraerAsignacionesCurso(IdCurso);
        foreach (Inscripciones item in asignaciones)
        {
            if (item.IdUsuario == idUsuario)
            {
                yaInscripto = true;
            }
        }

        // Las herramientas se guardan juntas ("ChatGPT · Excel · Gmail"): se separan para mostrarlas como chips
        List<string> herramientas = Funciones.Separar(curso.Herramientas, "·");

        ViewBag.Curso = curso;
        ViewBag.Unidades = bd.TraerUnidadesCurso(IdCurso);
        ViewBag.Lecciones = bd.TraerLeccionesCurso(IdCurso);
        ViewBag.Herramientas = herramientas;
        ViewBag.Inscriptos = asignaciones.Count;
        ViewBag.YaInscripto = yaInscripto;
        // Para armar las lecciones a su medida, la IA necesita el formulario de la persona
        ViewBag.TieneFormulario = bd.TraerFormularioEmpleado(idUsuario) != null;
        return View("Ver");
    }

    // ============ Inscribirse: le carga todas las lecciones del curso, nivelado por su experiencia con la IA ============
    [HttpPost]
    public IActionResult Inscribirse(int IdCurso)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        BD bd = new BD();
        Cursos curso = bd.TraerCurso(IdCurso);
        if (curso == null || !curso.Publico || curso.Estado != "Confirmado")
        {
            return RedirectToAction("Buscar");
        }

        // Si ya lo tiene (por su equipo o porque ya se inscribió), va directo al curso
        foreach (Inscripciones item in bd.TraerAsignacionesCurso(IdCurso))
        {
            if (item.IdUsuario == idUsuario)
            {
                return RedirectToAction("Ver", "Curso", new { IdCurso = IdCurso });
            }
        }

        FormulariosEmpleado form = bd.TraerFormularioEmpleado(idUsuario);
        if (form == null)
        {
            return RedirectToAction("Ver", new { IdCurso = IdCurso });
        }

        // Arranca en la unidad que corresponde a lo que contó en su formulario sobre la IA
        Inscripciones inscripcion = new Inscripciones();
        inscripcion.IdCurso = IdCurso;
        inscripcion.IdUsuario = idUsuario;
        inscripcion.UnidadInicial = DatosIA.UnidadPorNivel(form.NivelIA);
        inscripcion.MotivoNivel = "Arrancás según lo que contaste en tu formulario sobre tu experiencia con la IA.";
        inscripcion.Origen = "Publico";
        int idInscripcion = bd.AsignarCurso(inscripcion);
        foreach (Lecciones leccion in bd.TraerLeccionesCurso(IdCurso))
        {
            bd.AgregarLeccionUsuario(leccion.IdLeccion, idInscripcion, Lecciones.EstadoInicial(leccion.NumeroUnidad, leccion.Numero, inscripcion.UnidadInicial));
        }
        bd.ActualizarProgreso(IdCurso, idUsuario);

        // Se le deja preparada su primera lección mientras mira el curso
        if (_groqFeatureToggle.Enabled)
        {
            GeneradorLecciones.PrepararEnSegundoPlano(_groqService, idUsuario, GeneradorLecciones.Siguientes(bd, IdCurso, idUsuario, 0, 1));
        }
        return RedirectToAction("Ver", "Curso", new { IdCurso = IdCurso });
    }
}
