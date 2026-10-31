namespace Fluenty.Info360.Models;

public class FormulariosEmpleado
{
    public int IdFormularioEmpleado { get; set; }
    public int IdUsuario { get; set; }
    public int NivelIA { get; set; }
    public string Tareas { get; set; }
    public string Dificultades { get; set; }
    public string Objetivos { get; set; }
    public int TiempoXDia { get; set; }
    public string DificultadPrincipal { get; set; }
    public string MejoraEmpresa { get; set; }
    public string FortalezasDebilidades { get; set; }
    public string MejoraPersonal { get; set; }
    public DateTime FechaCompletado { get; set; }
}
