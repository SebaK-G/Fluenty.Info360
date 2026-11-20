namespace Fluenty.Info360.Models;

// Todo lo que muestra Curso/Ver (lo arma LogicaCurso.ArmarCamino):
// el curso y el camino de una persona (sus unidades y lecciones con el estado de cada una).
public class CaminoCurso
{
    public Cursos Curso { get; set; }
    // La inscripción de la persona que se mira (null si nadie del equipo tiene el curso todavía)
    public Inscripciones Asignacion { get; set; }
    // true = la persona mira su propio curso; false = un responsable mira el curso de un rango de abajo
    public bool ModoAlumno { get; set; }
    // Si se sumó desde el buscador, el curso no es de su rango: es un curso público
    public bool EsInscripcionPublica { get; set; }
    // La línea de arriba del título: "Modelo · Rango Vendedor · 🌎 Público · Categoría"
    public string Subtitulo { get; set; }
    // Las personas del equipo que tienen el curso (para elegir de quién mirar el camino)
    public List<Usuarios> Personas { get; set; }
    public string NombreEmpleado { get; set; }
    public int XP { get; set; }
    public int Racha { get; set; }
    public int UnidadInicial { get; set; }
    // "Arrancás en la Unidad 2" o "Juan Pérez arranca en la Unidad 2"
    public string TextoArranque { get; set; }
    public List<Unidades> Unidades { get; set; }
}
