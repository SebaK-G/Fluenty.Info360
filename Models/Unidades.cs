namespace Fluenty.Info360.Models;

// Una unidad del curso (por ejemplo "Unidad 2: responder mails con IA"). Tiene 5 lecciones.
public class Unidades
{
    public int IdUnidad { get; set; }
    public int IdCurso { get; set; }
    public int Numero { get; set; }
    public string Titulo { get; set; }
    public string Objetivo { get; set; }
    // No es columna: las lecciones de esta unidad (las agrupan LogicaCursoPublico y LogicaCurso para mostrarlas)
    public List<Lecciones> Lecciones { get; set; }
    // Para mostrar el camino de una persona (los arma LogicaCurso):
    // la clase de CSS (color de la unidad, o gris si ya la sabe) y la etiqueta "Ya lo sabe" / "Arranca acá"
    public string Clase { get; set; }
    public string Etiqueta { get; set; }
}
