namespace Fluenty.Info360.Models;

// Un curso que tiene una persona, con su avance: es lo que se muestra en "Mis cursos" del Home.
// Una persona puede tener varios (el de su equipo y, más adelante, cursos públicos).
public class CursoInscripto
{
    public int IdInscripcion { get; set; }
    public int IdCurso { get; set; }
    public string Titulo { get; set; }
    public string Descripcion { get; set; }
    public int Progreso { get; set; }
    public DateTime FechaAsignado { get; set; }

    // No viene de la base: la carga el controlador con la próxima lección disponible (o null si terminó)
    public Lecciones Proxima { get; set; }
}