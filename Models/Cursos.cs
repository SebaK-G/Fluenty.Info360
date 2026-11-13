namespace Fluenty.Info360.Models;

// El contenido del curso que arma la IA. Es para un rango (un grupo de personas);
// a quién está asignado y cómo va cada persona está en Inscripciones.
public class Cursos
{
    public int IdCurso { get; set; }
    // No son columnas de Cursos: las trae la consulta desde las personas que tienen el curso asignado
    public int IdEmpresa { get; set; }
    public int IdRango { get; set; }
    public string Titulo { get; set; }
    public string Descripcion { get; set; }
    public string Modelo { get; set; }
    public int Nivel { get; set; }
    public string Objetivos { get; set; }
    public string Estado { get; set; }
}
