namespace Fluenty.Info360.Models;

// El contenido del curso que arma la IA. Lo genera un rango para su gente (Origen = 'Equipo' en Inscripciones);
// si es público, además aparece en el buscador y cualquiera se puede inscribir (Origen = 'Publico').
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

    // Sprint 3: cursos públicos (aparecen en el buscador para usuarios de todas las empresas)
    public bool Publico { get; set; }
    public string Categoria { get; set; }
    public int DuracionHoras { get; set; }
    public string Herramientas { get; set; }

    // Las categorías posibles (los chips del buscador)
    public static readonly string[] Categorias = { "Automatización", "Productividad", "Personalización", "Atención al cliente", "Datos y reportes", "Marketing y contenido" };
}
