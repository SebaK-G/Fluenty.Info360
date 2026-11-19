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
            if (Funciones.EstaEnLista(idsDeAbajo, miembro.IdRango))
            {
                personas++;
                if (miembro.Tareas != null)
                {
                    foreach (TiposTarea tipo in tipos)
                    {
                        // IndexOf devuelve -1 si no lo encuentra
                        if (miembro.Tareas.IndexOf(tipo.Nombre) >= 0)
                        {
                            personasPorTarea[tipo.IdTipoTarea]++;
                        }
                    }
                }
                if (miembro.EstadoCurso == "Confirmado")
                {
                    enCurso++;
                    sumaProgreso = sumaProgreso + miembro.Progreso;
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
        // 0 = no cargó el valor de la hora
        decimal valorHora = 0;
        if (!Funciones.EstaVacio(ValorHora))
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
        if (tipo == null || Personas < 1 || Personas > 10000 || horasSemana <= 0 || horasSemana > 48 || valorHora < 0)
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

    // Pasa "2,5", "2.5", "15.000" o "$ 15.000,50" a número. Si no es un número, devuelve -1 (y la validación lo rechaza).
    // Se arma a mano, cifra por cifra, para que funcione igual en cualquier compu
    // (en algunas la coma es el decimal y en otras es el punto).
    private decimal LeerNumero(string texto)
    {
        texto = Funciones.Limpiar(texto).Replace("$", "").Replace(" ", "");
        // Cuántos puntos tiene: el largo del texto menos el largo sin los puntos
        int puntos = texto.Length - texto.Replace(".", "").Length;
        if (texto.IndexOf(",") >= 0)
        {
            // Con coma: la coma es el decimal y los puntos son de miles ("15.000,50")
            texto = texto.Replace(".", "").Replace(",", ".");
        }
        else if (puntos > 1 || (puntos == 1 && texto.Length - texto.IndexOf(".") == 4))
        {
            // Varios puntos, o uno solo con 3 cifras después: son de miles ("1.500.000", "15.000")
            texto = texto.Replace(".", "");
        }
        if (texto == "")
        {
            return -1;
        }

        decimal numero = 0;
        decimal divisor = 1;
        bool despuesDelPunto = false;
        for (int i = 0; i < texto.Length; i++)
        {
            string caracter = texto.Substring(i, 1);
            if (caracter == ".")
            {
                if (despuesDelPunto)
                {
                    return -1; // dos puntos decimales: no es un número
                }
                despuesDelPunto = true;
            }
            else
            {
                // La posición en "0123456789" es justo el valor de la cifra ("7" está en la posición 7)
                int cifra = "0123456789".IndexOf(caracter);
                if (cifra < 0)
                {
                    return -1; // una letra u otro símbolo
                }
                numero = numero * 10 + cifra;
                if (despuesDelPunto)
                {
                    divisor = divisor * 10;
                }
            }
        }
        // "2.5" se leyó como 25 con divisor 10: 25 / 10 = 2,5
        return numero / divisor;
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
