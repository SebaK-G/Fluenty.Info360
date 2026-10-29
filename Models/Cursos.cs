namespace Fluenty.Info360.Models;

public class Cursos
{
    public int IdCurso { get; set; }
    public int IdUsuario { get; set; }
    public string Titulo { get; set; }
    public string Descripcion { get; set; }
    public string Modelo { get; set; }
    public int Nivel { get; set; }
    public int Progreso { get; set; }
    public string Estado { get; set; }
}
