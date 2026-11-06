namespace Fluenty.Info360.Models;

// Cuentas sobre el árbol de rangos. Se trae una sola vez la lista de rangos de la empresa
// y acá se recorre: quién está abajo de quién.
public static class Arbol
{
    // Los rangos que están abajo de idRango (hijos, nietos...), sin incluirlo
    public static List<Rangos> Debajo(List<Rangos> todos, int idRango)
    {
        List<Rangos> resultado = new List<Rangos>();
        AgregarHijos(todos, idRango, 0, resultado);
        return resultado;
    }

    // Todo el árbol ordenado desde arriba, cada rango con su profundidad (0 = el de más arriba)
    public static List<Rangos> Ordenado(List<Rangos> todos)
    {
        List<Rangos> resultado = new List<Rangos>();
        foreach (Rangos rango in todos)
        {
            if (rango.IdRangoPadre == null)
            {
                rango.Profundidad = 0;
                resultado.Add(rango);
                AgregarHijos(todos, rango.IdRango, 1, resultado);
            }
        }
        return resultado;
    }

    // El rango y todo lo que tiene abajo, ordenado (para mostrar el subárbol de alguien)
    public static List<Rangos> ConSuArbol(List<Rangos> todos, int idRango)
    {
        List<Rangos> resultado = new List<Rangos>();
        foreach (Rangos rango in todos)
        {
            if (rango.IdRango == idRango)
            {
                rango.Profundidad = 0;
                resultado.Add(rango);
            }
        }
        AgregarHijos(todos, idRango, 1, resultado);
        return resultado;
    }

    public static bool TieneRangosAbajo(List<Rangos> todos, int idRango)
    {
        foreach (Rangos rango in todos)
        {
            if (rango.IdRangoPadre == idRango)
            {
                return true;
            }
        }
        return false;
    }

    public static bool EstaDebajo(List<Rangos> todos, int idRangoDeArriba, int idRango)
    {
        foreach (Rangos rango in Debajo(todos, idRangoDeArriba))
        {
            if (rango.IdRango == idRango)
            {
                return true;
            }
        }
        return false;
    }

    // Recorre el árbol en profundidad: primero un hijo con todos sus descendientes, después el siguiente
    private static void AgregarHijos(List<Rangos> todos, int idPadre, int profundidad, List<Rangos> resultado)
    {
        foreach (Rangos rango in todos)
        {
            if (rango.IdRangoPadre == idPadre)
            {
                rango.Profundidad = profundidad;
                resultado.Add(rango);
                AgregarHijos(todos, rango.IdRango, profundidad + 1, resultado);
            }
        }
    }
}
