namespace Fluenty.Info360.Models;

// Lo que devuelven las acciones de LeccionController que se llaman con fetch desde wwwroot/js/leccion.js.
// Cada clase tiene solo lo que el JavaScript necesita (por ejemplo, nunca manda la respuesta correcta de una pregunta nueva).

// La otra pregunta del mismo tema que le toca si se equivocó
public class PreguntaNueva
{
    public int IdTarjeta { get; set; }
    public string Titulo { get; set; }
    public string Texto { get; set; }
    public string Contexto { get; set; }
    public string OpcionA { get; set; }
    public string OpcionB { get; set; }
    public string OpcionC { get; set; }
}

// Lo que devuelve Responder
public class RespuestaPregunta
{
    public string Error { get; set; }
    public bool Bien { get; set; }
    public string Correcta { get; set; }
    public string Explicacion { get; set; }
    // Cada 3 errores en el mismo tema, el tema explicado de otra forma
    public string Reexplicacion { get; set; }
    public PreguntaNueva Siguiente { get; set; }
}

// Lo que devuelve Practicar ("Probalo vos")
public class RespuestaPractica
{
    public string Error { get; set; }
    public bool Aprobado { get; set; }
    public string Devolucion { get; set; }
    public int Intentos { get; set; }
    // Un ejemplo de pedido bien hecho (después de 3 intentos)
    public string Ejemplo { get; set; }
}

// Lo que devuelve Reexplicar ("Explicamelo más simple" / "Dame otro ejemplo")
public class RespuestaReexplicar
{
    public bool Ok { get; set; }
    public string Mensaje { get; set; }
    // true si llegó al tope de pedidos a la IA del día
    public bool SinPedidos { get; set; }
    public string Titulo { get; set; }
    public string Texto { get; set; }
    public int RestantesTarjeta { get; set; }
    public int RestantesHoy { get; set; }
}

// El resultado de terminar una lección (lo muestra Leccion/Completada)
public class ResultadoLeccion
{
    public int Nota { get; set; }
    public int AciertosPrimera { get; set; }
    public int Preguntas { get; set; }
    public bool EsExamen { get; set; }
    public int NotaMinima { get; set; }
    public bool Aprobo { get; set; }
    public int XPGanado { get; set; }
    public bool RachaSubio { get; set; }
    // La próxima lección disponible (null si no hay)
    public Lecciones Proxima { get; set; }
    // "saltar", "bajar" o "" (nada)
    public string Ofrecer { get; set; }
    // Si le falta resolver algo, el Orden de esa tarjeta (0 = no le falta nada)
    public int OrdenPendiente { get; set; }
}
