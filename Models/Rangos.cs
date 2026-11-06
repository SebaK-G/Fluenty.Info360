namespace Fluenty.Info360.Models;

// Un rango del árbol de la empresa (Dueño > Encargado > Vendedor...). Cada uno tiene su link (Codigo).
// IdRangoPadre es el rango de arriba; el dueño tiene IdRangoPadre null.
public class Rangos
{
    public int IdRango { get; set; }
    public int IdEmpresa { get; set; }
    public string Nombre { get; set; }
    public int? IdRangoPadre { get; set; }
    public string Codigo { get; set; }
    // Lo que hace la gente de este rango (lo cuenta el dueño). Si alguien llena mal su formulario, la IA usa esto.
    public string Tareas { get; set; }

    // No son columnas: NombreEmpresa viene de sp_TraerRangoPorCodigo,
    // Profundidad la calcula Arbol para mostrar el árbol con sangría.
    public string NombreEmpresa { get; set; }
    public int Profundidad { get; set; }
}
