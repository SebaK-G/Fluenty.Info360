namespace Fluenty.Info360.Models;

// Sprint 3 · Calculadora: las tareas que se pueden calcular (las mismas del formulario de empleado).
// PorcentajeAhorro = cuánto tiempo de esa tarea se ahorra usando bien la IA (una estimación que se cambia en la base).
public class TiposTarea
{
    public int IdTipoTarea { get; set; }
    public string Nombre { get; set; }
    public int PorcentajeAhorro { get; set; }
    // Un ejemplo corto de cómo ayuda la IA en esa tarea
    public string Ejemplo { get; set; }
}
