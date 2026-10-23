using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;

namespace Fluenty.Info360.Controllers;

public class EmpleadoController : Controller
{
    // Muestra el form para invitar
    public IActionResult Invitar()
    {
        if (HttpContext.Session.GetString("IdEmpresa") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        return View();
    }

    // Guarda la invitación y muestra el link
    [HttpPost]
    public IActionResult Invitar(string Nombre, string Apellido, string Email, string Puesto, string InfoEmpleador)
    {
        if (HttpContext.Session.GetString("IdEmpresa") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }

        Invitaciones invitacion = new Invitaciones();
        invitacion.IdEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        invitacion.Nombre = Nombre;
        invitacion.Apellido = Apellido;
        invitacion.Email = Email;
        invitacion.Puesto = Puesto;
        invitacion.InfoEmpleador = InfoEmpleador;
        invitacion.Codigo = Guid.NewGuid().ToString();

        BD bd = new BD();
        bd.CrearInvitacion(invitacion);

        ViewBag.Nombre = Nombre;
        ViewBag.Codigo = invitacion.Codigo;
        return View("LinkGenerado");
    }

    // Lista de invitaciones de la empresa
    public IActionResult Invitaciones()
    {
        if (HttpContext.Session.GetString("IdEmpresa") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        ViewBag.Invitaciones = bd.TraerInvitacionesEmpresa(int.Parse(HttpContext.Session.GetString("IdEmpresa")));
        ViewBag.EstadoInvitacionPendiente = bd.TraerEstadoInvitacionPendiente();
        return View();
    }
}