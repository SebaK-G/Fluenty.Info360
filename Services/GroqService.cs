using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Fluenty.Services;

// Habla con Groq mandando solicitudes HTTP (sin Semantic Kernel).
public class GroqService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string Url = "https://api.groq.com/openai/v1/chat/completions";
    private const string Modelo = "openai/gpt-oss-20b";

    public GroqService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(90);
        _apiKey = configuration["Groq:ApiKey"] ?? "";
    }

    // Para la página de prueba: una pregunta y una respuesta en texto
    public async Task<string> PreguntarAsync(string pregunta)
    {
        return await PreguntarAsync("Respondé en español, claro y corto.", pregunta, false);
    }

    // instrucciones = el prompt fijo, datos = la información del empleado.
    // Si pedirJson es true, Groq está obligado a devolver un JSON válido.
    public async Task<string> PreguntarAsync(string instrucciones, string datos, bool pedirJson)
    {
        if (_apiKey == "")
        {
            throw new Exception("Falta configurar Groq:ApiKey en User Secrets.");
        }

        // 1) Armamos el cuerpo del pedido
        object cuerpo;
        object[] mensajes = new object[]
        {
            new { role = "system", content = instrucciones },
            new { role = "user", content = datos }
        };
        if (pedirJson)
        {
            cuerpo = new { model = Modelo, temperature = 0.4, max_tokens = 6000, response_format = new { type = "json_object" }, messages = mensajes };
        }
        else
        {
            cuerpo = new { model = Modelo, temperature = 0.4, max_tokens = 1500, messages = mensajes };
        }

        // 2) Mandamos la solicitud POST con la API key
        HttpRequestMessage pedido = new HttpRequestMessage(HttpMethod.Post, Url);
        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        pedido.Content = new StringContent(JsonSerializer.Serialize(cuerpo), Encoding.UTF8, "application/json");

        HttpResponseMessage respuesta = await _httpClient.SendAsync(pedido);
        string texto = await respuesta.Content.ReadAsStringAsync();

        // 3) Errores comunes
        if ((int)respuesta.StatusCode == 429)
        {
            throw new Exception("La IA está recibiendo muchos pedidos. Esperá unos segundos y probá de nuevo.");
        }
        if (!respuesta.IsSuccessStatusCode)
        {
            throw new Exception("Groq respondió con error " + (int)respuesta.StatusCode + ": " + texto);
        }

        // 4) Sacamos el texto de la IA: choices[0].message.content
        JsonDocument documento = JsonDocument.Parse(texto);
        return documento.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
    }
}
