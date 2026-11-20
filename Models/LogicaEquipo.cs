namespace Fluenty.Info360.Models;

// La pantalla Equipo: el árbol de rangos de la empresa, crear rangos y contar qué hace cada uno.
public static class LogicaEquipo
{
    // El dueño ve todo el árbol; los demás, su rango y lo que tienen abajo.
    // Devuelve una lista vacía si la persona no tiene rangos abajo (no tiene nada para ver acá).
    public static List<Rangos> ArmarArbol(int idEmpresa, int idRango, bool esDueno, string linkBase)
    {
        BD bd = new BD();
        List<Rangos> todos = bd.TraerArbolEmpresa(idEmpresa);
        List<Rangos> arbol = new List<Rangos>();
        if (esDueno)
        {
            arbol = Arbol.Ordenado(todos);
        }
        else if (bd.ContarRangosAbajo(idRango) > 0)
        {
            arbol = Arbol.ConSuArbol(todos, idRango);
        }

        foreach (Rangos rango in arbol)
        {
            rango.EsPropio = rango.IdRango == idRango;
            rango.Link = linkBase + rango.Codigo;
            // Uri.EscapeDataString pasa el texto a un formato que se puede poner en un link (los espacios, los ":"...)
            rango.LinkWhatsApp = "https://wa.me/?text=" + Uri.EscapeDataString("Sumate a nuestro equipo en Fluenty como " + rango.Nombre + ": " + rango.Link);
            rango.NivelColor = rango.Profundidad;
            if (rango.NivelColor > 4)
            {
                rango.NivelColor = 4;
            }
            rango.Sangria = "";
            for (int i = 0; i < rango.Profundidad; i++)
            {
                rango.Sangria = rango.Sangria + "—";
            }
        }
        return arbol;
    }

    // Crea un rango abajo de otro. Devuelve el error para mostrar, o "" si salió bien.
    public static string CrearRango(int idEmpresa, string nombre, int idRangoPadre, string tareas)
    {
        if (Funciones.EstaVacio(nombre))
        {
            return "Escribí el nombre del rango.";
        }
        if (Funciones.Limpiar(nombre).Length > 50)
        {
            return "El nombre del rango puede tener hasta 50 letras.";
        }
        if (Funciones.Limpiar(tareas).Length > 300)
        {
            return "Lo que hace el rango puede tener hasta 300 letras.";
        }
        BD bd = new BD();
        // El rango de arriba tiene que existir y ser de su empresa
        Rangos padre = bd.TraerRango(idRangoPadre);
        if (padre == null || padre.IdEmpresa != idEmpresa)
        {
            return "Elegí abajo de qué rango va.";
        }
        Rangos nuevo = new Rangos();
        nuevo.IdEmpresa = idEmpresa;
        nuevo.Nombre = Funciones.Limpiar(nombre);
        nuevo.IdRangoPadre = idRangoPadre;
        // Guid.NewGuid arma un código único: es el link para sumarse a este rango
        nuevo.Codigo = Guid.NewGuid().ToString();
        nuevo.Tareas = Funciones.Limpiar(tareas);
        bd.CrearRango(nuevo);
        return "";
    }

    // Guarda qué hace la gente de un rango. Si alguien llena mal su formulario, la IA arma el curso con esto.
    public static Resultado GuardarTareas(int idEmpresa, int idRango, string tareas)
    {
        string texto = Funciones.Limpiar(tareas);
        if (texto.Length > 300)
        {
            return new Resultado(false, "Puede tener hasta 300 letras.");
        }
        BD bd = new BD();
        Rangos rango = bd.TraerRango(idRango);
        if (rango == null || rango.IdEmpresa != idEmpresa)
        {
            return new Resultado(false, "Ese rango no es de tu empresa.");
        }
        bd.ActualizarTareasRango(idRango, texto);
        return new Resultado(true, "Guardado ✓");
    }
}
