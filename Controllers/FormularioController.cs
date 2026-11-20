using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;

namespace Fluenty.Info360.Controllers;

// Sprint 2: cada uno se une con el link de SU RANGO (la lógica está en Models/LogicaFormulario.cs)
public class FormularioController : Controller
{
    public IActionResult Unirse(string Codigo)
    {
        BD bd = new BD();
        Rangos rango = bd.TraerRangoPorCodigo(Codigo);
        if (rango == null)
        {
            return View("LinkInvalido");
        }
        ViewBag.Rango = rango;
        // Las casillas de herramientas salen del catálogo
        ViewBag.Herramientas = bd.TraerHerramientas();
        ViewBag.Mensaje = "";
        return View();
    }

    // Herramientas llega con el IdHerramienta de cada casilla tildada (puede venir vacío)
    [HttpPost]
    public IActionResult Unirse(string Codigo, string Nombre, string Apellido, string Email, string Dni, string Telefono, string Contrasena,
        string Puesto, string Area, string[] Tareas, string OtraTarea, string TareaConcreta, int[] Herramientas, string DatosClientes,
        int NivelIA, string ComodidadTecnologia, string RangoEdad, string[] PreocupacionIA, int TiempoXDia, string Hobbies)
    {
        BD bd = new BD();
        Rangos rango = bd.TraerRangoPorCodigo(Codigo);
        if (rango == null)
        {
            return View("LinkInvalido");
        }

        Usuarios usuario = new Usuarios();
        usuario.Nombre = Funciones.Limpiar(Nombre);
        usuario.Apellido = Funciones.Limpiar(Apellido);
        usuario.Email = Funciones.Limpiar(Email);
        usuario.Dni = Funciones.Limpiar(Dni);
        usuario.Telefono = Funciones.Limpiar(Telefono);
        usuario.Contrasena = Contrasena;
        usuario.Puesto = Funciones.Limpiar(Puesto);
        usuario.IdRango = rango.IdRango;

        FormulariosEmpleado formulario = new FormulariosEmpleado();
        formulario.NivelIA = NivelIA;
        formulario.Area = Funciones.Limpiar(Area);
        formulario.TareaConcreta = TareaConcreta;
        formulario.DatosClientes = Funciones.Limpiar(DatosClientes);
        formulario.ComodidadTecnologia = Funciones.Limpiar(ComodidadTecnologia);
        formulario.RangoEdad = Funciones.Limpiar(RangoEdad);
        formulario.TiempoXDia = TiempoXDia;
        formulario.Hobbies = Hobbies;

        string error = LogicaFormulario.Validar(usuario, formulario);
        if (error != "")
        {
            ViewBag.Rango = rango;
            ViewBag.Herramientas = bd.TraerHerramientas();
            ViewBag.Mensaje = error;
            return View();
        }
        int idUsuario = LogicaFormulario.Registrar(usuario, formulario, Tareas, OtraTarea, Herramientas, PreocupacionIA);

        // Si no contó qué es lo que más tiempo le come, le hacemos UNA pregunta más (se puede saltear)
        if (LogicaFormulario.HaceFaltaUnaMas(formulario))
        {
            HttpContext.Session.SetString("IdUsuarioNuevo", idUsuario.ToString());
            HttpContext.Session.SetString("NombreNuevo", usuario.Nombre);
            return RedirectToAction("UnaMas");
        }
        ViewBag.Nombre = usuario.Nombre;
        return View("Listo");
    }

    // La pregunta de más (solo si dejó floja la tarea que más tiempo le come)
    public IActionResult UnaMas()
    {
        string idTexto = HttpContext.Session.GetString("IdUsuarioNuevo");
        if (idTexto == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        ViewBag.Opciones = LogicaFormulario.OpcionesUnaMas(int.Parse(idTexto));
        ViewBag.Nombre = HttpContext.Session.GetString("NombreNuevo");
        return View();
    }

    [HttpPost]
    public IActionResult UnaMas(string Opcion, string Otra, string Detalle)
    {
        string idTexto = HttpContext.Session.GetString("IdUsuarioNuevo");
        if (idTexto == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        LogicaFormulario.GuardarUnaMas(int.Parse(idTexto), Opcion, Otra, Detalle);
        return Terminar();
    }

    // "Saltear": no contesta y listo (el curso se arma con lo que hace su rango)
    public IActionResult Saltear()
    {
        return Terminar();
    }

    private IActionResult Terminar()
    {
        ViewBag.Nombre = HttpContext.Session.GetString("NombreNuevo");
        HttpContext.Session.Remove("IdUsuarioNuevo");
        HttpContext.Session.Remove("NombreNuevo");
        return View("Listo");
    }
}
