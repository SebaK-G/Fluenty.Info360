namespace Fluenty.Info360.Models;

// Estas 3 clases tienen la misma forma que el JSON que devuelve la IA (ver PROMPT-IA.md).
// Sirven solo para leer la respuesta; después se guarda en Cursos, Unidades y Lecciones.

public class CursoIA
{
    public string Modelo { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public List<string> Objetivos { get; set; } = new List<string>();
    public int UnidadInicial { get; set; }
    public string MotivoNivel { get; set; } = "";
    public List<UnidadIA> Unidades { get; set; } = new List<UnidadIA>();
    // Sprint 2: en qué unidad arranca cada persona del rango
    public List<NivelacionIA> Nivelacion { get; set; } = new List<NivelacionIA>();
}

public class UnidadIA
{
    public int Numero { get; set; }
    public string Titulo { get; set; } = "";
    public string Objetivo { get; set; } = "";
    public List<LeccionIA> Lecciones { get; set; } = new List<LeccionIA>();
}

public class LeccionIA
{
    public int Numero { get; set; }
    public string Titulo { get; set; } = "";
    public string Objetivo { get; set; } = "";
}

// ===== Sprint 2: nivelación de cada persona del rango =====
public class NivelacionIA
{
    public int IdUsuario { get; set; }
    public int UnidadInicial { get; set; }
    public string Motivo { get; set; } = "";
}

// ===== Sprint 2: el contenido de una lección (las tarjetas) =====
// "plan" es lo que la IA piensa ANTES de escribir (idea central, error común, comparación); no se muestra.
public class ContenidoLeccionIA
{
    public PlanLeccionIA Plan { get; set; } = new PlanLeccionIA();
    public List<TarjetaIA> Tarjetas { get; set; } = new List<TarjetaIA>();
}

public class PlanLeccionIA
{
    public string IdeaCentral { get; set; } = "";
    public string ErrorComun { get; set; } = "";
    public string Comparacion { get; set; } = "";
    public string ParaQue { get; set; } = "";
}

public class TarjetaIA
{
    public string Tipo { get; set; } = "";
    public string Subtipo { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string ParaQue { get; set; } = "";
    public string Texto { get; set; } = "";
    public string Ejemplo { get; set; } = "";
    public string Contexto { get; set; } = "";
    public string PedidoMalo { get; set; } = "";
    public string RespuestaMala { get; set; } = "";
    public string PedidoBueno { get; set; } = "";
    public string RespuestaBuena { get; set; } = "";
    public string OpcionA { get; set; } = "";
    public string OpcionB { get; set; } = "";
    public string OpcionC { get; set; } = "";
    public string Correcta { get; set; } = "";
    public string Explicacion { get; set; } = "";
    // Solo las preguntas: varias versiones del mismo tema (si se equivoca, le toca la siguiente)
    public List<TarjetaIA> Versiones { get; set; } = new List<TarjetaIA>();
}

public class TextoIA
{
    public string Titulo { get; set; } = "";
    public string Texto { get; set; } = "";
}

// La corrección de "Probalo vos"
public class CorreccionIA
{
    public bool Aprobado { get; set; }
    public string Devolucion { get; set; } = "";
}
