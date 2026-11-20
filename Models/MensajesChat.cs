namespace Fluenty.Info360.Models;

// Un mensaje de un chat del asistente. Rol: 'Usuario' (lo escribió la persona) o 'IA' (lo respondió la IA).
public class MensajesChat
{
    public int IdMensaje { get; set; }
    public int IdChat { get; set; }
    public string Rol { get; set; }
    public string Texto { get; set; }
    public DateTime Fecha { get; set; }
    // No es columna: el texto cortado en renglones, para mostrar cada uno como un párrafo (lo arma LogicaAsistente)
    public List<string> Parrafos { get; set; }
}
