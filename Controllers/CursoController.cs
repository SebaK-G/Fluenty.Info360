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
        if (HttpContext.Session.GetString("TipoUsuario") != "Empleador" || HttpContext.Session.GetString("IdEmpleador") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        FormulariosEmpleador formulario = new FormulariosEmpleador();
        formulario.IdEmpleador = int.Parse(HttpContext.Session.GetString("IdEmpleador"));
        formulario.Dificultades = Dificultades;
        formulario.Herramientas = Herramientas;
        formulario.Objetivos = Objetivos;

        BD bd = new BD();
        bd.GuardarFormularioEmpleador(formulario);
        return RedirectToAction("Index");
    }
}
