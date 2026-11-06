namespace Fluenty.Info360.Models;

// Lo que devuelve sp_TraerMiembrosEmpresa: una persona de la empresa (menos el dueño),
// su rango, si completó el formulario y su curso. Si no tiene curso, IdCurso, Progreso y EstadoCurso quedan en null.
public class EmpleadoCurso
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public string Puesto { get; set; }
    public int? IdRango { get; set; }
    public string NombreRango { get; set; }
    public bool TieneFormulario { get; set; }
    public string Tareas { get; set; }
    public int XP { get; set; }
    public int Racha { get; set; }
    // El último día que terminó una lección (null = todavía no hizo ninguna)
    public DateTime? UltimoDiaActivo { get; set; }
    public int? IdCurso { get; set; }
    public int? Progreso { get; set; }
    public string EstadoCurso { get; set; }

    // Si tiene el curso confirmado y no avanza, devuelve el aviso para su responsable ("" si va bien).
    // El responsable ve si participa, nunca lo que contestó en el formulario.
    public string Actividad()
    {
        if (EstadoCurso != "Confirmado" || Progreso == null || Progreso.Value >= 100)
        {
            return "";
        }
        if (UltimoDiaActivo == null)
        {
            return "Todavía no empezó";
        }
        int dias = (DateTime.Today - UltimoDiaActivo.Value.Date).Days;
        if (dias >= 7)
        {
            return "Sin avanzar hace " + dias + " días";
        }
        return "";
    }
}
