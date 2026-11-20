namespace Fluenty.Info360.Models;

// El Home: el mismo para todos los rangos.
// Si tiene rangos abajo, ve los números de su gente; si tiene cursos, ve su avance, XP y racha.
public static class LogicaHome
{
    public static ResumenEquipo ArmarResumen(int idEmpresa, int idRango)
    {
        ResumenEquipo resumen = new ResumenEquipo();
        resumen.Quietos = new List<string>();
        resumen.Actividades = new List<string>();

        string idsRangos = LogicaEmpleados.RangosDeAbajo(idEmpresa, idRango);
        if (idsRangos == "")
        {
            return resumen;
        }
        resumen.CantidadRangos = Funciones.Separar(idsRangos, ",").Count;

        // Los cursos distintos que hay entre su gente (para contarlos)
        List<int> cursos = new List<int>();
        foreach (EmpleadoCurso miembro in LogicaEmpleados.TraerEquipo(idsRangos))
        {
            resumen.CantidadPersonas++;
            if (miembro.EstadoCurso == "Confirmado")
            {
                resumen.EnCapacitacion++;
                if (!Funciones.EstaEnLista(cursos, miembro.IdCurso))
                {
                    cursos.Add(miembro.IdCurso);
                }
                // Actividad() dice si no empezó o hace una semana que no avanza ("" si va bien)
                if (miembro.Actividad() != "")
                {
                    resumen.Quietos.Add(miembro.Nombre + " " + miembro.Apellido + ": " + miembro.Actividad().ToLower());
                }
            }
            else if (miembro.TieneFormulario)
            {
                resumen.EsperandoCurso++;
            }
        }
        resumen.CantidadCursos = cursos.Count;

        BD bd = new BD();
        foreach (ActividadCurso actividad in bd.TraerActividadesRangos(idsRangos))
        {
            resumen.Actividades.Add("Curso \"" + actividad.Titulo + "\" asignado a " + actividad.Nombre + " " + actividad.Apellido);
        }
        return resumen;
    }

    // Sus cursos, cada uno con la próxima lección para hacer (null si ya lo terminó)
    public static List<CursoInscripto> TraerMisCursos(int idUsuario)
    {
        BD bd = new BD();
        List<CursoInscripto> misCursos = bd.TraerCursosUsuario(idUsuario);
        foreach (CursoInscripto curso in misCursos)
        {
            curso.Proxima = bd.TraerProximaLeccion(curso.IdCurso, idUsuario);
        }
        return misCursos;
    }
}
