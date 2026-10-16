
using Fluenty.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fluenty.Controllers;

public class IAController : Controller
{
    private readonly GroqService _groqService;

    public IAController(GroqService groqService)
    {
        _groqService = groqService;
    }

    public async Task<IActionResult> Index()
    {
        var respuesta = await _groqService.PreguntarAsync(
            "Explicá en dos oraciones qué es la inteligencia artificial."
        );

        return Content(respuesta);
    }
}
