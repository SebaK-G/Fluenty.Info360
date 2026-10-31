using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;

namespace Fluenty.Info360.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    public HomeController(ILogger<HomeController> logger){
        _logger = logger;
    }
    public IActionResult Privacy(){
        return View();
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(){
        return View();
    }

    public IActionResult Index(){
        // Si no hay nadie logueado, lo mandamos al login
        if (HttpContext.Session.GetString("IdEmpresa") == null){
            return RedirectToAction("Acceso");
        }
                // Un empleado no puede ver el panel del empleador
        if (HttpContext.Session.GetString("TipoUsuario") == "Empleado"){
            return RedirectToAction("PanelEmpleado");
        }

        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        BD bd = new BD();

        ViewBag.Nombre = HttpContext.Session.GetString("Nombre");
        ViewBag.NombreEmpresa = bd.TraerNombreEmpresa(idEmpresa);
        ViewBag.CantidadCursos = bd.ContarCursosEmpresa(idEmpresa);
        ViewBag.CantidadEmpleados = bd.ContarEmpleadosEmpresa(idEmpresa);
        // Registrados = todos los que se unieron con el link; en el equipo = los que ya tienen curso confirmado
        int registrados = bd.TraerEmpleadosConCurso(idEmpresa).Count;
        ViewBag.EmpleadosActivos = registrados;
        ViewBag.EsperandoCurso = registrados - ViewBag.CantidadEmpleados;
        ViewBag.MesesRestantes = bd.TraerMesesRestantesPlan();
        // Actividades recientes: los últimos 5 cursos confirmados de la empresa
        List<ActividadCurso> cursosConfirmados = bd.TraerActividadesEmpresa(idEmpresa);
        List<string> actividades = new List<string>();
        foreach (ActividadCurso actividad in cursosConfirmados){
            if (actividades.Count < 5){
                actividades.Add("Curso \"" + actividad.Titulo + "\" asignado a " + actividad.Nombre + " " + actividad.Apellido);
            }
        }
        ViewBag.Actividades = actividades;

        return View();
    }
        public IActionResult PanelEmpleado(){
        // Solo entra un empleado logueado
        if (HttpContext.Session.GetString("TipoUsuario") != "Empleado"){
            return RedirectToAction("Acceso");
        }
        BD bd = new BD();
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        ViewBag.Nombre = HttpContext.Session.GetString("Nombre");
        ViewBag.NombreEmpresa = bd.TraerNombreEmpresa(idEmpresa);

        // Su curso, solo si el empleador ya lo confirmó
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        Cursos curso = bd.TraerCursoPorEmpleado(idUsuario);
        if (curso != null && curso.Estado == "Confirmado"){
            ViewBag.Curso = curso;
        }
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
        BD bd = new BD();
        ViewBag.PorcentajeAhorro = bd.TraerPorcentajeAhorro();
        return View();
    }

    [HttpPost]
    public IActionResult CalcularEficiencia(int Empleados, int HorasSemana, int CostoHora){
        BD bd = new BD();
        int porcentajeAhorro = bd.TraerPorcentajeAhorro();

        int horasMes = Empleados * HorasSemana * 4 * porcentajeAhorro / 100;
        ViewBag.HorasMes = horasMes;
        ViewBag.HorasAnio = horasMes * 12;
        ViewBag.AhorroMes = horasMes * CostoHora;
        ViewBag.AhorroAnio = horasMes * CostoHora * 12;
        ViewBag.PorcentajeAhorro = porcentajeAhorro;
        return View("Landing");
    }

      [HttpPost]
    public IActionResult Login(string Email, string Contrasena){
        BD bd = new BD();
        Usuarios usuario = bd.Login(Email, Contrasena);
        if (usuario != null){
            HttpContext.Session.SetString("Nombre", usuario.Nombre);
            HttpContext.Session.SetString("Email", usuario.Email);
            HttpContext.Session.SetString("IdEmpresa", usuario.IdEmpresa.ToString());

            HttpContext.Session.SetString("IdUsuario", usuario.IdUsuario.ToString());

            // El Rol dice si es empleado o empleador
            HttpContext.Session.SetString("TipoUsuario", usuario.Rol);
            if (usuario.Rol == "Empleado"){
                return RedirectToAction("PanelEmpleado");
            }
            return RedirectToAction("Index");
        }
        ViewBag.Mensaje = "Email o contraseña incorrectos";
        return View("Acceso");
    }
}
