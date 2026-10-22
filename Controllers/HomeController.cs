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
        ViewBag.NombreEmpresa = "Ferran";
        ViewBag.CantidadCursos = 0;
        ViewBag.CantidadEmpleados = 0;
        ViewBag.Actividades = new List<string>();

        return View("Acceso");
    }
    public IActionResult Acceso(){
        return View();
    }

    public IActionResult Landing(){
        return View();
    }

    public IActionResult formularioTipoUsuario(string tipoUsuario){
        ViewBag.TipoUsuario = tipoUsuario;
        return View();
    }

    [HttpPost]
    public IActionResult GuardarPerfilUsuario(string TipoUsuario, Empleados empleado, Empleadores empleador){
        try{
            BD bd = new BD();

            if (TipoUsuario == "Empleado"){
                if (string.IsNullOrWhiteSpace(empleado.Puesto)){
                    ViewBag.TipoUsuario = TipoUsuario;
                    ViewBag.MensajeTipoUsuario = "Debe ingresar el puesto del empleado.";
                    return View("formularioTipoUsuario");
                }

                empleado.Estado = true;
                int idEmpleado = bd.CrearEmpleado(empleado);

                string email = HttpContext.Session.GetString("RegistroEmail");
                if (!string.IsNullOrWhiteSpace(email)){
                    bd.ActualizarUsuarioTipo(email, idEmpleado, null);
                }

                return RedirectToAction("Index");
            }

            if (TipoUsuario == "Empleador"){
                if (string.IsNullOrWhiteSpace(empleador.Cargo)){
                    ViewBag.TipoUsuario = TipoUsuario;
                    ViewBag.MensajeTipoUsuario = "Debe ingresar el cargo del empleador.";
                    return View("formularioTipoUsuario");
                }

                empleador.Estado = true;
                int idEmpleador = bd.CrearEmpleador(empleador);

                string email = HttpContext.Session.GetString("RegistroEmail");
                if (!string.IsNullOrWhiteSpace(email)){
                    bd.ActualizarUsuarioTipo(email, null, idEmpleador);
                }

                return RedirectToAction("Index");
            }

            ViewBag.MensajeTipoUsuario = "Debe seleccionar un tipo de usuario.";
            return View("Acceso");
        }
        catch (Exception ex){
            ViewBag.MensajeTipoUsuario = "No se pudo guardar la información: " + ex.Message;
            return View("formularioTipoUsuario");
        }
    }

    [HttpPost]
    public IActionResult CalcularEficiencia(int Empleados, int HorasSemana, int CostoHora){
        int horasMes = Empleados * HorasSemana * 4 * 25 / 100;
        ViewBag.HorasMes = horasMes;
        ViewBag.HorasAnio = horasMes * 12;
        ViewBag.AhorroMes = horasMes * CostoHora;
        ViewBag.AhorroAnio = horasMes * CostoHora * 12;
        return View("Landing");
    }

    [HttpPost]
    public IActionResult Login(string Email, string Contrasena){
        BD bd = new BD();
        Usuarios usuario = bd.Login(Email, Contrasena);
        if (usuario != null){
            HttpContext.Session.SetString("Nombre", usuario.Nombre);
            HttpContext.Session.SetString("Email", usuario.Email);
            return View("Index");
        }
        ViewBag.Mensaje = "Email o contraseña incorrectos";
        return View("Acceso");
    }

    [HttpPost]
    public IActionResult Signup(string Nombre, string Apellido, string Email, string NombreEmpresa, string TipoUsuario, string Dni, string Telefono, string Contrasena){
        try{
            if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(Apellido) || string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(NombreEmpresa) || string.IsNullOrWhiteSpace(TipoUsuario) ||
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

            bd.Signup(usuario);
            HttpContext.Session.SetString("RegistroEmail", Email);
            HttpContext.Session.SetString("TipoUsuario", TipoUsuario);

            return RedirectToAction("formularioTipoUsuario", new { tipoUsuario = TipoUsuario });
        }
        catch (Exception ex){
            ViewBag.MensajeRegistro = "No se pudo registrar el usuario: " + ex.Message;
            return View("Acceso");
        }
    }

}
