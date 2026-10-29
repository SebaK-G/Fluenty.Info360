using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;

namespace Fluenty.Info360.Controllers;

public class FormularioController : Controller
{
    // ============ El empleado se une solo con el link de la empresa ============
    public IActionResult Unirse(string Codigo)
    {
        BD bd = new BD();
        Empresas empresa = bd.TraerEmpresaPorCodigo(Codigo);
        if (empresa == null)
        {
            return View("LinkInvalido");
        }
        ViewBag.Empresa = empresa;
        return View();
    }

    [HttpPost]
    public IActionResult Unirse(string Codigo, string Nombre, string Apellido, string Email, string Dni, string Telefono, string Contrasena,
        string Puesto, int NivelIA, string Herramientas, string DificultadPrincipal, string MejoraEmpresa,
        string FortalezasDebilidades, string MejoraPersonal, string Tarea1, string Tarea2, string Tarea3,
        string Dificultades, string Objetivo1, string Objetivo2, string Objetivo3, string Objetivo4,
        string Objetivo5, string Objetivo6, string OtroObjetivo, int TiempoXDia)
    {
        BD bd = new BD();
        Empresas empresa = bd.TraerEmpresaPorCodigo(Codigo);
        if (empresa == null)
        {
            return View("LinkInvalido");
        }

        if (bd.ExisteUsuarioDuplicado(Email, Dni, Telefono))
        {
            ViewBag.Empresa = empresa;
            ViewBag.Mensaje = "Ya existe un usuario con ese email, DNI o teléfono.";
            return View();
        }

        // Pregunta 7: junta los objetivos que tildó
        string objetivos = "";
        if (Objetivo1 != null) { objetivos = objetivos + Objetivo1 + "; "; }
        if (Objetivo2 != null) { objetivos = objetivos + Objetivo2 + "; "; }
        if (Objetivo3 != null) { objetivos = objetivos + Objetivo3 + "; "; }
        if (Objetivo4 != null) { objetivos = objetivos + Objetivo4 + "; "; }
        if (Objetivo5 != null) { objetivos = objetivos + Objetivo5 + "; "; }
        if (Objetivo6 != null) { objetivos = objetivos + Objetivo6 + "; "; }
        if (OtroObjetivo != null && OtroObjetivo != "") { objetivos = objetivos + OtroObjetivo; }

        // 1. Creamos el empleado con su puesto
        Empleados empleado = new Empleados();
        empleado.Puesto = Puesto;
        empleado.Estado = true;
        int idEmpleado = bd.CrearEmpleado(empleado);

        // 2. Creamos su usuario, conectado a la empresa del link
        Usuarios usuario = new Usuarios();
        usuario.Nombre = Nombre;
        usuario.Apellido = Apellido;
        usuario.Email = Email;
        usuario.IdEmpresa = empresa.IdEmpresa;
        usuario.IdEmpleado = idEmpleado;
        usuario.Dni = Dni;
        usuario.Telefono = Telefono;
        usuario.Contrasena = Contrasena;
        bd.Signup(usuario);

        // 3. Guardamos su formulario (preguntas del doc "Logica de los Cursos")
        FormulariosEmpleado formulario = new FormulariosEmpleado();
        formulario.IdEmpleado = idEmpleado;
        formulario.NivelIA = NivelIA;
        formulario.Herramientas = Herramientas;
        formulario.DificultadPrincipal = DificultadPrincipal;
        formulario.MejoraEmpresa = MejoraEmpresa;
        formulario.FortalezasDebilidades = FortalezasDebilidades;
        formulario.MejoraPersonal = MejoraPersonal;
        formulario.Tareas = Tarea1 + "; " + Tarea2 + "; " + Tarea3;
        formulario.Dificultades = Dificultades;
        formulario.Objetivos = objetivos;
        formulario.TiempoXDia = TiempoXDia;
        bd.AgregarFormularioEmpleado(formulario);

        ViewBag.Nombre = Nombre;
        return View("Listo");
    }
}
