using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;

namespace Fluenty.Info360.Controllers;

public class HomeController : Controller
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(){
        return View();
    }

    // Sprint 2: el mismo Home para todos los rangos (la lógica está en Models/LogicaHome.cs).
    // Si tiene rangos abajo, ve los números de su gente; si tiene un curso, ve su avance, XP y racha.
    public IActionResult Index(){
        // Si no hay nadie logueado, lo mandamos al login
        if (HttpContext.Session.GetString("IdUsuario") == null){
            return RedirectToAction("Acceso");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        int idRango = int.Parse(HttpContext.Session.GetString("IdRango"));
        bool esDueno = HttpContext.Session.GetString("TipoUsuario") == "Empleador";
        BD bd = new BD();

        ViewBag.Nombre = HttpContext.Session.GetString("Nombre");
        ViewBag.NombreEmpresa = bd.TraerNombreEmpresa(idEmpresa);
        ViewBag.EsDueno = esDueno;
        ViewBag.MesesRestantes = bd.TraerMesesRestantesPlan();

        // Su gente: las personas de los rangos que tiene abajo
        ResumenEquipo resumen = LogicaHome.ArmarResumen(idEmpresa, idRango);
        ViewBag.Gestiona = resumen.CantidadRangos > 0;
        ViewBag.CantidadRangos = resumen.CantidadRangos;
        ViewBag.CantidadPersonas = resumen.CantidadPersonas;
        ViewBag.EnCapacitacion = resumen.EnCapacitacion;
        ViewBag.EsperandoCurso = resumen.EsperandoCurso;
        ViewBag.CantidadCursos = resumen.CantidadCursos;
        ViewBag.Quietos = resumen.Quietos;
        ViewBag.Actividades = resumen.Actividades;

        // Sus cursos (el que le asignó su rango de arriba y los que sumó después)
        Usuarios yo = bd.TraerEmpleado(idUsuario);
        ViewBag.XP = yo.XP;
        ViewBag.Racha = yo.Racha;
        ViewBag.MisCursos = LogicaHome.TraerMisCursos(idUsuario);

        ViewBag.Menu = Menu.Armar(idUsuario, idRango, esDueno);
        return View();
    }

    // Cerrar sesión: borra todo lo guardado en Session y vuelve a la Landing
    public IActionResult Salir(){
        HttpContext.Session.Clear();
        return RedirectToAction("Landing");
    }

    public IActionResult Acceso(){
        return View();
    }

    public IActionResult Landing(){
        return View();
    }

    [HttpPost]
    public IActionResult Login(string Email, string Contrasena){
        if (Funciones.EstaVacio(Email) || Funciones.EstaVacio(Contrasena)){
            ViewBag.Mensaje = "Completá el email y la contraseña";
            return View("Acceso");
        }
        BD bd = new BD();
        Usuarios usuario = bd.Login(Funciones.Limpiar(Email), Contrasena);
        if (usuario == null){
            ViewBag.Mensaje = "Email o contraseña incorrectos";
            return View("Acceso");
        }
        HttpContext.Session.SetString("Nombre", usuario.Nombre);
        HttpContext.Session.SetString("Email", usuario.Email);
        HttpContext.Session.SetString("IdEmpresa", usuario.IdEmpresa.ToString());
        HttpContext.Session.SetString("IdUsuario", usuario.IdUsuario.ToString());
        // El Rol dice si es el dueño ("Empleador") o alguien de un rango ("Empleado")
        HttpContext.Session.SetString("TipoUsuario", usuario.Rol);
        // Sprint 2: su rango en el árbol decide qué puede ver
        HttpContext.Session.SetString("IdRango", usuario.IdRango.ToString());
        return RedirectToAction("Index");
    }
}