namespace Fluenty.Info360.Models;

// Para la pantalla Cursos: un rango de abajo con su gente y su curso
public class GrupoRango
{
    public Rangos Rango { get; set; }
    public List<EmpleadoCurso> Miembros { get; set; } = new List<EmpleadoCurso>();
    public int ConFormulario { get; set; }
    public Cursos Curso { get; set; }
}
