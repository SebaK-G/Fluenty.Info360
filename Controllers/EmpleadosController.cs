using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;

namespace Fluenty.Info360.Controllers;

// Sprint 2: las personas de los rangos que el usuario tiene abajo, con su avance, XP y racha
public class EmpleadosController : Controller
{
    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        int idRango = int.Parse(HttpContext.Session.GetString("IdRango"));
        bool esDueno = HttpContext.Session.GetString("TipoUsuario") == "Empleador";

        string idsRangos = LogicaEmpleados.RangosDeAbajo(idEmpresa, idRango);
        if (idsRangos == "")
        {
            return RedirectToAction("Index", "Home");
        }
        ViewBag.Equipo = LogicaEmpleados.TraerEquipo(idsRangos);
        ViewBag.Menu = Menu.Armar(idUsuario, idRango, esDueno);
        return View();
    }
}
