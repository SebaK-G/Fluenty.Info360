namespace Fluenty.Info360.Models;

// Lo que devuelve sp_TraerEmpleadosConCurso: un empleado que se sumó con el link y su curso.
// Si todavía no tiene curso, IdCurso, Progreso y EstadoCurso quedan en null.
public class EmpleadoCurso
{
    public int IdEmpleado { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public string Puesto { get; set; }
    public string Tareas { get; set; }
    public int? IdCurso { get; set; }
    public int? Progreso { get; set; }
    public string EstadoCurso { get; set; }
}
