namespace Fluenty.Info360.Models;

// Qué botones ve cada persona en el menú de arriba (el <nav class="menu"> de cada pantalla).
// Cada controller lo arma con Menu.Armar y lo manda en ViewBag.Menu.
public class Menu
{
    public bool EsDueno { get; set; }
    // Tiene rangos abajo: ve Cursos, Calculadora y Empleados
    public bool Gestiona { get; set; }
    public int CantidadCursos { get; set; }
    // Si tiene un solo curso, "Mi curso" lleva directo a ese
    public int IdCursoPropio { get; set; }
    // Nombre y apellido que se ven a la derecha del menú
    public string Nombre { get; set; }

    public static Menu Armar(int idUsuario, int idRango, bool esDueno)
    {
        BD bd = new BD();
        Menu menu = new Menu();
        menu.EsDueno = esDueno;
        menu.Gestiona = bd.ContarRangosAbajo(idRango) > 0;
        Usuarios usuario = bd.TraerEmpleado(idUsuario);
        menu.Nombre = usuario.Nombre + " " + usuario.Apellido;
        List<CursoInscripto> misCursos = bd.TraerCursosUsuario(idUsuario);
        menu.CantidadCursos = misCursos.Count;
        if (misCursos.Count == 1)
        {
            menu.IdCursoPropio = misCursos[0].IdCurso;
        }
        return menu;
    }
}
