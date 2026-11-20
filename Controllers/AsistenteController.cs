using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;
using Fluenty.Services;

namespace Fluenty.Info360.Controllers;

// Sprint 3: el asistente de IA (la lógica está en Models/LogicaAsistente.cs).
public class AsistenteController : Controller
{
    private readonly GroqService _groqService;
    private readonly GroqFeatureToggle _groqFeatureToggle;

    public AsistenteController(GroqService groqService, GroqFeatureToggle groqFeatureToggle)
    {
        _groqService = groqService;
        _groqFeatureToggle = groqFeatureToggle;
    }

    // La pantalla: los chats a la izquierda y el chat abierto a la derecha.
    // IdChat = 0 es un chat nuevo (todavía vacío)
    public IActionResult Index(int IdChat = 0)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        int idRango = int.Parse(HttpContext.Session.GetString("IdRango"));
        bool esDueno = HttpContext.Session.GetString("TipoUsuario") == "Empleador";

        Chats chat = null;
        List<MensajesChat> mensajes = new List<MensajesChat>();
        if (IdChat != 0)
        {
            // Solo se abren los chats propios
            chat = LogicaAsistente.TraerChatPropio(IdChat, idUsuario);
            if (chat == null)
            {
                return RedirectToAction("Index", new { IdChat = 0 });
            }
            mensajes = LogicaAsistente.TraerMensajes(IdChat);
        }
        BD bd = new BD();

        ViewBag.Chats = bd.TraerChatsUsuario(idUsuario);
        ViewBag.Chat = chat;
        ViewBag.IdChat = IdChat;
        ViewBag.Mensajes = mensajes;
        ViewBag.Nombre = HttpContext.Session.GetString("Nombre");
        ViewBag.Restantes = LogicaAsistente.PreguntasRestantes(idUsuario);
        ViewBag.Sugerencias = LogicaAsistente.Sugerencias(idUsuario);
        ViewBag.Menu = Menu.Armar(idUsuario, idRango, esDueno);
        return View();
    }

    // Se llama con fetch desde wwwroot/js/asistente.js: guarda la pregunta, le pregunta a la IA
    // y devuelve la respuesta para mostrarla sin recargar la página.
    public async Task<RespuestaChat> Enviar(int IdChat, string Texto)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            RespuestaChat sinSesion = new RespuestaChat();
            sinSesion.Error = "Tenés que iniciar sesión.";
            return sinSesion;
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        int idRango = int.Parse(HttpContext.Session.GetString("IdRango"));
        bool esDueno = HttpContext.Session.GetString("TipoUsuario") == "Empleador";
        return await LogicaAsistente.Enviar(IdChat, Texto, idUsuario, esDueno, idRango, _groqService, _groqFeatureToggle.Enabled);
    }

    // Borrar un chat con todos sus mensajes
    [HttpPost]
    public IActionResult Borrar(int IdChat)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        if (LogicaAsistente.TraerChatPropio(IdChat, idUsuario) != null)
        {
            BD bd = new BD();
            bd.BorrarChat(IdChat);
        }
        return RedirectToAction("Index", new { IdChat = 0 });
    }
}
