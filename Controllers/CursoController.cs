using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;
using Fluenty.Services;

namespace Fluenty.Info360.Controllers;

// Sprint 2: los cursos son POR RANGO (la lógica está en Models/LogicaCurso.cs).
// Cada uno ve y crea cursos solo para los rangos que tiene abajo.
public class CursoController : Controller
{
    private readonly GroqService _groqService;
    private readonly GroqFeatureToggle _groqFeatureToggle;

    public CursoController(GroqService groqService, GroqFeatureToggle groqFeatureToggle)
    {
        _groqService = groqService;
        _groqFeatureToggle = groqFeatureToggle;
    }

    // Pantalla Cursos: los rangos de abajo, su gente y su curso
    public IActionResult Index(string Mensaje = "")
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        int idRango = int.Parse(HttpContext.Session.GetString("IdRango"));
        bool esDueno = HttpContext.Session.GetString("TipoUsuario") == "Empleador";

        List<GrupoRango> grupos = LogicaCurso.ArmarGrupos(idEmpresa, idRango);
        if (grupos.Count == 0)
        {
            return RedirectToAction("Index", "Home");
        }
        // Antes de generar cursos, la empresa tiene que completar su formulario (lo hace el dueño)
        BD bd = new BD();
        bool puedeGenerar = bd.TraerFormularioEmpresa(idEmpresa) != null;
        if (!puedeGenerar)
        {
            if (esDueno)
            {
                return RedirectToAction("FormularioEmpresa");
            }
            Mensaje = "El dueño todavía no completó el formulario de la empresa. Cuando lo complete, vas a poder generar los cursos.";
        }
        ViewBag.Grupos = grupos;
        ViewBag.PuedeGenerar = puedeGenerar;
        ViewBag.Categorias = Cursos.Categorias;
        ViewBag.Mensaje = Mensaje;
        ViewBag.Menu = Menu.Armar(idUsuario, idRango, esDueno);
        return View();
    }

    // DUEÑO: formulario de cómo quiere los cursos
    public IActionResult FormularioEmpresa(string Error = "")
    {
        if (HttpContext.Session.GetString("TipoUsuario") != "Empleador")
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        int idRango = int.Parse(HttpContext.Session.GetString("IdRango"));
        BD bd = new BD();
        if (bd.TraerFormularioEmpresa(idEmpresa) != null)
        {
            return RedirectToAction("Index");
        }
        ViewBag.Herramientas = bd.TraerHerramientas();
        ViewBag.Error = Error;
        ViewBag.Menu = Menu.Armar(idUsuario, idRango, true);
        return View();
    }

    [HttpPost]
    public IActionResult FormularioEmpresa(string Dificultades, int[] Herramientas, string Objetivos)
    {
        if (HttpContext.Session.GetString("TipoUsuario") != "Empleador")
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        string error = LogicaCurso.GuardarFormularioEmpresa(idEmpresa, Dificultades, Objetivos, Herramientas);
        if (error != "")
        {
            return RedirectToAction("FormularioEmpresa", new { Error = error });
        }
        return RedirectToAction("Index");
    }

    // La IA genera UN curso para todo el rango (queda como borrador para revisarlo)
    [HttpPost]
    public async Task<IActionResult> Generar(int IdRango, bool Publico = false, string Tema = "", string Categoria = "")
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        int idRango = int.Parse(HttpContext.Session.GetString("IdRango"));
        Resultado resultado = await LogicaCurso.Generar(idEmpresa, idRango, IdRango, Publico, Tema, Categoria, _groqService, _groqFeatureToggle.Enabled);
        if (!resultado.Ok)
        {
            return RedirectToAction("Index", new { Mensaje = resultado.Mensaje });
        }
        return RedirectToAction("Ver", new { IdCurso = resultado.Id });
    }

    // Sumar al curso del rango a alguien que se unió después
    [HttpPost]
    public async Task<IActionResult> Sumar(int IdCurso, int IdUsuario)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        int idRango = int.Parse(HttpContext.Session.GetString("IdRango"));
        string error = await LogicaCurso.Sumar(idEmpresa, idRango, IdCurso, IdUsuario, _groqService, _groqFeatureToggle.Enabled);
        if (error != "")
        {
            return RedirectToAction("Index", new { Mensaje = error });
        }
        return RedirectToAction("Ver", new { IdCurso = IdCurso, IdUsuario = IdUsuario });
    }

    // Ver un curso: el propio (como alumno) o el de un rango de abajo (como responsable)
    public IActionResult Ver(int IdCurso, int IdUsuario = 0, string Error = "")
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuarioSesion = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        int idRango = int.Parse(HttpContext.Session.GetString("IdRango"));
        bool esDueno = HttpContext.Session.GetString("TipoUsuario") == "Empleador";

        CaminoCurso camino = LogicaCurso.ArmarCamino(IdCurso, IdUsuario, idUsuarioSesion, idEmpresa, idRango);
        if (camino == null)
        {
            return RedirectToAction("Index", "Home");
        }
        ViewBag.Curso = camino.Curso;
        ViewBag.Asignacion = camino.Asignacion;
        ViewBag.ModoAlumno = camino.ModoAlumno;
        ViewBag.Subtitulo = camino.Subtitulo;
        ViewBag.Personas = camino.Personas;
        ViewBag.XP = camino.XP;
        ViewBag.Racha = camino.Racha;
        ViewBag.TextoArranque = camino.TextoArranque;
        ViewBag.Unidades = camino.Unidades;
        ViewBag.Error = Error;
        // En el menú se marca "Mi curso" si es el suyo, o "Cursos" si lo mira como responsable
        ViewBag.BotonActivo = "Cursos";
        if (camino.ModoAlumno)
        {
            ViewBag.BotonActivo = "MiCurso";
        }
        ViewBag.Menu = Menu.Armar(idUsuarioSesion, idRango, esDueno);
        return View();
    }

    // Rehacer UNA unidad del borrador con un pedido ("que sea más difícil")
    [HttpPost]
    public async Task<IActionResult> RehacerUnidad(int IdCurso, int NumeroUnidad, string Pedido)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        int idRango = int.Parse(HttpContext.Session.GetString("IdRango"));
        string error = await LogicaCurso.RehacerUnidad(idEmpresa, idRango, IdCurso, NumeroUnidad, Pedido, _groqService, _groqFeatureToggle.Enabled);
        if (error != "")
        {
            return RedirectToAction("Ver", new { IdCurso = IdCurso, Error = error });
        }
        return Redirect(Url.Action("Ver", new { IdCurso = IdCurso }) + "#unidad-" + NumeroUnidad);
    }

    // Confirmar: el curso le llega a todo el rango
    [HttpPost]
    public IActionResult Confirmar(int IdCurso)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        int idRango = int.Parse(HttpContext.Session.GetString("IdRango"));
        bool esDueno = HttpContext.Session.GetString("TipoUsuario") == "Empleador";
        Cursos curso = LogicaCurso.Confirmar(idEmpresa, idRango, IdCurso, _groqService, _groqFeatureToggle.Enabled);
        if (curso == null)
        {
            return RedirectToAction("Index");
        }
        ViewBag.IdCurso = IdCurso;
        ViewBag.Titulo = curso.Titulo;
        ViewBag.Menu = Menu.Armar(idUsuario, idRango, esDueno);
        return View("CursoCreado");
    }

    // Descartar el borrador para generarlo de nuevo
    [HttpPost]
    public IActionResult Descartar(int IdCurso)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        int idRango = int.Parse(HttpContext.Session.GetString("IdRango"));
        LogicaCurso.Descartar(idEmpresa, idRango, IdCurso);
        return RedirectToAction("Index");
    }
}
