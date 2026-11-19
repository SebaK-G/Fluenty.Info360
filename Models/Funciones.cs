namespace Fluenty.Info360.Models;

// Funciones chicas que usamos en varios controllers y vistas.
// Están armadas solo con lo que vimos en clase: Length, Substring, IndexOf, while, for y foreach.
public static class Funciones
{
    // Saca los espacios y los enters del principio y del final: "  hola  " -> "hola".
    // Si el texto es null (un campo que no se completó), devuelve "".
    public static string Limpiar(string texto)
    {
        if (texto == null)
        {
            return "";
        }
        string espacios = " \n\r\t";
        // Mientras el primer carácter sea un espacio, lo saca
        while (texto.Length > 0 && espacios.IndexOf(texto.Substring(0, 1)) >= 0)
        {
            texto = texto.Substring(1);
        }
        // Mientras el último carácter sea un espacio, lo saca
        while (texto.Length > 0 && espacios.IndexOf(texto.Substring(texto.Length - 1, 1)) >= 0)
        {
            texto = texto.Substring(0, texto.Length - 1);
        }
        return texto;
    }

    // true si no escribió nada (o solo espacios)
    public static bool EstaVacio(string texto)
    {
        return Limpiar(texto) == "";
    }

    // Parte un texto cada vez que aparece el separador, limpia cada pedazo y saltea los vacíos.
    // Separar("Excel ; Gmail;", ";") -> ["Excel", "Gmail"]
    public static List<string> Separar(string texto, string separador)
    {
        List<string> partes = new List<string>();
        if (texto == null)
        {
            return partes;
        }
        int posicion = texto.IndexOf(separador);
        while (posicion >= 0)
        {
            // El pedazo de antes del separador
            string parte = Limpiar(texto.Substring(0, posicion));
            if (parte != "")
            {
                partes.Add(parte);
            }
            // Seguimos con lo que queda después del separador
            texto = texto.Substring(posicion + separador.Length);
            posicion = texto.IndexOf(separador);
        }
        // El último pedazo (después del último separador)
        string ultima = Limpiar(texto);
        if (ultima != "")
        {
            partes.Add(ultima);
        }
        return partes;
    }

    // Lo contrario de Separar: Unir(["Excel", "Gmail"], " · ") -> "Excel · Gmail"
    public static string Unir(List<string> partes, string separador)
    {
        string texto = "";
        foreach (string parte in partes)
        {
            if (texto != "")
            {
                texto = texto + separador;
            }
            texto = texto + parte;
        }
        return texto;
    }

    // true si el número está en la lista
    public static bool EstaEnLista(List<int> lista, int valor)
    {
        foreach (int item in lista)
        {
            if (item == valor)
            {
                return true;
            }
        }
        return false;
    }

    // true si el texto está en el array (por ejemplo, si una categoría es una de las que existen)
    public static bool EstaEnArray(string[] lista, string valor)
    {
        foreach (string item in lista)
        {
            if (item == valor)
            {
                return true;
            }
        }
        return false;
    }
}
