
using Microsoft.SemanticKernel;

namespace Fluenty.Services;

public class GroqService
{
    private readonly Kernel _kernel;

    public GroqService(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> PreguntarAsync(string pregunta)
    {
        var respuesta = await _kernel.InvokePromptAsync(pregunta);

        return respuesta.ToString();
    }
}
