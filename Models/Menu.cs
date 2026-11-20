namespace Fluenty.Info360.Models;

// Qué botones ve cada persona en el menú de arriba (Views/Shared/_NavApp.cshtml).
// Cada controller lo arma con Menu.Armar y lo manda en ViewBag.Menu.
public class Menu
{
    public bool EsDueno { get; set; }
    // Tiene rangos abajo: ve Equipo, Cursos y Empleados
    public bool Gestiona { get; set; }
    public int CantidadCursos { get; set; }
    // Si tiene un solo curso, "Mi curso" lleva directo a ese
    public int IdCursoPropio { get; set; }

    public static Menu Armar(int idUsuario, int idRango, bool esDueno)
    {
        BD bd = new BD();
        Menu menu = new Menu();
        menu.EsDueno = esDueno;
        menu.Gestiona = bd.ContarRangosAbajo(idRango) > 0;
        List<CursoInscripto> misCursos = bd.TraerCursosUsuario(idUsuario);
        menu.CantidadCursos = misCursos.Count;
        if (misCursos.Count == 1)
        {
            menu.IdCursoPropio = misCursos[0].IdCurso;
        }
        return menu;
    }
}
