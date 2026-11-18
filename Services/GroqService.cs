using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Fluenty.Services;

// Una clave para llamar a la IA: la key, a qué dirección se manda y qué modelo usa.
// Así una clave de respaldo puede ser de Groq o de otro proveedor con el mismo formato de pedido.
public class ClaveIA
{
    public string Nombre { get; set; }
    public string ApiKey { get; set; }
    public string Url { get; set; }
    public string Modelo { get; set; }
}

// Habla con la IA mandando solicitudes HTTP (sin Semantic Kernel).
// Tiene una lista de claves: si una llega al límite (429) o deja de andar (401/402/403),
// el mismo pedido se reintenta con la siguiente.
public class GroqService
{
    private const string UrlGroq = "https://api.groq.com/openai/v1/chat/completions";
    private const string ModeloGroq = "openai/gpt-oss-120b";

    // La última clave que anduvo. Es static para que la recuerden todos los pedidos,
    // así no se empieza siempre por una clave que ya está agotada.
    private static int _claveActual = 0;

    private readonly HttpClient _httpClient;
    private readonly List<ClaveIA> _claves = new List<ClaveIA>();

    public GroqService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(120);

        // 1) La clave de siempre (User Secrets: "Groq:ApiKey")
        AgregarClave("Groq principal", configuration["Groq:ApiKey"], null, null);

