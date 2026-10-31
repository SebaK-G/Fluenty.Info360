namespace Fluenty.Info360.Models;

// El formulario que completa el empleador una sola vez: es de la empresa, no de una persona
public class FormulariosEmpresa
{
    public int IdFormularioEmpresa { get; set; }
    public int IdEmpresa { get; set; }
    public string Dificultades { get; set; }
    public string Objetivos { get; set; }
    public DateTime FechaCompletado { get; set; }
}
