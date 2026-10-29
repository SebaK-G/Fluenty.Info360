namespace Fluenty.Info360.Models;

// Estas 3 clases tienen la misma forma que el JSON que devuelve la IA (ver PROMPT-IA.md).
// Sirven solo para leer la respuesta; después se guarda en Cursos, PartesCurso y Preguntas.

public class CursoIA
{
    public string Modelo { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public List<ParteIA> Partes { get; set; } = new List<ParteIA>();
}

public class ParteIA
{
    public int Numero { get; set; }
    public string Titulo { get; set; } = "";
    public string Contenido { get; set; } = "";
    public List<PreguntaIA> Preguntas { get; set; } = new List<PreguntaIA>();
}

public class PreguntaIA
{
    public string Texto { get; set; } = "";
    public string OpcionA { get; set; } = "";
    public string OpcionB { get; set; } = "";
    public string OpcionC { get; set; } = "";
    public string Correcta { get; set; } = "";
}
