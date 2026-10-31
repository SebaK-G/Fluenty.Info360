namespace Fluenty.Info360.Models;

// Empleados y empleadores son todos usuarios: Rol dice qué es cada uno ("Empleador" o "Empleado").
// Activo = false es un usuario desactivado (no se borra, pero ya no puede entrar).
public class Usuarios
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
    public int IdEmpresa { get; set; }
    public string Email { get; set; }
    public string Dni { get; set; }
    public string Telefono { get; set; }
    public string Contrasena { get; set; }
    public string Rol { get; set; }
    public string Puesto { get; set; }
    public bool Activo { get; set; }
}
