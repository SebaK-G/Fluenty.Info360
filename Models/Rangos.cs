namespace Fluenty.Info360.Models;

// Un rango del árbol de la empresa (Dueño > Encargado > Vendedor...). Cada uno tiene su link (Codigo).
// IdRangoPadre es el rango de arriba. El del dueño está en NULL en la base y acá queda en 0.
public class Rangos
{
    public int IdRango { get; set; }
    public int IdEmpresa { get; set; }
    public string Nombre { get; set; }
    public int IdRangoPadre { get; set; }
    public string Codigo { get; set; }
    // Lo que hace la gente de este rango (lo cuenta el dueño). Si alguien llena mal su formulario, la IA usa esto.
    public string Tareas { get; set; }

    // No son columnas de la tabla:
    // NombreEmpresa viene de sp_TraerRangoPorCodigo; NombrePadre y CantidadPersonas, de sp_TraerArbolEmpresa.
    public string NombreEmpresa { get; set; }
    public string NombrePadre { get; set; }
    public int CantidadPersonas { get; set; }
    // Profundidad la calcula Arbol (0 = el de más arriba) para mostrar el árbol con sangría.
    public int Profundidad { get; set; }
    // Las arma LogicaEquipo para que la vista solo tenga que mostrarlas
    public bool EsPropio { get; set; }
    public string Link { get; set; }
    public string LinkWhatsApp { get; set; }
    // El color de su nivel en el árbol (de 4 en adelante, todos con el color 4)
    public int NivelColor { get; set; }
    // Rayitas para mostrarlo con sangría en la lista "Va abajo de"
    public string Sangria { get; set; }
}
