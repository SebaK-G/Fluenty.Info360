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

    // Pasa "2,5", "2.5", "15.000" o "$ 15.000,50" a número. Si no es un número, devuelve -1 (y la validación lo rechaza).
    // Se arma a mano, cifra por cifra, para que funcione igual en cualquier compu
    // (en algunas la coma es el decimal y en otras es el punto).
    public static decimal LeerNumero(string texto)
    {
        texto = Funciones.Limpiar(texto).Replace("$", "").Replace(" ", "");
        // Cuántos puntos tiene: el largo del texto menos el largo sin los puntos
        int puntos = texto.Length - texto.Replace(".", "").Length;
        if (texto.IndexOf(",") >= 0)
        {
            // Con coma: la coma es el decimal y los puntos son de miles ("15.000,50")
            texto = texto.Replace(".", "").Replace(",", ".");
        }
        else if (puntos > 1 || (puntos == 1 && texto.Length - texto.IndexOf(".") == 4))
        {
            // Varios puntos, o uno solo con 3 cifras después: son de miles ("1.500.000", "15.000")
            texto = texto.Replace(".", "");
        }
        if (texto == "")
        {
            return -1;
        }

        decimal numero = 0;
        decimal divisor = 1;
        bool despuesDelPunto = false;
        for (int i = 0; i < texto.Length; i++)
        {
            string caracter = texto.Substring(i, 1);
            if (caracter == ".")
            {
                if (despuesDelPunto)
                {
                    return -1; // dos puntos decimales: no es un número
                }
                despuesDelPunto = true;
            }
            else
            {
                // La posición en "0123456789" es justo el valor de la cifra ("7" está en la posición 7)
                int cifra = "0123456789".IndexOf(caracter);
                if (cifra < 0)
                {
                    return -1; // una letra u otro símbolo
                }
                numero = numero * 10 + cifra;
                if (despuesDelPunto)
                {
                    divisor = divisor * 10;
                }
            }
        }
        // "2.5" se leyó como 25 con divisor 10: 25 / 10 = 2,5
        return numero / divisor;
    }
}
