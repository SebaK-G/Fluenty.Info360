namespace Fluenty.Info360.Models;

// Un curso asignado a una persona: su avance y en qué unidad arranca (la nivelación es de cada uno)
public class CursoXUsuario
{
    public int IdCursoXUsuario { get; set; }
    public int IdCurso { get; set; }
    public int IdUsuario { get; set; }
    public int Progreso { get; set; }
    public int UnidadInicial { get; set; }
    public string MotivoNivel { get; set; }
    public DateTime FechaAsignado { get; set; }
}
