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
    public IActionResult Signup(Usuarios usuario){
        if (usuario == null){
            ViewBag.MensajeRegistro = "Debe completar los datos del usuario.";
            return View("Acceso");
        }
        try{
            BD miBD = new BD();
            miBD.Signup(usuario);
            ViewBag.MensajeRegistro = "Registro exitoso. Ya puedes iniciar sesión.";
            return View("Acceso");
        }
        catch (Exception ex){
            ViewBag.MensajeRegistro = "No se pudo registrar el usuario: " + ex.Message;
            return View("Acceso");
        }
    }
}