        // 2) Claves de respaldo: "IA:Claves:0:ApiKey", "IA:Claves:1:ApiKey", ...
        //    Url y Modelo son opcionales (si no están, se usa Groq).
        foreach (IConfigurationSection seccion in configuration.GetSection("IA:Claves").GetChildren())
        {
            AgregarClave("Respaldo " + seccion.Key, seccion["ApiKey"], seccion["Url"], seccion["Modelo"]);
        }
    }

    private void AgregarClave(string nombre, string apiKey, string url, string modelo)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return;
        }
        ClaveIA clave = new ClaveIA();
        clave.Nombre = nombre;
        clave.ApiKey = apiKey.Trim();
        clave.Url = string.IsNullOrWhiteSpace(url) ? UrlGroq : url.Trim();
        clave.Modelo = string.IsNullOrWhiteSpace(modelo) ? ModeloGroq : modelo.Trim();
        _claves.Add(clave);
    }

    // Para la página de prueba: una pregunta y una respuesta en texto
    public async Task<string> PreguntarAsync(string pregunta)
    {
        return await PreguntarAsync("Respondé en español, claro y corto.", pregunta, false);
    }

    // instrucciones = el prompt fijo, datos = la información del empleado.
    // Si pedirJson es true, la IA está obligada a devolver un JSON válido.
    public async Task<string> PreguntarAsync(string instrucciones, string datos, bool pedirJson)
    {
        if (_claves.Count == 0)
        {
            throw new Exception("Falta configurar Groq:ApiKey en User Secrets.");
        }

        object[] mensajes = new object[]
        {
            new { role = "system", content = instrucciones },
            new { role = "user", content = datos }
        };

        // Se prueba cada clave como mucho una vez, empezando por la última que anduvo
        int inicio = _claveActual % _claves.Count;
        for (int intento = 0; intento < _claves.Count; intento++)
        {
            int indice = (inicio + intento) % _claves.Count;
            ClaveIA clave = _claves[indice];

            int estado;
            string texto;
            try
            {
                HttpResponseMessage respuesta = await Enviar(clave, mensajes, pedirJson);
                estado = (int)respuesta.StatusCode;
                texto = await respuesta.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("No se pudo contactar con la IA. Revisá la conexión o la configuración de la API.", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new InvalidOperationException("La llamada a la IA tardó demasiado y se canceló. Probá de nuevo más tarde.", ex);
            }

            // Límite alcanzado (429) o clave que no sirve (401 inválida, 402 sin créditos, 403 sin permiso):
            // se pasa a la siguiente clave y se reintenta el mismo pedido
            if (estado == 429 || estado == 401 || estado == 402 || estado == 403)
            {
                // Se ve en la terminal de dotnet watch: sirve para saber qué clave falló
                Console.WriteLine("[IA] La clave '" + clave.Nombre + "' respondió " + estado + ". Pruebo con la siguiente.");
                _claveActual = indice + 1;
                continue;
            }

            if (estado < 200 || estado > 299)
            {
                Console.WriteLine("[IA] La clave '" + clave.Nombre + "' respondió error " + estado + ": " + Recortar(texto));
                throw new InvalidOperationException("La IA respondió con error " + estado + ": " + Recortar(texto));
            }

            // Anduvo: queda como la clave a usar en los próximos pedidos
            _claveActual = indice;
            return LeerContenido(texto, clave);
        }

        // Ninguna clave pudo responder
        throw new InvalidOperationException("La IA está recibiendo muchos pedidos. Esperá unos minutos y probá de nuevo.");
    }

    // Arma y manda la solicitud POST con la key de esa clave
    private async Task<HttpResponseMessage> Enviar(ClaveIA clave, object[] mensajes, bool pedirJson)
    {
        // El cuerpo se arma con un Dictionary para poder agregar campos solo cuando hacen falta
        Dictionary<string, object> cuerpo = new Dictionary<string, object>();
        cuerpo["model"] = clave.Modelo;
        cuerpo["temperature"] = 0.4;
        cuerpo["messages"] = mensajes;
        if (pedirJson)
        {
            cuerpo["max_tokens"] = 8000;
            cuerpo["response_format"] = new { type = "json_object" };
        }
        else
        {
            cuerpo["max_tokens"] = 1500;
        }
        // Los modelos gpt-oss "piensan" antes de responder y eso gasta del máximo de tokens.
        // Con razonamiento bajo la respuesta no queda cortada y sale más rápido.
        if (clave.Modelo.Contains("gpt-oss"))
        {
            cuerpo["reasoning_effort"] = "low";
        }

        HttpRequestMessage pedido = new HttpRequestMessage(HttpMethod.Post, clave.Url);
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", clave.ApiKey);
        pedido.Content = new StringContent(JsonSerializer.Serialize(cuerpo), Encoding.UTF8, "application/json");
        return await _httpClient.SendAsync(pedido);
    }

    // Saca el texto de la respuesta (choices[0].message.content)
    private string LeerContenido(string texto, ClaveIA clave)
    {
        JsonDocument documento;
        try
        {
            documento = JsonDocument.Parse(texto);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("No se pudo leer la respuesta de la IA porque la API devolvió un formato inválido. Respuesta recibida: " + Recortar(texto), ex);
        }

        if (!documento.RootElement.TryGetProperty("choices", out JsonElement choices) ||
            choices.ValueKind != JsonValueKind.Array ||
            choices.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("La IA no devolvió una respuesta válida en el campo 'choices'.");
        }

        JsonElement primerChoice = choices[0];

        // Si se quedó sin tokens, la respuesta vino cortada (el JSON no cierra)
        if (primerChoice.TryGetProperty("finish_reason", out JsonElement motivo) &&
            motivo.ValueKind == JsonValueKind.String && motivo.GetString() == "length")
        {
            Console.WriteLine("[IA] La respuesta de '" + clave.Nombre + "' quedó cortada (se quedó sin tokens).");
            throw new InvalidOperationException("La respuesta de la IA quedó cortada porque era muy larga. Probá de nuevo.");
        }

        if (!primerChoice.TryGetProperty("message", out JsonElement message) ||
            !message.TryGetProperty("content", out JsonElement content) ||
            content.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("La IA devolvió una respuesta sin contenido útil para mostrar.");
        }

        string resultado = content.GetString();
        if (string.IsNullOrWhiteSpace(resultado))
        {
            throw new InvalidOperationException("La IA devolvió una respuesta vacía o sin texto.");
        }

        return resultado;
    }

    private string Recortar(string texto)
    {
        if (texto == null)
        {
            return "";
        }
        return texto.Length > 500 ? texto.Substring(0, 500) + "..." : texto;
    }
}