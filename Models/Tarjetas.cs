namespace Fluenty.Info360.Models;

// Una tarjeta de una lección, hecha para una persona.
// Tipos que explican: gancho, explicacion, comparacion, chat, historia, cierre.
// Tipos que preguntan: opcion, verdaderofalso, completar, error, mejorrespuesta (tienen varias versiones con el mismo Orden).
// practica: "Probalo vos", la corrige la IA.
public class Tarjetas
{
    public int IdTarjeta { get; set; }
    public int IdLeccionXUsuario { get; set; }
    public int Orden { get; set; }
    public int Version { get; set; }
    public string Tipo { get; set; }
    public string Titulo { get; set; }
    public string ParaQue { get; set; }
    public string Texto { get; set; }
    public string Ejemplo { get; set; }
    public string Contexto { get; set; }
    public string PedidoMalo { get; set; }
    public string RespuestaMala { get; set; }
    public string PedidoBueno { get; set; }
    public string RespuestaBuena { get; set; }
    public string OpcionA { get; set; }
    public string OpcionB { get; set; }
    public string OpcionC { get; set; }
    public string Correcta { get; set; }
    public string Explicacion { get; set; }
    public string Resultado { get; set; }
    public int Intentos { get; set; }
    public int VecesReexplicada { get; set; }

    // No son columnas: las arma LogicaLeccion para que la vista solo tenga que mostrarlas
    public bool EsDePregunta { get; set; }
    // Si ya está resuelta: una tarjeta que solo explica, o una pregunta/práctica que ya hizo bien
    public bool Resuelta { get; set; }
    // El cartelito de arriba ("💡 Para arrancar", "🤔 ¿Cuál es mejor?"...)
    public string Etiqueta { get; set; }
    // Lo que se lee en voz alta con "Escuchar"
    public string TextoParaLeer { get; set; }
    // El ejemplo cortado en pasos (un renglón por paso)
    public List<string> PasosEjemplo { get; set; }

    // Los tipos que son preguntas (se responden eligiendo una opción)
    public static readonly string[] TiposPregunta = { "opcion", "verdaderofalso", "completar", "error", "mejorrespuesta" };

    public bool EsPregunta()
    {
        return Funciones.EstaEnArray(TiposPregunta, Tipo);
    }
}
