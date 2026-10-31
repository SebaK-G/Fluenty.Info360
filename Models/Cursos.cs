namespace Fluenty.Info360.Models;

// El contenido del curso que arma la IA. Es de una empresa;
// a quién está asignado y cómo va cada persona está en CursoXUsuario.
public class Cursos
{
    public int IdCurso { get; set; }
    public int IdEmpresa { get; set; }
    public string Titulo { get; set; }
    public string Descripcion { get; set; }
    public string Modelo { get; set; }
    public int Nivel { get; set; }
    public string Objetivos { get; set; }
    public string Estado { get; set; }
}
