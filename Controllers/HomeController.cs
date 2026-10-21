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
        ViewBag.Nombre = HttpContext.Session.GetString("Nombre") ?? "Usuario";
        ViewBag.NombreEmpresa = "tu empresa";
        ViewBag.CantidadCursos = 0;
        ViewBag.CantidadEmpleados = 0;
        ViewBag.Actividades = new List<string>();

        return View();
    }
    public IActionResult Acceso(){
        return View();
    }

    public IActionResult Landing(){
        return View();
    }

    [HttpPost]
    public IActionResult Login(string Email, string Contrasena){
        BD bd = new BD();
        Usuarios usuario = bd.Login(Email, Contrasena);
        if (usuario != null){
            HttpContext.Session.SetString("Nombre", usuario.Nombre);
            HttpContext.Session.SetString("Email", usuario.Email);
            return RedirectToAction("Index");
        }
        ViewBag.Mensaje = "Email o contraseña incorrectos";
        return View("Acceso");
    }

    [HttpPost]
    public IActionResult Signup(string Nombre, string Apellido, string Email, string NombreEmpresa, string Dni, string Telefono, string Contrasena){
        try{
            if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(Apellido) || string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(NombreEmpresa) || string.IsNullOrWhiteSpace(Dni) ||
                string.IsNullOrWhiteSpace(Telefono) || string.IsNullOrWhiteSpace(Contrasena)){
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

            bd.Signup(usuario);
            ViewBag.MensajeRegistro = "Registro exitoso. Ya puedes iniciar sesión.";
            return View("Acceso");
        }
        catch (Exception ex){
            ViewBag.MensajeRegistro = "No se pudo registrar el usuario: " + ex.Message;
            return View("Acceso");
        }
    }

    [HttpPost]
    public IActionResult GuardarTipoUsuario(string TipoUsuario, int? IdEmpleado, int? IdEmpleador){
        if (TipoUsuario == "Empleado"){
            if (IdEmpleado == null){
                ViewBag.Mensaje = "Debe ingresar un IdEmpleado.";
                return View("Formularios");
            }

            ViewBag.Mensaje = "Tipo de usuario: Empleado. IdEmpleado: " + IdEmpleado;
            return View("Formularios");
        }

        if (IdEmpleador == null){
            ViewBag.Mensaje = "Debe ingresar un IdEmpleador.";
            return View("Formularios");
        }

        ViewBag.Mensaje = "Tipo de usuario: Empleador. IdEmpleador: " + IdEmpleador;
        return View("Formularios");
    }
}
