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
        int idEmpleado = int.Parse(HttpContext.Session.GetString("IdEmpleado"));
        Cursos curso = bd.TraerCursoPorEmpleado(idEmpleado);
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

    public IActionResult formularioTipoUsuario(string tipoUsuario){
        ViewBag.TipoUsuario = tipoUsuario;
        return View();
    }

    [HttpPost]
    public IActionResult GuardarPerfilUsuario(string TipoUsuario, Empleadores empleador){
        try{
            BD bd = new BD();

            // Solo se registran empleadores: los empleados se suman con el link de la empresa
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
                HttpContext.Session.SetString("TipoUsuario", "Empleador");
                HttpContext.Session.SetString("IdEmpleador", idEmpleador.ToString());

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

            // Si tiene IdEmpleado es empleado, si no es empleador
            if (usuario.IdEmpleado != null && usuario.IdEmpleado > 0){
                HttpContext.Session.SetString("TipoUsuario", "Empleado");
                HttpContext.Session.SetString("IdEmpleado", usuario.IdEmpleado.ToString());
                return RedirectToAction("PanelEmpleado");
            }

            HttpContext.Session.SetString("TipoUsuario", "Empleador");
            HttpContext.Session.SetString("IdEmpleador", usuario.IdEmpleador.ToString());
            return RedirectToAction("Index");
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
            HttpContext.Session.SetString("Nombre", Nombre);
            HttpContext.Session.SetString("Email", Email);
            // En vez de la contraseña y el nombre de la empresa, guardamos el IdEmpresa
            HttpContext.Session.SetString("IdEmpresa", idEmpresa.Value.ToString());

            return RedirectToAction("formularioTipoUsuario", new { tipoUsuario = TipoUsuario });
        }
        catch (Exception ex){
            ViewBag.MensajeRegistro = "No se pudo registrar el usuario: " + ex.Message;
            return View("Acceso");
        }
    }

}