using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;
using Fluenty.Services;

namespace Fluenty.Info360.Controllers;

// Sprint 3: el asistente de IA. Cada persona tiene sus chats (como en ChatGPT) para preguntarle a la IA
// cómo usarla en su trabajo. La IA conoce su trabajo (su formulario) y los cursos que está haciendo.
public class AsistenteController : Controller
{
    // Cuántos mensajes anteriores del chat se le mandan a la IA (más mensajes = más tokens)
    private const int MensajesDeMemoria = 20;

    private readonly GroqService _groqService;
    private readonly GroqFeatureToggle _groqFeatureToggle;

    public AsistenteController(GroqService groqService, GroqFeatureToggle groqFeatureToggle)
    {
        _groqService = groqService;
        _groqFeatureToggle = groqFeatureToggle;
    }

    // ============ La pantalla: los chats a la izquierda y el chat abierto a la derecha ============
    // IdChat = 0 es un chat nuevo (todavía vacío)
    public IActionResult Index(int IdChat = 0)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        BD bd = new BD();

        Chats chat = null;
        List<MensajesChat> mensajes = new List<MensajesChat>();
        if (IdChat != 0)
        {
            chat = bd.TraerChat(IdChat);
            // Solo se abren los chats propios
            if (chat == null || chat.IdUsuario != idUsuario)
            {
                return RedirectToAction("Index", new { IdChat = 0 });
            }
            mensajes = bd.TraerMensajesChat(IdChat);
        }

        // Si el último envío falló, el error (y la pregunta sin mandar) quedaron guardados en Session
        ViewBag.Error = HttpContext.Session.GetString("ErrorChat");
        ViewBag.Borrador = HttpContext.Session.GetString("BorradorChat");
        HttpContext.Session.Remove("ErrorChat");
        HttpContext.Session.Remove("BorradorChat");

