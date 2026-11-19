namespace Fluenty.Info360.Models;

// Una lección de una unidad (el contenido).
// Estado y NumeroUnidad no son columnas de Lecciones: vienen de sp_TraerLeccionesUsuario.
// Estado sale de LeccionXUsuario (Bloqueada, Disponible, Completada o YaSabe) para esa persona.
public class Lecciones
{
    public int IdLeccion { get; set; }
    public int IdUnidad { get; set; }
    public int Numero { get; set; }
    public string Titulo { get; set; }
    public string Objetivo { get; set; }
    public string Estado { get; set; }
    public int NumeroUnidad { get; set; }
    // Vienen de LeccionXUsuario: la nota que sacó (porcentaje de aciertos a la primera) y cuántas veces la hizo.
    // Si Intentos es 0 todavía no la rindió (y Nota queda en 0)
    public int Nota { get; set; }
    public int Intentos { get; set; }

    // El estado con el que arranca cada lección cuando alguien se suma a un curso:
    // las unidades de antes de donde arranca quedan como "YaSabe", la primera lección de su unidad "Disponible"
    // y el resto "Bloqueada". Lo usan CursoController (cursos del equipo) y CursoPublicoController (buscador).
    public static string EstadoInicial(int numeroUnidad, int numeroLeccion, int unidadInicial)
    {
        if (numeroUnidad < unidadInicial)
        {
            return "YaSabe";
        }
        if (numeroUnidad == unidadInicial && numeroLeccion == 1)
        {
            return "Disponible";
        }
        return "Bloqueada";
    }
}
