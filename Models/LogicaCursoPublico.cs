using Fluenty.Services;

namespace Fluenty.Info360.Models;

// Sprint 3: el buscador de cursos públicos (la pantalla Explorar).
// Cualquier persona logueada (de cualquier empresa) puede buscar un curso público, ver su temario e inscribirse.
// Al inscribirse queda en Inscripciones con Origen = 'Publico' y el curso aparece en "Mis cursos".
public static class LogicaCursoPublico
{
    // Busca los cursos y les arma lo que muestra la tarjeta (el nivel en palabras y la descripción corta)
    public static List<CursoPublico> Buscar(string texto, string categoria, int nivel, string orden, int idUsuario)
    {
        BD bd = new BD();
        List<CursoPublico> cursos = bd.BuscarCursosPublicos(Funciones.Limpiar(texto), Funciones.Limpiar(categoria), nivel, orden, idUsuario);
        foreach (CursoPublico curso in cursos)
        {
            curso.NombreNivel = CursoPublico.NivelEnPalabras(curso.Nivel);
            // Si la descripción es muy larga, se muestran los primeros 140 caracteres
            curso.DescripcionCorta = curso.Descripcion;
            if (curso.Descripcion != null && curso.Descripcion.Length > 140)
            {
                curso.DescripcionCorta = curso.Descripcion.Substring(0, 140) + "...";
            }
        }
        return cursos;
    }

    // Un curso se puede ver si es público y está confirmado (null si no)
    public static Cursos TraerCursoPublico(int idCurso)
    {
        BD bd = new BD();
        Cursos curso = bd.TraerCurso(idCurso);
        if (curso == null || !curso.Publico || curso.Estado != "Confirmado")
        {
            return null;
        }
        return curso;
    }

    // Las unidades del curso, cada una con sus lecciones (para el temario)
    public static List<Unidades> ArmarTemario(int idCurso)
    {
        BD bd = new BD();
        List<Unidades> unidades = bd.TraerUnidadesCurso(idCurso);
        List<Lecciones> lecciones = bd.TraerLeccionesCurso(idCurso);
        foreach (Unidades unidad in unidades)
        {
            unidad.Lecciones = new List<Lecciones>();
            foreach (Lecciones leccion in lecciones)
            {
                if (leccion.NumeroUnidad == unidad.Numero)
                {
                    unidad.Lecciones.Add(leccion);
                }
            }
        }
        return unidades;
    }

    // Le carga todas las lecciones del curso, nivelado por lo que contó de su experiencia con la IA.
    // Devuelve el error para mostrar, o "" si quedó inscripto (o ya lo estaba).
    public static string Inscribirse(int idCurso, int idUsuario, GroqService groqService, bool iaPrendida)
    {
        if (TraerCursoPublico(idCurso) == null)
        {
            return "Ese curso no existe o no es público.";
        }
        BD bd = new BD();
        // Si ya lo tiene (por su equipo o porque ya se inscribió), no hay nada que hacer
        if (bd.EstaInscripto(idCurso, idUsuario))
        {
            return "";
        }
        FormulariosEmpleado form = bd.TraerFormularioEmpleado(idUsuario);
        if (form == null)
        {
            return "Para inscribirte hace falta tu formulario de empleado.";
        }

        // Arranca en la unidad que corresponde a lo que contó en su formulario sobre la IA
        Inscripciones inscripcion = new Inscripciones();
        inscripcion.IdCurso = idCurso;
        inscripcion.IdUsuario = idUsuario;
        inscripcion.UnidadInicial = DatosIA.UnidadPorNivel(form.NivelIA);
        inscripcion.MotivoNivel = "Arrancás según lo que contaste en tu formulario sobre tu experiencia con la IA.";
        inscripcion.Origen = "Publico";
        int idInscripcion = bd.AsignarCurso(inscripcion);
        foreach (Lecciones leccion in bd.TraerLeccionesCurso(idCurso))
        {
            bd.AgregarLeccionUsuario(leccion.IdLeccion, idInscripcion, Lecciones.EstadoInicial(leccion.NumeroUnidad, leccion.Numero, inscripcion.UnidadInicial));
        }
        bd.ActualizarProgreso(idCurso, idUsuario);

        // Se le deja preparada su primera lección mientras mira el curso
        if (iaPrendida)
        {
            GeneradorLecciones.PrepararEnSegundoPlano(groqService, idUsuario, GeneradorLecciones.Siguientes(bd, idCurso, idUsuario, 0, 1));
        }
        return "";
    }
}
