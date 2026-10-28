namespace Fluenty.Info360.Models;

public class Preguntas
{
    public int IdPregunta { get; set; }
    public int IdCurso { get; set; }
    public int NumeroParte { get; set; }
    public string Texto { get; set; }
    public string OpcionA { get; set; }
    public string OpcionB { get; set; }
    public string OpcionC { get; set; }
    public string Correcta { get; set; }
}