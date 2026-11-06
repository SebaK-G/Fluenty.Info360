using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;

namespace Fluenty.Info360.Controllers;

// Sprint 2: las personas de los rangos que el usuario tiene abajo, con su avance, XP y racha
public class EmpleadoController : Controller
{
    public IActionResult Index()
    {
        string idRangoTexto = HttpContext.Session.GetString("IdRango");
        if (HttpContext.Session.GetString("IdUsuario") == null || idRangoTexto == null || idRangoTexto == "")
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        List<int> idsDeAbajo = new List<int>();
        foreach (Rangos rango in Arbol.Debajo(bd.TraerRangosEmpresa(idEmpresa), int.Parse(idRangoTexto)))
        {
            idsDeAbajo.Add(rango.IdRango);
        }
        if (idsDeAbajo.Count == 0)
        {
            return RedirectToAction("Index", "Home");
        }

        List<EmpleadoCurso> equipo = new List<EmpleadoCurso>();
        foreach (EmpleadoCurso miembro in bd.TraerMiembrosEmpresa(idEmpresa))
        {
            if (miembro.IdRango != null && idsDeAbajo.Contains(miembro.IdRango.Value))
            {
                equipo.Add(miembro);
            }
        }
        ViewBag.Equipo = equipo;
        return View();
    }
}
