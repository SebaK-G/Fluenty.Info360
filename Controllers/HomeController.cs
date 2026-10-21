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

    [HttpPost]
    public IActionResult Login(string Email, string Contrasena){
        BD miBD = new BD();
        Usuarios usuario = miBD.Login(Email, Contrasena);
        if (usuario != null){
            HttpContext.Session.SetString("Nombre", usuario.Nombre);
            HttpContext.Session.SetString("Email", usuario.Email);
            return RedirectToAction("Index");
        }
        ViewBag.Mensaje = "Email o contraseña incorrectos";
        return View("Acceso");
    }

    [HttpPost]
    public IActionResult Signup(string NombreEmpresa, string EmailEmpresa, int CantidadEmpleados, string Cuit, string Nombre, string Apellido, string Email, string Dni, string Telefono, string Contrasena){
        try{
            BD miBD = new BD();
            Empresas empresa = new Empresas();
            empresa.Nombre = NombreEmpresa;
            empresa.Email = EmailEmpresa;
            empresa.CantidadEmpleados = CantidadEmpleados;
            empresa.Cuit = Cuit;

            int idEmpresa = miBD.CrearEmpresa(empresa);

            Usuarios usuario = new Usuarios();
            usuario.Nombre = Nombre;
            usuario.Apellido = Apellido;
            usuario.IdEmpresa = idEmpresa;
            usuario.Email = Email;
            usuario.Dni = Dni;
            usuario.Telefono = Telefono;
            usuario.Contrasena = Contrasena;

            miBD.Signup(usuario);
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
