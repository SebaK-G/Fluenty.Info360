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

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(string pregunta)
    {
        if (!_groqFeatureToggle.Enabled)
        {
            ViewBag.Respuesta = "Groq está deshabilitado temporalmente.";
            return View();
        }

        var respuesta = await _groqService.PreguntarAsync(pregunta);
        ViewBag.Respuesta = respuesta;
        return View();
    }
}
