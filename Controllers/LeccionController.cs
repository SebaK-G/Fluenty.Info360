using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;
using Fluenty.Services;

namespace Fluenty.Info360.Controllers;

// Sprint 2: el reproductor de lecciones (la lógica está en Models/LogicaLeccion.cs).
// Responder, Practicar y Reexplicar se llaman con fetch desde wwwroot/js/leccion.js y devuelven objetos (JSON).
public class LeccionController : Controller
{
    private readonly GroqService _groqService;
    private readonly GroqFeatureToggle _groqFeatureToggle;

    public LeccionController(GroqService groqService, GroqFeatureToggle groqFeatureToggle)
    {
        _groqService = groqService;
        _groqFeatureToggle = groqFeatureToggle;
    }

    // Abrir una lección (Tarjeta = en qué tarjeta arranca; Falta = volvió porque le faltaba algo)
    public async Task<IActionResult> Index(int IdLeccion, int Tarjeta = 1, int Falta = 0)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        LeccionUsuario leccion = LogicaLeccion.TraerLeccionParaHacer(IdLeccion, idUsuario);
        if (leccion == null)
        {
            return RedirectToAction("Index", "Home");
        }
        string error = await LogicaLeccion.Preparar(leccion, idUsuario, _groqService, _groqFeatureToggle.Enabled);
        if (error != "")
        {
            ViewBag.Error = error;
            ViewBag.Leccion = leccion;
            return View("Error");
        }

        BD bd = new BD();
        Usuarios yo = bd.TraerEmpleado(idUsuario);
        List<Tarjetas> tarjetas = LogicaLeccion.ArmarTarjetas(leccion);
        ViewBag.Leccion = leccion;
        ViewBag.Tarjetas = tarjetas;
        ViewBag.TarjetaInicial = LogicaLeccion.TarjetaInicial(Tarjeta, tarjetas.Count);
        ViewBag.EsExamen = leccion.Numero == 5;
        ViewBag.XP = yo.XP;
        ViewBag.Racha = yo.Racha;
        ViewBag.PedidosRestantes = LogicaLeccion.TopePedidosDia() - bd.TraerPedidosIAHoy(idUsuario);
        ViewBag.Mensaje = "";
        if (Falta == 1)
        {
            ViewBag.Mensaje = "Todavía te falta contestar bien alguna pregunta o aprobar el \"Probalo vos\". Te llevamos a esa tarjeta.";
        }
        return View();
    }

    // Responder una pregunta: se corrige en el servidor. Si se equivoca, le toca otra pregunta del mismo tema.
    public async Task<RespuestaPregunta> Responder(int IdTarjeta, string Letra)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            RespuestaPregunta sinSesion = new RespuestaPregunta();
            sinSesion.Error = "Tenés que iniciar sesión.";
            return sinSesion;
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        return await LogicaLeccion.Responder(IdTarjeta, Letra, idUsuario, _groqService, _groqFeatureToggle.Enabled);
    }

    // "Probalo vos": la IA corrige y dice si aprobó
    public async Task<RespuestaPractica> Practicar(int IdTarjeta, string Respuesta)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            RespuestaPractica sinSesion = new RespuestaPractica();
            sinSesion.Error = "Tenés que iniciar sesión.";
            return sinSesion;
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        return await LogicaLeccion.Practicar(IdTarjeta, Respuesta, idUsuario, _groqService, _groqFeatureToggle.Enabled);
    }

    // "Explicamelo más simple" (Modo = simple) o "Dame otro ejemplo" (Modo = ejemplo)
    public async Task<RespuestaReexplicar> Reexplicar(int IdTarjeta, string Modo)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            RespuestaReexplicar sinSesion = new RespuestaReexplicar();
            sinSesion.Mensaje = "Tenés que iniciar sesión.";
            return sinSesion;
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        return await LogicaLeccion.Reexplicar(IdTarjeta, Modo, idUsuario, _groqService, _groqFeatureToggle.Enabled);
    }

    // Terminar la lección: nota, XP, racha y (si es examen) aprobar o volver a rendir
    [HttpPost]
    public IActionResult Completar(int IdLeccion)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        BD bd = new BD();
        LeccionUsuario leccion = bd.TraerLeccionUsuario(IdLeccion, idUsuario);
        if (leccion == null || (leccion.Estado != "Disponible" && leccion.Estado != "Completada"))
        {
            return RedirectToAction("Index", "Home");
        }
        if (bd.TraerTarjetas(leccion.IdLeccionXUsuario).Count == 0)
        {
            return RedirectToAction("Index", new { IdLeccion = IdLeccion });
        }

        ResultadoLeccion resultado = LogicaLeccion.Completar(leccion, idUsuario, _groqService, _groqFeatureToggle.Enabled);
        // Si le falta algo, no termina: vuelve a esa tarjeta
        if (resultado.OrdenPendiente != 0)
        {
            return RedirectToAction("Index", new { IdLeccion = IdLeccion, Tarjeta = resultado.OrdenPendiente, Falta = 1 });
        }
        Usuarios yo = bd.TraerEmpleado(idUsuario);
        ViewBag.Leccion = leccion;
        ViewBag.Nota = resultado.Nota;
        ViewBag.AciertosPrimera = resultado.AciertosPrimera;
        ViewBag.Preguntas = resultado.Preguntas;
        ViewBag.EsExamen = resultado.EsExamen;
        ViewBag.NotaMinima = resultado.NotaMinima;
        ViewBag.Aprobo = resultado.Aprobo;
        ViewBag.XPGanado = resultado.XPGanado;
        ViewBag.RachaSubio = resultado.RachaSubio;
        ViewBag.Proxima = resultado.Proxima;
        ViewBag.Ofrecer = resultado.Ofrecer;
        ViewBag.XP = yo.XP;
        ViewBag.Racha = yo.Racha;
        return View("Completada");
    }

    // Le fue perfecto en su primera lección: saltea las lecciones 2 a 4 y va al examen de la unidad
    [HttpPost]
    public IActionResult Saltar(int IdLeccion)
    {
        return Nivelar(IdLeccion, "saltar");
    }

    // Le costó mucho su primera lección: arranca una unidad antes
    [HttpPost]
    public IActionResult Bajar(int IdLeccion)
    {
        return Nivelar(IdLeccion, "bajar");
    }

    private IActionResult Nivelar(int idLeccion, string opcion)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        int idCurso = LogicaLeccion.Nivelar(idLeccion, idUsuario, opcion, _groqService, _groqFeatureToggle.Enabled);
        if (idCurso == 0)
        {
            return RedirectToAction("Index", "Home");
        }
        return RedirectToAction("Ver", "Curso", new { IdCurso = idCurso });
    }
}
