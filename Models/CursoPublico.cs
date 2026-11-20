namespace Fluenty.Info360.Models;

// Un curso del buscador de cursos públicos (sp_BuscarCursosPublicos).
// Inscriptos no es una columna: la consulta cuenta cuántas personas tienen el curso.
public class CursoPublico
{
    public int IdCurso { get; set; }
    public string Titulo { get; set; }
    public string Descripcion { get; set; }
    public string Categoria { get; set; }
    public int DuracionHoras { get; set; }
    public string Herramientas { get; set; }
    public int Nivel { get; set; }
    public int Inscriptos { get; set; }
    // Si la persona que busca ya lo tiene (lo calcula sp_BuscarCursosPublicos)
    public bool YaLoTiene { get; set; }
    // No vienen de la base: las arma LogicaCursoPublico para mostrar
    public string NombreNivel { get; set; }
    public string DescripcionCorta { get; set; }

    // El nivel del curso en palabras (Nivel = el nivel con la IA de la gente para la que se armó, de 1 a 4)
    public static string NivelEnPalabras(int nivel)
    {
        if (nivel >= 4)
        {
            return "Avanzado";
        }
        if (nivel == 3)
        {
            return "Intermedio";
        }
        return "Principiante";
    }
}