        ViewBag.Chats = bd.TraerChatsUsuario(idUsuario);
        ViewBag.Chat = chat;
        ViewBag.Mensajes = mensajes;
        ViewBag.Nombre = HttpContext.Session.GetString("Nombre");
        ViewBag.Restantes = TopeDia(bd) - bd.TraerMensajesChatHoy(idUsuario);
        ViewBag.Sugerencias = Sugerencias(bd, idUsuario);
        return View("Index");
    }

    // ============ Mandar una pregunta: se guarda, se le pregunta a la IA y se guarda la respuesta ============
    [HttpPost]
    public async Task<IActionResult> Enviar(int IdChat, string Texto)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        BD bd = new BD();

        if (Texto == null || Texto.Trim() == "")
        {
            return RedirectToAction("Index", new { IdChat = IdChat });
        }
        Texto = DatosIA.Recortar(Texto.Trim(), 2000);

        if (IdChat != 0)
        {
            Chats chat = bd.TraerChat(IdChat);
            if (chat == null || chat.IdUsuario != idUsuario)
            {
                return RedirectToAction("Index", new { IdChat = 0 });
            }
        }
        if (!_groqFeatureToggle.Enabled)
        {
            HttpContext.Session.SetString("ErrorChat", "La IA está deshabilitada en este momento.");
            return RedirectToAction("Index", new { IdChat = IdChat });
        }
        if (bd.TraerMensajesChatHoy(idUsuario) >= TopeDia(bd))
        {
            HttpContext.Session.SetString("ErrorChat", "Llegaste al tope de preguntas al asistente de hoy. ¡Volvé mañana! 🙂");
            HttpContext.Session.SetString("BorradorChat", Texto);
            return RedirectToAction("Index", new { IdChat = IdChat });
        }

        // Si es un chat nuevo, se crea con el comienzo de la pregunta como título
        if (IdChat == 0)
        {
            IdChat = bd.CrearChat(idUsuario, Titulo(Texto));
        }
        bd.AgregarMensajeChat(IdChat, "Usuario", Texto);

        // A la IA le mandamos los últimos mensajes del chat (incluida la pregunta nueva)
        List<MensajesChat> todos = bd.TraerMensajesChat(IdChat);
        List<MensajesChat> historial = new List<MensajesChat>();
        for (int i = 0; i < todos.Count; i++)
        {
            if (i >= todos.Count - MensajesDeMemoria)
            {
                historial.Add(todos[i]);
            }
        }

        try
        {
            string respuesta = await _groqService.ConversarAsync(ArmarInstrucciones(bd, idUsuario), historial);
            bd.AgregarMensajeChat(IdChat, "IA", respuesta.Trim());
        }
        catch (Exception ex)
        {
            // La pregunta queda guardada en el chat; se avisa el error para que pueda volver a intentar
            HttpContext.Session.SetString("ErrorChat", ex.Message);
        }
        return RedirectToAction("Index", new { IdChat = IdChat });
    }

    // ============ Borrar un chat con todos sus mensajes ============
    [HttpPost]
    public IActionResult Borrar(int IdChat)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        BD bd = new BD();
        Chats chat = bd.TraerChat(IdChat);
        if (chat != null && chat.IdUsuario == idUsuario)
        {
            bd.BorrarChat(IdChat);
        }
        return RedirectToAction("Index", new { IdChat = 0 });
    }

    // Lo que la IA tiene que saber: quién es, qué hace la persona y qué cursos está haciendo
    private string ArmarInstrucciones(BD bd, int idUsuario)
    {
        string texto = "Sos el asistente de Fluenty, una app que enseña a usar inteligencia artificial en el trabajo. Tenés dos trabajos:\n";
        texto = texto + "1) Guiar a la persona DENTRO de Fluenty: si pregunta cómo hacer algo en la app (crear un curso, sumar a un empleado, hacer una lección...), explicale los pasos usando el manual de abajo, con los nombres exactos de los menús y botones. Si dice 'este sistema', 'la app' o 'acá', se refiere a Fluenty.\n";
        texto = texto + "2) Ayudarla a usar la IA en su trabajo: proponele prompts listos para copiar, explicale paso a paso y dale ejemplos de SU trabajo.\n";
        texto = texto + "Hablás en español rioplatense (vos), con un tono cercano y simple, sin palabras técnicas sin explicar. ";
        texto = texto + "Respondé corto: como mucho 3 párrafos o una lista de pasos. ";
        texto = texto + "Escribí en texto plano: nada de markdown, ni asteriscos, ni numerales; para listas usá guiones (- ) o números (1. ). ";
        texto = texto + "Si te piden algo que no tiene que ver con el trabajo, la IA o Fluenty, respondé amable y volvé al tema. ";
        texto = texto + "Nunca le pidas contraseñas ni datos privados de clientes, y si los pega, recordale que no los comparta con ninguna IA.\n\n";
        texto = texto + GuiaFluenty.Manual + "\n\n";

        // Qué menús tiene esta persona (lo mismo que decide _NavApp)
        bool esDueno = HttpContext.Session.GetString("TipoUsuario") == "Empleador";
        bool gestiona = false;
        string idRangoTexto = HttpContext.Session.GetString("IdRango");
        if (idRangoTexto != null && idRangoTexto != "")
        {
            int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
            gestiona = Arbol.TieneRangosAbajo(bd.TraerRangosEmpresa(idEmpresa), int.Parse(idRangoTexto));
        }
        texto = texto + GuiaFluenty.QueVe(esDueno, gestiona, bd.TraerCursosUsuario(idUsuario).Count) + "\n";

        Usuarios persona = bd.TraerEmpleado(idUsuario);
        FormulariosEmpleado form = bd.TraerFormularioEmpleado(idUsuario);
        if (persona != null && form != null)
        {
            texto = texto + "LA PERSONA CON LA QUE HABLÁS:\n" + DatosIA.Persona(bd, persona, form) + "\n";
        }
        else if (persona != null)
        {
            texto = texto + "LA PERSONA CON LA QUE HABLÁS: " + persona.Nombre + ", " + persona.Puesto + ".\n\n";
        }

        List<CursoInscripto> cursos = bd.TraerCursosUsuario(idUsuario);
        if (cursos.Count > 0)
        {
            texto = texto + "LOS CURSOS DE FLUENTY QUE ESTÁ HACIENDO (podés recomendarle seguir con ellos):\n";
            foreach (CursoInscripto curso in cursos)
            {
                texto = texto + "- " + curso.Titulo + " (" + curso.Progreso + "% completado)\n";
            }
        }
        return texto;
    }

    // Preguntas de ejemplo para arrancar un chat nuevo (con su tarea si la contó en el formulario)
    private List<string> Sugerencias(BD bd, int idUsuario)
    {
        List<string> sugerencias = new List<string>();
        FormulariosEmpleado form = bd.TraerFormularioEmpleado(idUsuario);
        if (form != null && !DatosIA.EsFloja(form.TareaConcreta))
        {
            sugerencias.Add("¿Cómo me puede ayudar la IA con esto?: " + DatosIA.Recortar(form.TareaConcreta, 120));
        }
        sugerencias.Add("¿Cómo funciona Fluenty y por dónde empiezo?");
        sugerencias.Add("Armame un prompt para responder mails de clientes más rápido");
        sugerencias.Add("¿Qué cosas de mi trabajo puedo automatizar con IA?");
        return sugerencias;
    }

    // El título del chat: las primeras palabras de la primera pregunta
    private string Titulo(string texto)
    {
        string titulo = texto.Replace("\r", " ").Replace("\n", " ");
        if (titulo.Length > 60)
        {
            titulo = titulo.Substring(0, 57) + "...";
        }
        return titulo;
    }

    private int TopeDia(BD bd)
    {
        int tope = bd.TraerConfiguracion("TopeMensajesChatDia");
        if (tope <= 0)
        {
            tope = 30;
        }
        return tope;
    }
}
