using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;
using Fluenty.Services;

namespace Fluenty.Info360.Controllers;

// Sprint 3: el buscador de cursos públicos (la lógica está en Models/LogicaCursoPublico.cs).
public class CursoPublicoController : Controller
{
    private readonly GroqService _groqService;
    private readonly GroqFeatureToggle _groqFeatureToggle;

    public CursoPublicoController(GroqService groqService, GroqFeatureToggle groqFeatureToggle)
    {
        _groqService = groqService;
        _groqFeatureToggle = groqFeatureToggle;
    }

    // Buscador: texto, categoría, nivel y orden
    public IActionResult Buscar(string Texto = "", string Categoria = "", int Nivel = 0, string Orden = "recientes")
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        int idRango = int.Parse(HttpContext.Session.GetString("IdRango"));
        bool esDueno = HttpContext.Session.GetString("TipoUsuario") == "Empleador";
        if (Orden != "populares")
        {
            Orden = "recientes";
        }

        ViewBag.Cursos = LogicaCursoPublico.Buscar(Texto, Categoria, Nivel, Orden, idUsuario);
        ViewBag.Categorias = Cursos.Categorias;
        ViewBag.Texto = Funciones.Limpiar(Texto);
        ViewBag.Categoria = Funciones.Limpiar(Categoria);
        ViewBag.Nivel = Nivel;
        ViewBag.Orden = Orden;
        ViewBag.Menu = Menu.Armar(idUsuario, idRango, esDueno);
        return View();
    }

    // Detalle de un curso público: descripción, herramientas y temario
    public IActionResult Ver(int IdCurso, string Error = "")
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        int idRango = int.Parse(HttpContext.Session.GetString("IdRango"));
        bool esDueno = HttpContext.Session.GetString("TipoUsuario") == "Empleador";
        Cursos curso = LogicaCursoPublico.TraerCursoPublico(IdCurso);
        if (curso == null)
        {
            return RedirectToAction("Buscar");
        }
        BD bd = new BD();

        ViewBag.Curso = curso;
        ViewBag.NombreNivel = CursoPublico.NivelEnPalabras(curso.Nivel);
        ViewBag.Unidades = LogicaCursoPublico.ArmarTemario(IdCurso);
        // Las herramientas se guardan juntas ("ChatGPT · Excel · Gmail"): se separan para mostrarlas como chips
        ViewBag.Herramientas = Funciones.Separar(curso.Herramientas, "·");
        // Cada objetivo viene en un renglón: se muestran como lista
        ViewBag.Objetivos = Funciones.Separar(curso.Objetivos, "\n");
        ViewBag.Inscriptos = bd.ContarInscriptos(IdCurso);
        ViewBag.YaInscripto = bd.EstaInscripto(IdCurso, idUsuario);
        // Para armar las lecciones a su medida, la IA necesita el formulario de la persona
        ViewBag.TieneFormulario = bd.TraerFormularioEmpleado(idUsuario) != null;
        ViewBag.Error = Error;
        ViewBag.Menu = Menu.Armar(idUsuario, idRango, esDueno);
        return View();
    }

    // Inscribirse: le carga todas las lecciones del curso y lo lleva a su curso
    [HttpPost]
    public IActionResult Inscribirse(int IdCurso)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        string error = LogicaCursoPublico.Inscribirse(IdCurso, idUsuario, _groqService, _groqFeatureToggle.Enabled);
        if (error != "")
        {
            return RedirectToAction("Ver", new { IdCurso = IdCurso, Error = error });
        }
        return RedirectToAction("Ver", "Curso", new { IdCurso = IdCurso });
    }
}
