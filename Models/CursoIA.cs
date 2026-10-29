namespace Fluenty.Info360.Models;

// Estas 3 clases tienen la misma forma que el JSON que devuelve la IA (ver PROMPT-IA.md).
// Sirven solo para leer la respuesta; después se guarda en Cursos, Unidades y Lecciones.

public class CursoIA
{
    public string Modelo { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public List<string> Objetivos { get; set; } = new List<string>();
    public int UnidadInicial { get; set; }
    public string MotivoNivel { get; set; } = "";
    public List<UnidadIA> Unidades { get; set; } = new List<UnidadIA>();
}

public class UnidadIA
{
    public int Numero { get; set; }
    public string Titulo { get; set; } = "";
    public string Objetivo { get; set; } = "";
    public List<LeccionIA> Lecciones { get; set; } = new List<LeccionIA>();
}

public class LeccionIA
{
    public int Numero { get; set; }
    public string Titulo { get; set; } = "";
    public string Objetivo { get; set; } = "";
}
