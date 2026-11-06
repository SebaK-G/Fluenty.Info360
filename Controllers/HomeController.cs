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

    // Sprint 2: el mismo Home para todos los rangos.
    // Si tiene rangos abajo, ve los números de su gente; si tiene un curso, ve su avance, XP y racha.
    public IActionResult Index(){
        // Si no hay nadie logueado, lo mandamos al login
        if (HttpContext.Session.GetString("IdEmpresa") == null){
            return RedirectToAction("Acceso");
        }
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        BD bd = new BD();

        ViewBag.Nombre = HttpContext.Session.GetString("Nombre");
        ViewBag.NombreEmpresa = bd.TraerNombreEmpresa(idEmpresa);
        ViewBag.EsDueno = HttpContext.Session.GetString("TipoUsuario") == "Empleador";
        ViewBag.MesesRestantes = bd.TraerMesesRestantesPlan();

        // ----- Su gente: las personas de los rangos que tiene abajo -----
        List<Rangos> rangos = bd.TraerRangosEmpresa(idEmpresa);
        List<int> idsDeAbajo = new List<int>();
        string idRangoTexto = HttpContext.Session.GetString("IdRango");
        if (idRangoTexto != null && idRangoTexto != ""){
            foreach (Rangos rango in Arbol.Debajo(rangos, int.Parse(idRangoTexto))){
                idsDeAbajo.Add(rango.IdRango);
            }
        }
        ViewBag.Gestiona = idsDeAbajo.Count > 0;
        ViewBag.CantidadRangos = idsDeAbajo.Count;

        int personas = 0;
        int enCapacitacion = 0;
        int esperando = 0;
        List<int> cursos = new List<int>();
        // Los que tienen curso y no avanzan (no empezaron o hace una semana que no hacen nada)
        List<string> quietos = new List<string>();
        foreach (EmpleadoCurso miembro in bd.TraerMiembrosEmpresa(idEmpresa)){
            if (miembro.IdRango != null && idsDeAbajo.Contains(miembro.IdRango.Value)){
                personas++;
                if (miembro.EstadoCurso == "Confirmado"){
                    enCapacitacion++;
                    if (!cursos.Contains(miembro.IdCurso.Value)){
                        cursos.Add(miembro.IdCurso.Value);
                    }
                    if (miembro.Actividad() != ""){
                        quietos.Add(miembro.Nombre + " " + miembro.Apellido + ": " + miembro.Actividad().ToLower());
                    }
                }
                else if (miembro.TieneFormulario){
                    esperando++;
                }
            }
        }
        ViewBag.CantidadPersonas = personas;
        ViewBag.EnCapacitacion = enCapacitacion;
        ViewBag.EsperandoCurso = esperando;
        ViewBag.CantidadCursos = cursos.Count;
        ViewBag.Quietos = quietos;

        // Actividades recientes: los últimos 5 cursos asignados en sus rangos
        List<string> actividades = new List<string>();
        foreach (ActividadCurso actividad in bd.TraerActividadesEmpresa(idEmpresa)){
            if (actividades.Count < 5 && idsDeAbajo.Contains(actividad.IdRango)){
                actividades.Add("Curso \"" + actividad.Titulo + "\" asignado a " + actividad.Nombre + " " + actividad.Apellido);
            }
        }
        ViewBag.Actividades = actividades;

        // ----- Su propio curso (si su rango de arriba le asignó uno) -----
        Usuarios yo = bd.TraerEmpleado(idUsuario);
        ViewBag.XP = yo.XP;
        ViewBag.Racha = yo.Racha;
        ViewBag.MiCurso = null;
        Cursos miCurso = bd.TraerCursoPorEmpleado(idUsuario);
        if (miCurso != null && miCurso.Estado == "Confirmado"){
            ViewBag.MiCurso = miCurso;
            ViewBag.MiProgreso = 0;
            foreach (CursoXUsuario asignacion in bd.TraerAsignacionesCurso(miCurso.IdCurso)){
                if (asignacion.IdUsuario == idUsuario){
                    ViewBag.MiProgreso = asignacion.Progreso;
                }
            }
            // La próxima lección para hacer
            ViewBag.Proxima = null;
            foreach (Lecciones leccion in bd.TraerLeccionesUsuario(miCurso.IdCurso, idUsuario)){
                if (leccion.Estado == "Disponible" && ViewBag.Proxima == null){
                    ViewBag.Proxima = leccion;
                }
            }
        }

        return View();
    }

    // El panel del empleado ahora es el mismo Home
    public IActionResult PanelEmpleado(){
        return RedirectToAction("Index");
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
        BD bd = new BD();
        Usuarios usuario = bd.Login(Email, Contrasena);
        if (usuario != null){
            HttpContext.Session.SetString("Nombre", usuario.Nombre);
            HttpContext.Session.SetString("Email", usuario.Email);
            HttpContext.Session.SetString("IdEmpresa", usuario.IdEmpresa.ToString());

            HttpContext.Session.SetString("IdUsuario", usuario.IdUsuario.ToString());

            // El Rol dice si es el dueño ("Empleador") o alguien de un rango ("Empleado")
            HttpContext.Session.SetString("TipoUsuario", usuario.Rol);
            // Sprint 2: su rango en el árbol decide qué puede ver
            string idRango = "";
            if (usuario.IdRango != null){
                idRango = usuario.IdRango.ToString();
            }
            HttpContext.Session.SetString("IdRango", idRango);
            return RedirectToAction("Index");
        }
        ViewBag.Mensaje = "Email o contraseña incorrectos";
        return View("Acceso");
    }
}
