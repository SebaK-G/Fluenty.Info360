
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<Kernel>(sp => new Kernel(sp));
builder.Services.AddTransient<Fluenty.Services.GroqService>();

// Configuración de la IA con Groq.
// El conector OpenAI permite utilizar endpoints compatibles.
#pragma warning disable SKEXP0010
builder.Services.AddOpenAIChatCompletion(
    modelId: "openai/gpt-oss-20b",
    apiKey: builder.Configuration["Groq:ApiKey"]
        ?? throw new InvalidOperationException(
            "Falta configurar Groq:ApiKey en User Secrets."
        ),
    endpoint: new Uri("https://api.groq.com/openai/v1")
);
#pragma warning restore SKEXP0010

builder.Services.AddSession();

// Servicios MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configuración del pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
).WithStaticAssets();

app.Run();
