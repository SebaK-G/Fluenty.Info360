using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;

namespace Fluenty.Info360.Controllers;

// Sprint 2: el árbol de rangos de la empresa.
// El dueño arma las "cajas" (Gerente, Encargado, Vendedor...) y cada rango tiene su propio link para sumarse.
// Quien tiene rangos abajo ve su parte del árbol y los links de esos rangos (para invitar a su gente).
// La lógica está en Models/LogicaEquipo.cs; acá solo se lee la sesión y se arma lo que necesita la vista.
public class EquipoController : Controller
{
    public IActionResult Index(string Error = "")
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        int idRango = int.Parse(HttpContext.Session.GetString("IdRango"));
        bool esDueno = HttpContext.Session.GetString("TipoUsuario") == "Empleador";

        // La dirección base del link de cada rango (se le agrega el código)
        string linkBase = Url.Action("Unirse", "Formulario", null, Request.Scheme) + "?Codigo=";
        List<Rangos> arbol = LogicaEquipo.ArmarArbol(idEmpresa, idRango, esDueno, linkBase);
        if (arbol.Count == 0)
        {
            return RedirectToAction("Index", "Home");
        }
        ViewBag.Arbol = arbol;
        ViewBag.EsDueno = esDueno;
        ViewBag.Error = Error;
        ViewBag.Menu = Menu.Armar(idUsuario, idRango, esDueno);
        return View();
    }

    // Solo el dueño crea rangos. IdRangoPadre = el rango que va arriba del nuevo.
    [HttpPost]
    public IActionResult Crear(string Nombre, int IdRangoPadre, string Tareas)
    {
        if (HttpContext.Session.GetString("TipoUsuario") != "Empleador")
        {
            return RedirectToAction("Index", "Home");
        }
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        string error = LogicaEquipo.CrearRango(idEmpresa, Nombre, IdRangoPadre, Tareas);
        return RedirectToAction("Index", new { Error = error });
    }

    // Se llama con fetch desde wwwroot/js/equipo.js: guarda qué hace la gente de un rango sin recargar la página.
    // Solo el dueño puede hacerlo.
    public Resultado GuardarTareas(int IdRango, string Tareas)
    {
        if (HttpContext.Session.GetString("TipoUsuario") != "Empleador")
        {
            return new Resultado(false, "Solo el dueño puede cambiar esto.");
        }
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        return LogicaEquipo.GuardarTareas(idEmpresa, IdRango, Tareas);
    }
}
