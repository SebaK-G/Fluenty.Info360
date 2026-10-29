var builder = WebApplication.CreateBuilder(args);
var groqHabilitado = true;

// IA: GroqService habla con Groq por solicitudes HTTP (sin Semantic Kernel).
// La API key va en User Secrets: dotnet user-secrets set "Groq:ApiKey" "TU_KEY"
builder.Services.AddHttpClient<Fluenty.Services.GroqService>();
builder.Services.AddSingleton(new Fluenty.Services.GroqFeatureToggle { Enabled = groqHabilitado });

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
