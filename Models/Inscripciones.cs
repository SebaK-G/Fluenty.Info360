namespace Fluenty.Info360.Models;

// La inscripción de una persona a su curso: su avance general y en qué unidad arranca (la nivelación es de cada uno).
// El detalle lección por lección está en LeccionXUsuario, que cuelga de acá.
public class Inscripciones
{
    public int IdInscripcion { get; set; }
    // No es columna de Inscripciones: el curso se sabe por sus lecciones (LeccionXUsuario)
    public int IdCurso { get; set; }
    public int IdUsuario { get; set; }
    public int Progreso { get; set; }
    public int UnidadInicial { get; set; }
    public string MotivoNivel { get; set; }
    public DateTime FechaAsignado { get; set; }
}
