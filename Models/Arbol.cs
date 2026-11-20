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

    // Todo el árbol ordenado por niveles: primero el de más arriba (el dueño), después todos los del nivel 1,
    // después todos los del nivel 2, y así. Cada rango queda con su profundidad (0 = el de más arriba).
    public static List<Rangos> Ordenado(List<Rangos> todos)
    {
        List<Rangos> resultado = new List<Rangos>();
        foreach (Rangos rango in todos)
        {
            // El de más arriba (el dueño) no tiene rango padre: queda en 0
            if (rango.IdRangoPadre == 0)
            {
                rango.Profundidad = 0;
                resultado.Add(rango);
            }
        }
        AgregarPorNiveles(todos, resultado);
        return resultado;
    }

    // El rango y todo lo que tiene abajo, también por niveles (para mostrar el subárbol de alguien)
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
        AgregarPorNiveles(todos, resultado);
        return resultado;
    }

    // Recorre la lista desde el principio y, por cada rango, agrega al final a los que dependen de él.
    // Como los hijos se agregan al final, primero quedan todos los de un nivel y después los del siguiente.
    // (La base es un árbol, sin ciclos, así que en algún momento no quedan más hijos y el while termina.)
    private static void AgregarPorNiveles(List<Rangos> todos, List<Rangos> resultado)
    {
        int posicion = 0;
        while (posicion < resultado.Count)
        {
            Rangos padre = resultado[posicion];
            foreach (Rangos rango in todos)
            {
                if (rango.IdRangoPadre == padre.IdRango)
                {
                    rango.Profundidad = padre.Profundidad + 1;
                    resultado.Add(rango);
                }
            }
            posicion++;
        }
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
