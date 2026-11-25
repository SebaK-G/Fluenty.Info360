using System.Net;
using Fluenty.Services;

namespace Fluenty.Info360.Models;

// Sprint 3: el asistente de IA. Cada persona tiene sus chats (como en ChatGPT) para preguntarle a la IA
// cómo usarla en su trabajo. La IA conoce su trabajo (su formulario) y los cursos que está haciendo.
public static class LogicaAsistente
{
    // Cuántos mensajes anteriores del chat se le mandan a la IA (más mensajes = más tokens)
    private const int MensajesDeMemoria = 20;

    // El chat, solo si es de esa persona (null si no existe o es de otro)
    public static Chats TraerChatPropio(int idChat, int idUsuario)
    {
        BD bd = new BD();
        Chats chat = bd.TraerChat(idChat);
        if (chat == null || chat.IdUsuario != idUsuario)
        {
            return null;
        }
        return chat;
    }

    // Los mensajes del chat, cada uno cortado en párrafos para mostrarlo
    public static List<MensajesChat> TraerMensajes(int idChat)
    {
        BD bd = new BD();
        List<MensajesChat> mensajes = bd.TraerMensajesChat(idChat);
        foreach (MensajesChat mensaje in mensajes)
        {
            mensaje.Parrafos = Funciones.Separar(mensaje.Texto, "\n");
        }
        return mensajes;
    }

    // Cuántas preguntas le quedan por hoy
    public static int PreguntasRestantes(int idUsuario)
    {
        BD bd = new BD();
        int restantes = TopeDia(bd) - bd.TraerMensajesChatHoy(idUsuario);
        if (restantes < 0)
        {
            restantes = 0;
        }
        return restantes;
    }

    // Manda la pregunta: la guarda, le pregunta a la IA con los últimos mensajes y guarda la respuesta
    public static async Task<RespuestaChat> Enviar(int idChat, string texto, int idUsuario, bool esDueno, int idRango, GroqService groqService, bool iaPrendida)
    {
        RespuestaChat respuesta = new RespuestaChat();
        respuesta.IdChat = idChat;
        respuesta.Error = "";
        BD bd = new BD();

        if (Funciones.EstaVacio(texto))
        {
            respuesta.Error = "Escribí tu pregunta.";
            return respuesta;
        }
        texto = DatosIA.Recortar(Funciones.Limpiar(texto), 2000);
        if (idChat != 0 && TraerChatPropio(idChat, idUsuario) == null)
        {
            respuesta.Error = "Ese chat no es tuyo.";
            return respuesta;
        }
        if (!iaPrendida)
        {
            respuesta.Error = "La IA está deshabilitada en este momento.";
            return respuesta;
        }
        if (PreguntasRestantes(idUsuario) <= 0)
        {
            respuesta.Error = "Llegaste al tope de preguntas al asistente de hoy. ¡Volvé mañana! 🙂";
            return respuesta;
        }

        // Si es un chat nuevo, se crea con el comienzo de la pregunta como título
        if (idChat == 0)
        {
            string titulo = Titulo(texto);
            respuesta.IdChat = bd.CrearChat(idUsuario, titulo);
            respuesta.ChatNuevo = true;
            respuesta.TituloChat = WebUtility.HtmlEncode(titulo);
        }
        bd.AgregarMensajeChat(respuesta.IdChat, "Usuario", texto);
        respuesta.Pregunta = ParrafosParaHtml(texto);

        try
        {
            // A la IA le mandamos los últimos mensajes del chat (incluida la pregunta nueva)
            List<MensajesChat> historial = bd.TraerUltimosMensajesChat(respuesta.IdChat, MensajesDeMemoria);
            string textoIA = Funciones.Limpiar(await groqService.ConversarAsync(ArmarInstrucciones(bd, idUsuario, esDueno, idRango), historial));
            bd.AgregarMensajeChat(respuesta.IdChat, "IA", textoIA);
            respuesta.Respuesta = ParrafosParaHtml(textoIA);
            respuesta.Ok = true;
        }
        catch (Exception ex)
        {
            // La pregunta queda guardada en el chat; se avisa el error para que pueda volver a intentar
            respuesta.Error = ex.Message;
        }
        respuesta.Restantes = PreguntasRestantes(idUsuario);
        return respuesta;
    }

    // Preguntas de ejemplo para arrancar un chat nuevo (con su tarea si la contó en el formulario)
    public static List<string> Sugerencias(int idUsuario)
    {
        BD bd = new BD();
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

    // Lo que la IA tiene que saber: quién es, qué hace la persona y qué cursos está haciendo
    private static string ArmarInstrucciones(BD bd, int idUsuario, bool esDueno, int idRango)
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

        // Qué menús tiene esta persona (lo mismo que muestra el menú de arriba)
        bool gestiona = bd.ContarRangosAbajo(idRango) > 0;
        List<CursoInscripto> cursos = bd.TraerCursosUsuario(idUsuario);
        texto = texto + GuiaFluenty.QueVe(esDueno, gestiona, cursos.Count) + "\n";

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

    // El texto cortado en renglones y listo para ponerlo con innerHTML desde JavaScript.
    // WebUtility.HtmlEncode cambia los < y > por códigos, así lo que escriben la persona o la IA
    // se muestra como texto y nunca se ejecuta como HTML.
    private static List<string> ParrafosParaHtml(string texto)
    {
        List<string> parrafos = new List<string>();
        foreach (string parrafo in Funciones.Separar(texto, "\n"))
        {
            parrafos.Add(WebUtility.HtmlEncode(parrafo));
        }
        return parrafos;
    }

    // El título del chat: las primeras palabras de la primera pregunta
    private static string Titulo(string texto)
    {
        string titulo = texto.Replace("\r", " ").Replace("\n", " ");
        if (titulo.Length > 60)
        {
            titulo = titulo.Substring(0, 57) + "...";
        }
        return titulo;
    }

    private static int TopeDia(BD bd)
    {
        int tope = bd.TraerConfiguracion("TopeMensajesChatDia");
        if (tope <= 0)
        {
            tope = 30;
        }
        return tope;
    }
}
