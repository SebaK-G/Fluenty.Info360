namespace Fluenty.Info360.Models;

// Sprint 3 · Asistente de IA: cada conversación de una persona con la IA.
// Cuelga de Usuarios (la base sigue siendo un árbol): Usuarios → Chats → MensajesChat.
public class Chats
{
    public int IdChat { get; set; }
    public int IdUsuario { get; set; }
    // Se arma con el comienzo de la primera pregunta
    public string Titulo { get; set; }
    public DateTime FechaCreado { get; set; }
    // Cuándo se escribió el último mensaje (para ordenar la lista de chats)
    public DateTime FechaUltimo { get; set; }
}
