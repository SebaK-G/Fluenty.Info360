namespace Fluenty.Info360.Models;

// Lo que devuelve sp_TraerActividadesEmpresa: un curso confirmado y para quién
public class ActividadCurso
{
    public int IdCurso { get; set; }
    public string Titulo { get; set; }
    public string Nombre { get; set; }
    public string Apellido { get; set; }
}
