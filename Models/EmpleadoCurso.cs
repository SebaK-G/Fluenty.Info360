namespace Fluenty.Info360.Models;

// Sirve para la lista de empleados con su curso (si no tiene curso, IdCurso queda en null)
public class EmpleadoCurso
{
    public int IdEmpleado { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public string Puesto { get; set; }
    public int? IdCurso { get; set; }
    public int? Progreso { get; set; }
}