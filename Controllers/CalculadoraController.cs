using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;

namespace Fluenty.Info360.Controllers;

// Sprint 3: la calculadora de eficiencia (la lógica está en Models/LogicaCalculadora.cs).
// Solo la usa quien tiene gente a cargo (el dueño o un responsable).
public class CalculadoraController : Controller
{
    // La pantalla: las 4 preguntas, el resultado y los cálculos guardados.
    // IdCalculo = el cálculo que se muestra como resultado (0 = ninguno todavía)
    public IActionResult Index(int IdCalculo = 0, string Error = "")
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        int idRango = int.Parse(HttpContext.Session.GetString("IdRango"));
        bool esDueno = HttpContext.Session.GetString("TipoUsuario") == "Empleador";

        // Si no tiene rangos abajo, no tiene equipo para calcular
        string idsRangos = LogicaEmpleados.RangosDeAbajo(idEmpresa, idRango);
        if (idsRangos == "")
        {
            return RedirectToAction("Index", "Home");
        }
        List<EmpleadoCurso> equipo = LogicaEmpleados.TraerEquipo(idsRangos);
        BD bd = new BD();

        ViewBag.Tipos = LogicaCalculadora.TraerTareas(equipo);
        ViewBag.Personas = equipo.Count;
        ViewBag.EnCurso = LogicaCalculadora.ContarConCurso(equipo);
        ViewBag.Avance = LogicaCalculadora.CalcularAvance(equipo);
        ViewBag.Calculos = bd.TraerCalculosUsuario(idUsuario);
        // El cálculo a mostrar (null si no hay ninguno elegido o no es suyo)
        ViewBag.Resultado = bd.TraerCalculo(IdCalculo, idUsuario);
        ViewBag.Error = Error;
        ViewBag.Menu = Menu.Armar(idUsuario, idRango, esDueno);
        return View();
    }

    // Calcular: hace la cuenta, la guarda y vuelve a la pantalla mostrándola
    [HttpPost]
    public IActionResult Calcular(int IdTipoTarea, int Personas, string HorasSemana, string ValorHora)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        Calculos calculo = new Calculos();
        calculo.IdUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        calculo.IdTipoTarea = IdTipoTarea;
        calculo.Personas = Personas;
        string error = LogicaCalculadora.Calcular(calculo, HorasSemana, ValorHora);
        if (error != "")
        {
            return RedirectToAction("Index", new { Error = error });
        }
        return RedirectToAction("Index", new { IdCalculo = calculo.IdCalculo });
    }

    // Se llama con fetch desde wwwroot/js/calculadora.js: borra un cálculo guardado sin recargar la página.
    // El Stored Procedure solo borra si el cálculo es de esa persona.
    public Resultado Borrar(int IdCalculo)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return new Resultado(false, "Tenés que iniciar sesión.");
        }
        BD bd = new BD();
        bd.BorrarCalculo(IdCalculo, int.Parse(HttpContext.Session.GetString("IdUsuario")));
        return new Resultado(true, "Borrado");
    }
}
