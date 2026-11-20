namespace Fluenty.Info360.Models;

// La gente que una persona tiene a cargo: los de todos los rangos que están abajo del suyo.
// La usan la pantalla Empleados y el Home.
public static class LogicaEmpleados
{
    // Los Id de los rangos que están abajo de idRango, separados por coma ("2,3,5").
    // Así se los pasamos a los Stored Procedures que filtran por esos rangos.
    // Devuelve "" si no tiene rangos abajo.
    public static string RangosDeAbajo(int idEmpresa, int idRango)
    {
        BD bd = new BD();
        // El árbol se recorre en C# (Arbol.cs) porque cada rango puede tener rangos abajo, y esos otros más
        List<string> ids = new List<string>();
        foreach (Rangos rango in Arbol.Debajo(bd.TraerRangosEmpresa(idEmpresa), idRango))
        {
            ids.Add(rango.IdRango.ToString());
        }
        return Funciones.Unir(ids, ",");
    }

    // Las personas de los rangos de abajo, con su curso y su avance
    public static List<EmpleadoCurso> TraerEquipo(string idsRangos)
    {
        if (idsRangos == "")
        {
            return new List<EmpleadoCurso>();
        }
        BD bd = new BD();
        return bd.TraerMiembrosRangos(idsRangos);
    }
}
