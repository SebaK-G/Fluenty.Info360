namespace Fluenty.Info360.Models;

// Los números del Home para quien tiene gente a cargo (lo arma LogicaHome)
public class ResumenEquipo
{
    public int CantidadRangos { get; set; }
    public int CantidadPersonas { get; set; }
    public int EnCapacitacion { get; set; }
    public int EsperandoCurso { get; set; }
    public int CantidadCursos { get; set; }
    // Los que tienen curso y no avanzan: "Nombre Apellido: todavía no empezó"
    public List<string> Quietos { get; set; }
    // Los últimos cursos asignados: "Curso "X" asignado a Nombre Apellido"
    public List<string> Actividades { get; set; }
}
