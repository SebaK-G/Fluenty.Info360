
using Fluenty.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fluenty.Controllers;

public class IAController : Controller
{
    private readonly GroqService _groqService;
    private readonly GroqFeatureToggle _groqFeatureToggle;

    public IAController(GroqService groqService, GroqFeatureToggle groqFeatureToggle)
    {
        _groqService = groqService;
        _groqFeatureToggle = groqFeatureToggle;
    }

    public async Task<IActionResult> Index()
    {
        if (!_groqFeatureToggle.Enabled)
        {
            return Content(
                "Groq está deshabilitado temporalmente. Cuando se habilite tu acceso, cambiá groqHabilitado a true en Program.cs."
            );
        }

        var respuesta = await _groqService.PreguntarAsync(
            "Explicá en dos oraciones qué es la inteligencia artificial."
        );

        return Content(respuesta);
    }
}
