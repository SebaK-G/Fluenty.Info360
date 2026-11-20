namespace Fluenty.Info360.Models;

// Sprint 3: la calculadora de eficiencia. Solo la usa quien tiene gente a cargo (el dueño o un responsable).
// Calcula cuántas horas (y cuánta plata, si carga el valor de la hora) ahorra su equipo
// cuando usa la IA en una tarea, y cuánto ya estaría ahorrando con lo que avanzó en los cursos.
public static class LogicaCalculadora
{
    // Las tareas que se pueden calcular, cada una con cuántas personas de su equipo la marcaron en el formulario
    public static List<TiposTarea> TraerTareas(List<EmpleadoCurso> equipo)
    {
        BD bd = new BD();
        List<TiposTarea> tipos = bd.TraerTiposTarea();
        foreach (TiposTarea tipo in tipos)
        {
            tipo.CantidadPersonas = 0;
            foreach (EmpleadoCurso miembro in equipo)
            {
                // Tareas es el texto con todas las tareas que marcó. IndexOf devuelve -1 si no la encuentra.
                if (miembro.Tareas != null && miembro.Tareas.IndexOf(tipo.Nombre) >= 0)
                {
                    tipo.CantidadPersonas++;
                }
            }
        }
        return tipos;
    }

    public static int ContarConCurso(List<EmpleadoCurso> equipo)
    {
        int conCurso = 0;
        foreach (EmpleadoCurso miembro in equipo)
        {
            if (miembro.EstadoCurso == "Confirmado")
            {
                conCurso++;
            }
        }
        return conCurso;
    }

    // El avance del equipo: el promedio de los que tienen curso, contando como 0 a los que todavía no
    public static int CalcularAvance(List<EmpleadoCurso> equipo)
    {
        if (equipo.Count == 0)
        {
            return 0;
        }
        int sumaProgreso = 0;
        foreach (EmpleadoCurso miembro in equipo)
        {
            if (miembro.EstadoCurso == "Confirmado")
            {
                sumaProgreso = sumaProgreso + miembro.Progreso;
            }
        }
        return sumaProgreso / equipo.Count;
    }

    // Revisa los datos, hace la cuenta y la guarda. Devuelve el error para mostrar, o "" si salió bien.
    // calculo.IdCalculo queda con el Id del cálculo guardado.
    public static string Calcular(Calculos calculo, string horasSemana, string valorHora)
    {
        // Los números llegan como texto: así "2,5" y "2.5" valen lo mismo en cualquier compu
        calculo.HorasSemana = Funciones.LeerNumero(horasSemana);
        // 0 = no cargó el valor de la hora
        calculo.ValorHora = 0;
        if (!Funciones.EstaVacio(valorHora))
        {
            calculo.ValorHora = Funciones.LeerNumero(valorHora);
        }

        BD bd = new BD();
        TiposTarea tipo = bd.TraerTipoTarea(calculo.IdTipoTarea);
        // Que los números tengan sentido (una semana tiene como mucho 48 horas de trabajo)
        if (tipo == null || calculo.Personas < 1 || calculo.Personas > 10000 || calculo.HorasSemana <= 0 || calculo.HorasSemana > 48 || calculo.ValorHora < 0)
        {
            return "Revisá los datos: elegí una tarea, al menos 1 persona y entre 0,5 y 48 horas por semana.";
        }
        calculo.PorcentajeAhorro = tipo.PorcentajeAhorro;
        calculo.Calcular();
        calculo.IdCalculo = bd.GuardarCalculo(calculo);
        return "";
    }
}
