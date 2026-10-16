namespace Fluenty.Info360.Models;

public class Usuarios
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public int IdEmpresa { get; set; }
    public string Email { get; set; }
    public int? IdEmpleador { get; set; }
    public int? IdEmpleado { get; set; }
    public string Dni { get; set; }
    public string Telefono { get; set; }
    public string Contrasena { get; set; }
}
