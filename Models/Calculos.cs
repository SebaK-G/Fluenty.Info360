namespace Fluenty.Info360.Models;

// Sprint 3 · Calculadora de eficiencia: cada cálculo que hace un responsable queda guardado.
// Cuelga de Usuarios (quién lo hizo) y de TiposTarea (qué tarea), como HerramientasXUsuario: no forma ciclos.
public class Calculos
{
    // Semanas que tiene un mes en promedio (52 semanas / 12 meses)
    public const double SemanasPorMes = 4.33;
    // Horas de una jornada de trabajo
    public const double HorasPorDia = 8;

    public int IdCalculo { get; set; }
    public int IdUsuario { get; set; }
    public int IdTipoTarea { get; set; }
    // Viene del JOIN con TiposTarea (para mostrar el historial)
    public string NombreTarea { get; set; }

    // Las respuestas
    public int Personas { get; set; }
    public decimal HorasSemana { get; set; }
    public decimal? ValorHora { get; set; }

    // Los resultados (se guardan para que el historial no cambie si después se cambia el porcentaje)
    public int PorcentajeAhorro { get; set; }
    public decimal HorasAhorradasMes { get; set; }
    public decimal? PlataAhorradaMes { get; set; }
    public DateTime Fecha { get; set; }

    // La cuenta: horas de la tarea en el mes (todas las personas) por el porcentaje que ahorra la IA
    public void Calcular()
    {
        double horasTareaMes = Personas * (double)HorasSemana * SemanasPorMes;
        HorasAhorradasMes = (decimal)Math.Round(horasTareaMes * PorcentajeAhorro / 100, 1);
        PlataAhorradaMes = null;
        if (ValorHora != null && ValorHora > 0)
        {
            PlataAhorradaMes = Math.Round(HorasAhorradasMes * ValorHora.Value, 0);
        }
    }

    public decimal HorasAhorradasSemana()
    {
        return Math.Round(HorasAhorradasMes / (decimal)SemanasPorMes, 1);
    }

    // Las horas ahorradas en el mes, contadas en jornadas de trabajo
    public decimal DiasAhorradosMes()
    {
        return Math.Round(HorasAhorradasMes / (decimal)HorasPorDia, 1);
    }

    public decimal? PlataAhorradaAnio()
    {
        if (PlataAhorradaMes == null)
        {
            return null;
        }
        return PlataAhorradaMes.Value * 12;
    }

    // Lo que ya se estaría ahorrando hoy, según cuánto avanzó el equipo en sus cursos (avance de 0 a 100)
    public decimal HorasAhorradasHoy(int avance)
    {
        return Math.Round(HorasAhorradasMes * avance / 100, 1);
    }
}
