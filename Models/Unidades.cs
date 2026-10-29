namespace Fluenty.Info360.Models;

// Una unidad del curso (por ejemplo "Unidad 2: responder mails con IA"). Tiene 5 lecciones.
public class Unidades
{
    public int IdUnidad { get; set; }
    public int IdCurso { get; set; }
    public int Numero { get; set; }
    public string Titulo { get; set; }
    public string Objetivo { get; set; }
}
