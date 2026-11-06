namespace Fluenty.Info360.Models;

// Lo que devuelve sp_TraerLeccionUsuario: una lección con el estado que tiene para una persona,
// y los datos de su unidad y su curso (para el reproductor de lecciones)
public class LeccionUsuario
{
    public int IdLeccionXUsuario { get; set; }
    public int IdLeccion { get; set; }
    public int IdUsuario { get; set; }
    public string Estado { get; set; }
    public int? Nota { get; set; }
    public int Intentos { get; set; }
    public string Titulo { get; set; }
    public string Objetivo { get; set; }
    public int Numero { get; set; }
    public int NumeroUnidad { get; set; }
    public string TituloUnidad { get; set; }
    public int IdCurso { get; set; }
    public string TituloCurso { get; set; }
    public string EstadoCurso { get; set; }
}
