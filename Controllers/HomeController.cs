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
        ViewBag.EmpleadosActivos = ViewBag.CantidadEmpleados;
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

    [HttpPost]
    public IActionResult Signup(string Nombre, string Apellido, string Email, string NombreEmpresa, string Cargo, string Dni, string Telefono, string Contrasena){
        try{
            if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(Apellido) || string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(NombreEmpresa) || string.IsNullOrWhiteSpace(Cargo) ||
                string.IsNullOrWhiteSpace(Dni) || string.IsNullOrWhiteSpace(Telefono) || string.IsNullOrWhiteSpace(Contrasena)){
                ViewBag.MensajeRegistro = "Faltan datos obligatorios para registrar el usuario.";
                return View("Acceso");
            }

            BD bd = new BD();
            int? idEmpresa = bd.ObtenerIdEmpresaPorNombre(NombreEmpresa);
            if (idEmpresa == null || idEmpresa <= 0){
                ViewBag.MensajeRegistro = "La empresa ingresada no existe.";
                return View("Acceso");
            }
            if (bd.ExisteUsuarioDuplicado(Email, Dni, Telefono)){
                ViewBag.MensajeRegistro = "El email, DNI o teléfono ya están registrados en otro usuario.";
                return View("Acceso");
            }

            Usuarios usuario = new Usuarios();
            usuario.Nombre = Nombre;
            usuario.Apellido = Apellido;
            usuario.IdEmpresa = idEmpresa.Value;
            usuario.Email = Email;
            usuario.Dni = Dni;
            usuario.Telefono = Telefono;
            usuario.Contrasena = Contrasena;
            // Desde acá solo se registran empleadores; los empleados se suman con el link
            usuario.Rol = "Empleador";
            usuario.Puesto = Cargo;

            int idUsuario = bd.Signup(usuario);
            HttpContext.Session.SetString("IdUsuario", idUsuario.ToString());
            HttpContext.Session.SetString("TipoUsuario", "Empleador");
            HttpContext.Session.SetString("Nombre", Nombre);
            HttpContext.Session.SetString("Email", Email);
            // En vez de la contraseña y el nombre de la empresa, guardamos el IdEmpresa
            HttpContext.Session.SetString("IdEmpresa", idEmpresa.Value.ToString());

            return RedirectToAction("Index");
        }
        catch (Exception ex){
            ViewBag.MensajeRegistro = "No se pudo registrar el usuario: " + ex.Message;
            return View("Acceso");
        }
    }

}