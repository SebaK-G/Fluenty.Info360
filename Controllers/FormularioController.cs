using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;

namespace Fluenty.Info360.Controllers;

public class FormularioController : Controller
{
    // El empleado entra por el link. No hace falta estar logueado: el código es la llave.
    public IActionResult Completar(string Codigo)
    {
        BD bd = new BD();
        string estadoPendiente = bd.TraerEstadoInvitacionPendiente();
        Invitaciones invitacion = bd.TraerInvitacionPorCodigo(Codigo);
        if (invitacion == null || invitacion.Estado != estadoPendiente)
        {
            return View("LinkInvalido");
        }
        ViewBag.Invitacion = invitacion;
        return View();
    }

    [HttpPost]
    public IActionResult Completar(string Codigo, string Dni, string Telefono, string Contrasena,
        int NivelIA, string Tarea1, string Tarea2, string Tarea3,
        string Herramientas, string Dificultades, string Objetivos, int TiempoXDia)
    {
        BD bd = new BD();
        string estadoPendiente = bd.TraerEstadoInvitacionPendiente();

        // Volvemos a buscar la invitación: los datos salen de la BD, no del form
        Invitaciones invitacion = bd.TraerInvitacionPorCodigo(Codigo);
        if (invitacion == null || invitacion.Estado != estadoPendiente)
        {
            return View("LinkInvalido");
        }

        if (bd.ExisteUsuarioDuplicado(invitacion.Email, Dni, Telefono))
        {
            ViewBag.Invitacion = invitacion;
            ViewBag.Mensaje = "Ya existe un usuario con ese email, DNI o teléfono.";
            return View();
        }

        // 1. Creamos el empleado
        Empleados empleado = new Empleados();
        empleado.Puesto = invitacion.Puesto;
        empleado.Estado = true;
        int idEmpleado = bd.CrearEmpleado(empleado);

        // 2. Creamos su usuario, ya conectado a la empresa y al empleado
        Usuarios usuario = new Usuarios();
        usuario.Nombre = invitacion.Nombre;
        usuario.Apellido = invitacion.Apellido;
        usuario.Email = invitacion.Email;
        usuario.IdEmpresa = invitacion.IdEmpresa;
        usuario.IdEmpleado = idEmpleado;
        usuario.Dni = Dni;
        usuario.Telefono = Telefono;
        usuario.Contrasena = Contrasena;
        bd.Signup(usuario);

        // 3. Guardamos su formulario
        FormulariosEmpleado formulario = new FormulariosEmpleado();
        formulario.IdEmpleado = idEmpleado;
        formulario.NivelIA = NivelIA;
        formulario.Tareas = Tarea1 + "; " + Tarea2 + "; " + Tarea3;
        formulario.Herramientas = Herramientas;
        formulario.Dificultades = Dificultades;
        formulario.Objetivos = Objetivos;
        formulario.TiempoXDia = TiempoXDia;
        bd.AgregarFormularioEmpleado(formulario);

        // 4. Marcamos la invitación como completada
        bd.CompletarInvitacion(Codigo, idEmpleado);

        ViewBag.Nombre = invitacion.Nombre;
        return View("Listo");
    }
}