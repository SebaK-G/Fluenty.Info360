namespace Fluenty.Info360.Models;

// Sprint 3 · Calculadora de eficiencia: cada cálculo que hace un responsable queda guardado.
// Cuelga de Usuarios (quién lo hizo) y de TiposTarea (qué tarea), como HerramientasXUsuario: no forma ciclos.
public class Calculos
{
    // Semanas que tiene un mes en promedio (52 semanas / 12 meses)
    public const decimal SemanasPorMes = 4.33m;
    // Horas de una jornada de trabajo
    public const decimal HorasPorDia = 8;

    public int IdCalculo { get; set; }
    public int IdUsuario { get; set; }
    public int IdTipoTarea { get; set; }
    // Viene del JOIN con TiposTarea (para mostrar el historial)
    public string NombreTarea { get; set; }

    // Las respuestas
    public int Personas { get; set; }
    public decimal HorasSemana { get; set; }
    // 0 = no cargó el valor de la hora (entonces no se calcula la plata)
    public decimal ValorHora { get; set; }

    // Los resultados (se guardan para que el historial no cambie si después se cambia el porcentaje)
    public int PorcentajeAhorro { get; set; }
    public decimal HorasAhorradasMes { get; set; }
    // 0 = no se calculó (no cargó el valor de la hora)
    public decimal PlataAhorradaMes { get; set; }
    public DateTime Fecha { get; set; }

    // La cuenta: horas de la tarea en el mes (todas las personas) por el porcentaje que ahorra la IA.
    // No se redondea acá: la base guarda las horas con 1 decimal y la plata sin decimales,
    // y la vista muestra los números redondeados con ToString("0.#") y ToString("N0").
    public void Calcular()
    {
        decimal horasTareaMes = Personas * HorasSemana * SemanasPorMes;
        HorasAhorradasMes = horasTareaMes * PorcentajeAhorro / 100;
        PlataAhorradaMes = 0;
        if (ValorHora > 0)
        {
            PlataAhorradaMes = HorasAhorradasMes * ValorHora;
        }
    }

    public decimal HorasAhorradasSemana()
    {
        return HorasAhorradasMes / SemanasPorMes;
    }

    // Las horas ahorradas en el mes, contadas en jornadas de trabajo
    public decimal DiasAhorradosMes()
    {
        return HorasAhorradasMes / HorasPorDia;
    }

    // 0 si no cargó el valor de la hora
    public decimal PlataAhorradaAnio()
    {
        return PlataAhorradaMes * 12;
    }

    // Lo que ya se estaría ahorrando hoy, según cuánto avanzó el equipo en sus cursos (avance de 0 a 100)
    public decimal HorasAhorradasHoy(int avance)
    {
        return HorasAhorradasMes * avance / 100;
    }
}
