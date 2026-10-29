namespace Fluenty.Info360.Models;

// Una lección de una unidad. Estado: Bloqueada, Disponible, Completada o YaSabe.
// NumeroUnidad viene de sp_TraerLeccionesCurso, para saber en qué unidad mostrarla.
public class Lecciones
{
    public int IdLeccion { get; set; }
    public int IdUnidad { get; set; }
    public int Numero { get; set; }
    public string Titulo { get; set; }
    public string Objetivo { get; set; }
    public string Estado { get; set; }
    public int NumeroUnidad { get; set; }
}
