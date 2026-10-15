using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Fluenty.Models;
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
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult Index(){
        return View();
    }

    public IActionResult Acceso(){
        return View();
    }

    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string Email, string Contrasena)
    {
        BD miBD = new BD();
        Usuario usuario = miBD.Login(Email, Contrasena);

        if (usuario != null)
        {
            HttpContext.Session.SetString("Nombre", usuario.Nombre);
            HttpContext.Session.SetString("Email", usuario.Email);
            return RedirectToAction("Index");
        }

        ViewBag.Mensaje = "Email o contraseña incorrectos";
        return View();
    }
}
