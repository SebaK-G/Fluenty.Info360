using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;

namespace Fluenty.Info360.Controllers;

// Sprint 2: el árbol de rangos de la empresa.
// El dueño arma las "cajas" (Gerente, Encargado, Vendedor...) y cada rango tiene su propio link para sumarse.
// Quien tiene rangos abajo ve su parte del árbol y los links de esos rangos (para invitar a su gente).
public class EquipoController : Controller
{
    public IActionResult Index()
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        bool esDueno = HttpContext.Session.GetString("TipoUsuario") == "Empleador";
        List<Rangos> rangos = bd.TraerRangosEmpresa(idEmpresa);

        // El dueño ve todo el árbol; los demás, su rango y lo que tienen abajo
        List<Rangos> arbol;
        if (esDueno)
        {
            arbol = Arbol.Ordenado(rangos);
        }
        else
        {
            string idRangoTexto = HttpContext.Session.GetString("IdRango");
            if (idRangoTexto == null || idRangoTexto == "" || !Arbol.TieneRangosAbajo(rangos, int.Parse(idRangoTexto)))
            {
                return RedirectToAction("Index", "Home");
            }
            arbol = Arbol.ConSuArbol(rangos, int.Parse(idRangoTexto));
        }

        // Cuántas personas hay en cada rango
        List<EmpleadoCurso> miembros = bd.TraerMiembrosEmpresa(idEmpresa);
        Dictionary<int, int> personasPorRango = new Dictionary<int, int>();
        foreach (Rangos rango in arbol)
        {
            int cantidad = 0;
            foreach (EmpleadoCurso miembro in miembros)
            {
                if (miembro.IdRango == rango.IdRango)
                {
                    cantidad++;
                }
            }
            personasPorRango[rango.IdRango] = cantidad;
        }

        ViewBag.Arbol = arbol;
        ViewBag.PersonasPorRango = personasPorRango;
        ViewBag.EsDueno = esDueno;
        ViewBag.IdRangoPropio = HttpContext.Session.GetString("IdRango");
        // La dirección base del link de cada rango (se le agrega el código)
        ViewBag.LinkBase = Url.Action("Unirse", "Formulario", null, Request.Scheme) + "?Codigo=";
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
        BD bd = new BD();
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));

        // El rango de arriba tiene que ser de su empresa
        bool padreValido = false;
        foreach (Rangos rango in bd.TraerRangosEmpresa(idEmpresa))
        {
            if (rango.IdRango == IdRangoPadre)
            {
                padreValido = true;
            }
        }
        if (padreValido && Nombre != null && Nombre.Trim() != "")
        {
            Rangos nuevo = new Rangos();
            nuevo.IdEmpresa = idEmpresa;
            nuevo.Nombre = Nombre.Trim();
            nuevo.IdRangoPadre = IdRangoPadre;
            nuevo.Codigo = Guid.NewGuid().ToString();
            nuevo.Tareas = DatosIA.Recortar(Texto(Tareas), 300);
            bd.CrearRango(nuevo);
        }
        return RedirectToAction("Index");
    }

    // Solo el dueño cuenta qué hace cada rango. Si un empleado llena mal (o a medias) su formulario,
    // la IA arma el curso con esto, así el curso sirve igual.
    [HttpPost]
    public IActionResult GuardarTareas(int IdRango, string Tareas)
    {
        if (HttpContext.Session.GetString("TipoUsuario") != "Empleador")
        {
            return RedirectToAction("Index", "Home");
        }
        BD bd = new BD();
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        foreach (Rangos rango in bd.TraerRangosEmpresa(idEmpresa))
        {
            if (rango.IdRango == IdRango)
            {
                bd.ActualizarTareasRango(IdRango, DatosIA.Recortar(Texto(Tareas), 300));
            }
        }
        return RedirectToAction("Index");
    }

    private string Texto(string valor)
    {
        if (valor == null)
        {
            return "";
        }
        return valor.Trim();
    }
}
