using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;

namespace Fluenty.Info360.Controllers;

public class EmpleadoController : Controller
{
    // Empleados del equipo: los que ya tienen su curso confirmado (Figma: "Empleados").
    // El empleador ya no carga empleados a mano: se suman solos con el link de la pantalla Cursos.
    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("TipoUsuario") != "Empleador")
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        List<EmpleadoCurso> todos = bd.TraerEmpleadosConCurso(idEmpresa);
        List<EmpleadoCurso> equipo = new List<EmpleadoCurso>();
        foreach (EmpleadoCurso empleado in todos)
        {
            if (empleado.EstadoCurso == "Confirmado")
            {
                equipo.Add(empleado);
            }
        }
        ViewBag.Equipo = equipo;
        return View();
    }
}
