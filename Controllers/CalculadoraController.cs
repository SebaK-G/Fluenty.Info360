using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Fluenty.Info360.Models;

namespace Fluenty.Info360.Controllers;

// Sprint 3: la calculadora de eficiencia. Solo la usa quien tiene gente a cargo (el dueño o un responsable).
// Calcula cuántas horas (y cuánta plata, si carga el valor de la hora) ahorra su equipo
// cuando usa la IA en una tarea, y cuánto ya estaría ahorrando con lo que avanzó en los cursos.
public class CalculadoraController : Controller
{
    // ============ La pantalla: las 4 preguntas, el resultado y los cálculos guardados ============
    // IdCalculo = el cálculo que se muestra como resultado (0 = ninguno todavía)
    public IActionResult Index(int IdCalculo = 0)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        List<int> idsDeAbajo = RangosDeAbajo(bd);
        if (idsDeAbajo.Count == 0)
        {
            return RedirectToAction("Index", "Home");
        }
        int idUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
        List<TiposTarea> tipos = bd.TraerTiposTarea();

        // Su gente: cuántos marcaron cada tarea en el formulario y cuánto avanzaron en promedio en sus cursos
        Dictionary<int, int> personasPorTarea = new Dictionary<int, int>();
        foreach (TiposTarea tipo in tipos)
        {
            personasPorTarea[tipo.IdTipoTarea] = 0;
        }
        int personas = 0;
        int enCurso = 0;
        int sumaProgreso = 0;
        foreach (EmpleadoCurso miembro in bd.TraerMiembrosEmpresa(idEmpresa))
        {
            if (miembro.IdRango != null && idsDeAbajo.Contains(miembro.IdRango.Value))
            {
                personas++;
                if (miembro.Tareas != null)
                {
                    foreach (TiposTarea tipo in tipos)
                    {
                        if (miembro.Tareas.Contains(tipo.Nombre))
                        {
                            personasPorTarea[tipo.IdTipoTarea]++;
                        }
                    }
                }
                if (miembro.EstadoCurso == "Confirmado" && miembro.Progreso != null)
                {
                    enCurso++;
                    sumaProgreso = sumaProgreso + miembro.Progreso.Value;
                }
            }
        }
        // El avance del equipo: el promedio de los que tienen curso, contando como 0 a los que todavía no
        int avance = 0;
        if (personas > 0)
        {
            avance = sumaProgreso / personas;
        }

        // El cálculo a mostrar (solo si es suyo)
        List<Calculos> calculos = bd.TraerCalculosUsuario(idUsuario);
        Calculos resultado = null;
        foreach (Calculos calculo in calculos)
        {
            if (calculo.IdCalculo == IdCalculo)
            {
                resultado = calculo;
            }
        }

        ViewBag.Tipos = tipos;
        ViewBag.PersonasPorTarea = personasPorTarea;
        ViewBag.Personas = personas;
        ViewBag.EnCurso = enCurso;
        ViewBag.Avance = avance;
        ViewBag.Calculos = calculos;
        ViewBag.Resultado = resultado;
        ViewBag.Error = HttpContext.Session.GetString("ErrorCalculo");
        HttpContext.Session.Remove("ErrorCalculo");
        return View("Index");
    }

    // ============ Calcular: hace la cuenta, la guarda y la muestra ============
    [HttpPost]
    public IActionResult Calcular(int IdTipoTarea, int Personas, string HorasSemana, string ValorHora)
    {
        // Los números llegan como texto: así "2,5" y "2.5" valen lo mismo en cualquier compu
        decimal horasSemana = LeerNumero(HorasSemana);
        decimal? valorHora = null;
        if (ValorHora != null && ValorHora.Trim() != "")
        {
            valorHora = LeerNumero(ValorHora);
        }
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        if (RangosDeAbajo(bd).Count == 0)
        {
            return RedirectToAction("Index", "Home");
        }

        TiposTarea tipo = null;
        foreach (TiposTarea item in bd.TraerTiposTarea())
        {
            if (item.IdTipoTarea == IdTipoTarea)
            {
                tipo = item;
            }
        }
        // Que los números tengan sentido (una semana tiene como mucho 48 horas de trabajo)
        if (tipo == null || Personas < 1 || Personas > 10000 || horasSemana <= 0 || horasSemana > 48 || (valorHora != null && valorHora < 0))
        {
            HttpContext.Session.SetString("ErrorCalculo", "Revisá los datos: elegí una tarea, al menos 1 persona y entre 0,5 y 48 horas por semana.");
            return RedirectToAction("Index");
        }

        Calculos calculo = new Calculos();
        calculo.IdUsuario = int.Parse(HttpContext.Session.GetString("IdUsuario"));
        calculo.IdTipoTarea = IdTipoTarea;
        calculo.Personas = Personas;
        calculo.HorasSemana = horasSemana;
        calculo.ValorHora = valorHora;
        calculo.PorcentajeAhorro = tipo.PorcentajeAhorro;
        calculo.Calcular();
        int idCalculo = bd.GuardarCalculo(calculo);
        return RedirectToAction("Index", new { IdCalculo = idCalculo });
    }

    // ============ Borrar un cálculo guardado ============
    [HttpPost]
    public IActionResult Borrar(int IdCalculo)
    {
        if (HttpContext.Session.GetString("IdUsuario") == null)
        {
            return RedirectToAction("Acceso", "Home");
        }
        BD bd = new BD();
        bd.BorrarCalculo(IdCalculo, int.Parse(HttpContext.Session.GetString("IdUsuario")));
        return RedirectToAction("Index");
    }

    // Pasa "2,5", "2.5" o "15.000" a número. Si no es un número, devuelve -1 (y la validación lo rechaza).
    private decimal LeerNumero(string texto)
    {
        if (texto == null)
        {
            return -1;
        }
        texto = texto.Trim().Replace("$", "").Replace(" ", "");
        // Con coma: la coma es el decimal y los puntos son de miles ("15.000,50")
        if (texto.Contains(","))
        {
            texto = texto.Replace(".", "").Replace(",", ".");
        }
        // Solo puntos y tres cifras después del último: son de miles ("15.000")
        else if (texto.Contains(".") && texto.Length - texto.LastIndexOf('.') == 4)
        {
            texto = texto.Replace(".", "");
        }
        decimal numero;
        if (decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out numero))
        {
            return numero;
        }
        return -1;
    }

    // Los rangos que tiene abajo (si no tiene ninguno, no puede usar la calculadora)
    private List<int> RangosDeAbajo(BD bd)
    {
        List<int> ids = new List<int>();
        string idRangoTexto = HttpContext.Session.GetString("IdRango");
        if (idRangoTexto != null && idRangoTexto != "")
        {
            int idEmpresa = int.Parse(HttpContext.Session.GetString("IdEmpresa"));
            foreach (Rangos rango in Arbol.Debajo(bd.TraerRangosEmpresa(idEmpresa), int.Parse(idRangoTexto)))
            {
                ids.Add(rango.IdRango);
            }
        }
        return ids;
    }
}
