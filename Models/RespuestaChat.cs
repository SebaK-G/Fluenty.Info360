namespace Fluenty.Info360.Models;

// Lo que devuelve AsistenteController.Enviar a wwwroot/js/asistente.js (con fetch) después de preguntarle a la IA
public class RespuestaChat
{
    public bool Ok { get; set; }
    // Si algo salió mal, qué pasó (para mostrarlo abajo del chat)
    public string Error { get; set; }
    public int IdChat { get; set; }
    // true si la pregunta abrió un chat nuevo (entonces el JS lo agrega a la lista de la izquierda)
    public bool ChatNuevo { get; set; }
    public string TituloChat { get; set; }
    // La pregunta y la respuesta, cortadas en renglones
    public List<string> Pregunta { get; set; }
    public List<string> Respuesta { get; set; }
    public int Restantes { get; set; }
}
