namespace Fluenty.Info360.Models;

public class Invitaciones
{
    public int IdInvitacion { get; set; }
    public int IdEmpresa { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public string Email { get; set; }
    public string Puesto { get; set; }
    public string InfoEmpleador { get; set; }
    public string Codigo { get; set; }
    public string Estado { get; set; }
    public int? IdEmpleado { get; set; }
}