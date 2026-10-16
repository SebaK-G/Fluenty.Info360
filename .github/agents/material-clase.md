# Material autorizado y obligatorio de clase

Este archivo conserva el material técnico aportado por el usuario para usarlo como referencia cerrada. No se debe sustituir por conocimiento externo ni ampliarse sin autorización expresa.

==================== INICIO DEL MATERIAL DE CLASE ====================

## Diapositiva 1: Métodos para los tipos de datos STRING

**REPASO**

Declaración y asignación (definir):
```csharp
string texto = "Hola Mundo";
```

**NOVEDAD!!**

Algunos tipos de datos tienen métodos y propiedades que nos pueden ser muy útiles!
```csharp
int largo = texto.Length;
string porcionTexto = Texto.SubString(0,5);
int posicionSeparacionPalabra = Texto.IndexOf(" ");
string nuevoTexto = texto.Replace("Hola", "Chau");
string textoMayuscula = texto.ToUpper();
string textoMinuscula = texto.ToLower();
char letra = texto; // Un texto es una cadena de chars!!!
```

## Diapositiva 2: Tipo de datos DateTime

**Declaración:**

DateTime es un tipo de dato que nos sirve para representar fechas y horas. Se maneja como un objeto.
```csharp
DateTime miCumple;
```

**Asignación:** Como podemos asignar una fecha a un OBJETO de TIPO DateTime
```csharp
miCumple = new DateTime(2009,02,25);
DateTime fechaHoraNacimiento = new DateTime(2009,02,25,9,20,0); // DD-MM-YYYY HH:MM:SS
miCumple = new DateTime(); // Todavía no se la fecha (constructor vacío)
DateTime Hoy = DateTime.Today; // Me devuelve el día en que se está ejecutando el programa
DateTime HoyyAhora = DateTime.Now; // Día y hora en que se está ejecutando el programa
```

## Diapositiva 3: Colecciones

**Definición:** Una colección de datos es una estructura que agrupa múltiples elementos de un mismo tipo, permitiendo su manipulación de manera organizada y eficiente.

¿Qué tipos de datos vimos hasta ahora?
- **LISTAS:** Que pueden tener un tamaño ilimitado de elementos.
- **ARRAYS:** En la definición indicamos cuántos elementos va a tener.

Preguntas de repaso:
- ¿Cómo se definían?
- ¿Cómo se llenaban?
- ¿Cómo se vacía una colección?
- ¿Cómo se elimina un elemento?
- ¿Cómo se recorrían?

## Diapositiva 4: Colecciones - Dictionary (Definición Teórica)

**DEFINICIÓN TEÓRICA:** Un diccionario es una colección de datos que almacena elementos en forma de pares clave-valor, donde cada clave es única y se asocia a un valor específico. Los diccionarios permiten acceder, modificar, agregar o eliminar elementos de manera eficiente.

**CONSIDERACIONES IMPORTANTES:**
- **Claves:** Se le define el tipo de dato. Cuando creamos elementos en el diccionario las claves de un elemento no pueden repetirse dentro de la colección.
- **Valor:** También se le define un tipo de dato y al crear un elemento generamos una clave lo asociamos a dicho valor.
- **Búsqueda:** Utilizaremos la clave para buscar un valor deseado.
- **Recorrido:** Podemos recorrer el diccionario desde las claves o desde los valores para obtener todos los elementos.

## Diapositiva 5: Colecciones - Dictionary (Definición Técnica)

**DEFINICIÓN TÉCNICA:**
```csharp
Dictionary<int, Alumno> dicAlumnos = new Dictionary<int, Alumno>();
```

En este caso estamos creando un diccionario de alumnos donde cada elemento tendrá una clave numérica y un valor del tipo Alumno. Este ejemplo nos puede servir para guardar el DNI como clave del alumno y toda la info de un alumno como Valor.

**PEQUEÑAS DIFERENCIAS CON LISTAS EN SU UTILIZACIÓN:**
```csharp
dicAlumnos.Add(dni,objAlumno); // Agregar Elemento
dicAlumnos[dni] // Obtener un valor x su clave
dicAlumnos.ContainsKey(dni) // Búsqueda
dicAlumnos.Remove(dni) // Remover
```

## Diapositiva 6: Colecciones - Dictionary (Recorridos)

**RECORRER DICCIONARIO desde sus claves:**
```csharp
foreach (int clave in dicAlumnos.Keys)
{
    Console.WriteLine(clave); // imprime el dni
    Console.WriteLine(dicAlumnos[clave]); // imprime su valor
}
```

**RECORRER DICCIONARIO desde sus valores:**
```csharp
foreach (Alumno objAlum in dicAlumnos.Values)
{
    Console.WriteLine(objAlum.Nombre); // imprime el nombre
    // NO TENGO ACCESO CON ESTE RECORRIDO A LA CLAVE
}
```

## Diapositiva 7: Colecciones - Dictionary (Iteración Completa)

También es posible iterar teniendo en cuenta todo el par [clave, valor]:
```csharp
foreach (KeyValuePair<int, Alumno> alumno in dicAlumnos)
{
    Console.WriteLine(alumno.Key); // imprime el dni
    Console.WriteLine(alumno.Value.Nombre); // imprime el nombre del alumno
}
```

Mismo approach usando var (infiere el tipo KeyValuePair en la ejecución):
```csharp
foreach (var alumno in dicAlumnos)
{
    Console.WriteLine(alumno.Key); // imprime el dni
    Console.WriteLine(alumno.Value.Nombre); // imprime el nombre del alumno
}
```

# 1. Fundamentos de .NET y Arquitectura MVC

.NET es un framework de desarrollo de código abierto, gratuito y administrado, compatible con Windows, Linux y macOS. Un framework actúa como un esquema o marco de trabajo que provee un esqueleto inicial para evitar que el desarrollador deba plantear una estructura global desde cero, facilitando el trabajo colaborativo y la integración de librerías.

MVC (Modelo-Vista-Controlador) es un patrón de arquitectura sumamente extendido en aplicaciones web que separa de forma elegante la lógica de negocio y de acceso a datos de la interfaz de usuario. Este patrón se divide en tres componentes clave:
- **Modelo:** Representa los objetos de negocio, manipula los datos y gestiona los accesos y privilegios sobre dicha información.
- **Controlador:** Actúa como conductor u orquestador de la aplicación; captura las peticiones URL del usuario, invoca las operaciones del modelo y selecciona la vista apropiada como respuesta.
- **Vista:** Es la encargada de presentar la información procesada por el modelo en un formato visual adecuado para la interacción del usuario (usualmente marcado HTML/.cshtml).

Este patrón de arquitectura de backend no es exclusivo de .NET; se utiliza en muchas tecnologías y lenguajes como Python (Django), JavaScript (Express) o PHP (Laravel o Symfony).

# 2. Preparación del Entorno y Estructura del Proyecto

**Instalación:** Se requiere descargar e instalar el .NET SDK (dotnet) desde el sitio oficial de descargas de Microsoft.

**Creación del Proyecto:** En la terminal integrada de Visual Studio Code, se ejecutan los comandos para inicializar y abrir la aplicación:
```bash
dotnet new mvc -n PrimerProyecto
code -r PrimerProyecto
```

**Estructura del Proyecto:** Un proyecto MVC estándar cuenta con las siguientes carpetas y archivos esenciales:
- `/Controllers`: Aloja las clases controladoras de peticiones de URL.
- `/Models`: Contiene las clases de datos y objetos de negocio.
- `/Views`: Contiene los archivos de la interfaz de usuario con extensión .cshtml.
- `/wwwroot`: Almacena recursos estáticos como hojas de estilo (.css), scripts (.js), imágenes y librerías.
- `Program.cs` y archivos de configuración `appsettings.json`.

**Ejecución:** Antes de iniciar la aplicación por primera vez, se debe habilitar el certificado HTTPS de desarrollo escribiendo `dotnet dev-certs https --trust` en la terminal. Posteriormente, se presiona F5 (o Start Debugging) para ejecutar el proyecto e ingresar al navegador mediante el puerto local asignado (por ejemplo, `https://localhost:5001/`).

# 3. Implementación de Modelos, Controladores y Acciones

**Creación del Controlador:** Los controladores heredan de la clase base Controller. En un escenario típico de administración académica, el HomeController.cs puede definir métodos denominados Action Methods encargados de procesar la petición y devolver una respuesta mediante `return View();`:
```csharp
using Microsoft.AspNetCore.Mvc;

namespace TP4.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
    public IActionResult ListaEstudiantes() => View();
    public IActionResult InfoEstudiante() => View();
}
```
Navegar a la ruta `[url]/Home/ListaEstudiantes` invocará de forma automática y transparente la acción correspondiente del controlador.

**Creación de un Modelo:** Se realiza dentro de la carpeta `/Models` creando un archivo de clase (por ejemplo, `Auto.cs`):
```csharp
namespace PrimerProyecto.Models
{
    public class Auto
    {
        // Definición de propiedades y constructor...
    }
}
```

**Instanciación e Intercambio de Datos:** En el Action Method del controlador se instancia el modelo y se puede transferir hacia la vista de manera dinámica a través de ViewBag o ViewData:
```csharp
private static Auto miAuto;

public IActionResult Index()
{
    miAuto = new Auto("AAA123", "Chevrolet");
    ViewBag.MiAuto = miAuto; // Se asigna al ViewBag para enviarlo a la vista
    return View();
}
```

# 4. Vistas, Razor y Enlaces Dinámicos

**Razor:** Es un motor de vistas que utiliza la sintaxis `@{ .. }` para permitir la ejecución de código C# directamente incrustado dentro de los archivos HTML (.cshtml).

**Envío de Datos Dinámicos:**
- **ViewData:** Estructura de diccionario de clave-valor útil para pasar datos sencillos (ej: `ViewData["Title"] = "Index";`).
- **ViewBag:** Objeto dinámico de tipo wrapper donde se pueden definir variables sobre la marcha sin clases formales.

En la vista se consumen las variables de ViewBag:
```html
<h1>El Nombre del Alumno es @ViewBag.NombreAlumno y tiene @ViewBag.Edad años</h1>
```

**Generación de Enlaces (URL.Action):** Se utiliza el helper `@Url.Action` en el atributo `href` de las etiquetas de enlace (`<a>`) para determinar dinámicamente la ruta a la que apunta la acción de un controlador. Admite el paso de objetos anónimos como parámetros de consulta:
```html
<!-- Sintaxis básica -->
<a href='@Url.Action("InfoEstudiante", "Estudiante")'>Federico Perchuk</a>

<!-- Envío de parámetros al Action Method -->
<a href='@Url.Action("ListadoEstudiantes", "Estudiante", new { Curso="4A" })'>Ir al Curso 4A</a>
```

**Diferencia clave en navegación:**
- `Return View`: Renderiza y devuelve la vista especificada directamente en el servidor sin generar una nueva solicitud HTTP en el navegador.
- `RedirectToAction`: Redirige al navegador a una acción diferente, provocando una nueva solicitud HTTP completa.

# 5. Páginas Maestras (MasterPages) y Enlaces Oficiales

**MasterPages (`_Layout.cshtml`):** Al crear un proyecto, bajo la carpeta `/Views/Shared` se define el archivo `_Layout.cshtml`. Este archivo almacena las estructuras HTML repetitivas (menús, pies de página, metas de cabecera) para evitar duplicar código en cada vista individual.

**Función `@RenderBody()`:** Es la instrucción Razor más importante dentro de una MasterPage. Al interpretarse el sitio web, .NET Core renderiza la estructura general del Layout y "pega" de forma dinámica el contenido de la vista solicitada justamente en la posición que ocupa `@RenderBody()`.

**Enlaces y documentación oficial:** La presentación incluye hipervínculos clave para la consulta de información de referencia de Microsoft:
- Tutorial de Introducción a ASP.NET Core MVC
- Guía completa de ASP.NET Core MVC
- Crear una API en .NET Core

# Formularios HTML y recepción de información

## Diapositiva 1: Introducción a los Formularios

Cualquier página HTML puede mejorarse con formularios interactivos que pidan al visitante que rellene información: introducir texto, seleccionar opciones, confirmar con un botón...

Su propósito es recopilar datos del usuario y enviarlos a un servidor. Es fundamental para la interacción y la recolección de datos en aplicaciones web.

Como vamos a ver, los resultados tienen que procesarse en otro lenguaje, como ASP.NET, por ejemplo. Mientras tanto, tenemos un gran número de etiquetas HTML por descubrir.

Bienvenidos y bienvenidas, al maravilloso mundo de los formularios.

## Diapositiva 2: El Tag FORM

Cuando tienes la necesidad de insertar un formulario en tu página HTML, necesitas empezar escribiendo una etiqueta `<form>`. Este tag debe tener su tag de cierre `</form>`.

Dentro de este tag escribiremos muchos tags de ingreso de datos que conoceremos en detalle en esta presentación.

Se escriben en las Views ya que es el ingreso de datos que se le presenta al usuario.

## Diapositiva 3: Atributos Esenciales del TAG Form

Existen muchos atributos más, pero vamos a hacer hincapié en estos tres atributos esenciales:
- `action`: Define la URL para enviar los datos que el usuario ingresa en un form. La seguridad (HTTPS) es clave.
- `method`: Este atributo especifica cómo se van a enviar los datos que el usuario escriba en el formulario.
- `enctype`: Define el tipo de codificación que se usará al enviar los datos del formulario al servidor mediante el método POST.

## Diapositiva 4: Valores Posibles en Atributos Esenciales

**1. Atributo action:** Especifica a qué dirección web se enviarán y procesarán los datos. En el caso de un proyecto MVC la dirección SIEMPRE debe ser una acción del controller.

**2. Atributo method:**
- `GET`: La información se envía en la URL de la página y es visible. Solo permite 255 caracteres.
- `POST`: Es el método más usado en formularios, ya que permite mayor cantidad de información y es más seguro porque los datos no se muestran en la barra de direcciones.

**3. Atributo enctype:**
- `application/x-www-form-urlencoded`: Los datos del formulario se codifican como pares clave=valor, separados por `&` (es el enctype por default de envío).
- `multipart/form-data`: Se utiliza cuando el formulario incluye archivos. Esta codificación permite enviar tanto archivos como otros datos.

Ejemplo de sintaxis del Form:
```html
<form action='@Url.Action("GuardarDatos", "Home")' method="POST" enctype="multipart/form-data">
```

## Diapositiva 5: TAGS para Campos de Entrada Básicos (Campos de Texto)

HTML provee varios tags para introducir texto en un formulario.

Texto de una línea:
```html
<input type="text" name="nombre" />
```

Tipo clave (Password):
```html
<input type="password" name="contraseña" />
```

En múltiples líneas:
```html
<textarea name="descripcionLarga" rows=10 cols=5 />
```

Textos validados:
```html
<input type="email" name="mail" />
<input type="tel" name="telefono" />
<input type="color" name="colorPelo" />
<input type="url" name="paginaWeb" />
<input type="date" name="fecha" />
<input type="number" name="edad" />
```

Nota: Todos estos tags tienen más atributos para investigar, como placeholder, maxlength y los que ya conocieron en Arte, como Class, Id, etc. ¡Para investigar!

## Diapositiva 6: TAGS con Opciones de Selección (Concepto)

HTML incluye un montón de opciones que pueden usarse en el formulario. Son elementos que requieren que el visitante elija a partir de una lista de posibilidades:
- Casillas de selección única (Rojo, Azul, Verde).
- Casillas de selección múltiple (Deportes, Música, Cine).
- Listas desplegables (Argentina, España, México, Chile).

## Diapositiva 7: Código de Tags con Opciones de Selección

**1. Casillas de selección única (Radio Buttons)**
```html
<input type="radio" name="color" value="rojo"> Rojo
<input type="radio" name="color" value="azul"> Azul
<input type="radio" name="color" value="verde"> Verde
```
Comportamiento: En el input radio la información que se guardará es el value seleccionado.

**2. Casillas de selección múltiple (Checkboxes)**
```html
<input type="checkbox" name="deportes"> Deportes
<input type="checkbox" name="musica"> Música
<input type="checkbox" name="cine"> Cine
```
Comportamiento: En el input checkbox la información que se guardará será un true si está seleccionado o false en caso contrario.

**3. Listas desplegables (Select)**
```html
<select name="pais">
    <option value="1">Argentina</option>
    <option value="2">España</option>
    <option value="3">México</option>
    <option value="4">Chile</option>
</select>
```
Comportamiento: En el tag select la información que se guardará también es el value del option seleccionado.

Nota: Dentro de todos estos tags tenemos atributos muy interesantes como Checked, Required y Selected para investigar cómo funcionan.

## Diapositiva 8: TAG Hidden y TAG File

**TAG Hidden (Oculto)**

El elemento input, cuyo atributo Type se define con el valor `hidden`, representa cualquier cadena de texto arbitraria que no está pensada para ser vista o editada por el usuario. Los controles ocultos son especialmente útiles para enviar datos al servidor definidos por el autor, basados o no en la interacción con el usuario.
```html
<input type="hidden" name="IdPelicula" value="1" />
```

**TAG File (Carga de Archivos)**

El elemento input, cuyo atributo Type se define con el valor `file`, nos sirve para pedirle al usuario que suba un archivo. Este puede ser una imagen, documento o cualquier archivo de nuestra computadora que queramos enviar al servidor.
```html
<input type="file" name="foto" />
```

## Diapositiva 9: Botones de Envío

Toda la información cargada por un usuario mediante un navegador se debe enviar para procesar. Para esto se utilizan los botones de envío:

**submit:** Botón de envío principal del formulario. Redirecciona a la página especificada en el atributo action del tag form.
```html
<input type="submit" value="Guardar" />
```

**reset:** Restablece el formulario, eliminando todo lo cargado por el usuario.
```html
<input type="Reset" value="Borrar Formulario" />
```

**image:** Equivalente al botón submit, pero visualmente será una imagen. Añade el atributo src para especificar la URL de la imagen.
```html
<input type="image" src="/img/guardar.jpg" />
```

**button:** Botón genérico que no tendrá efecto alguno por sí solo. En general, este botón se maneja con JavaScript para desarrollar acciones dinámicas en la página.
```html
<input type="button" onclick="funcion()" />
```

## Diapositiva 10: Recepción de Información de un FORM

Habiendo aprendido cómo crear un formulario web para interactuar con un usuario, ahora lo que debemos aprender es qué hacer con los datos que son enviados desde un tag form. En esta sección aprenderemos a recibir dichos datos para luego tomar una acción (guardarlos en un objeto, enviarlos a otra página, etc.).

## Diapositiva 11: Introducción a la Recepción de Datos

Para recibir los datos enviados desde un formulario como el de la diapositiva 4:
```html
<form action='@Url.Action("GuardarDatos", "Home")' method="POST" enctype="multipart/form-data">
```

Debemos crear una acción en el controller "Home" llamada "GuardarDatos". Ahí recibiremos toda la información del form en forma de Parámetros.

⚠️ ¡Atención! Los nombres de los parámetros en el controlador deben llamarse igual a los nombres de los controles creados en la View (es decir, corresponder con el atributo name).

## Diapositiva 12: Ejemplo de Recepción de Información

En la View:
```html
<form action="@Url.Action("Login", "Cuenta")" method="post">
    Usuario: <input type="text" name="usuario">
    Contraseña: <input type="password" name="contrasena">
    <input type="submit" value="Entrar">
</form>
```

Nota: Aquí se destaca que el atributo name es fundamental para enlazar con el backend.

En el controller Cuenta:
```csharp
public class CuentaController : Controller
{
    [HttpPost]
    public ActionResult Login(string usuario, string contrasena)
    {
        // Aquí podrías validar usuario y contraseña si quisieras.
        ViewBag.Usuario = usuario;
        return View("Bienvenido");
    }
}
```

## Diapositiva 13: Recepción de Archivos de un tag Form (View)

Si estamos enviando un archivo desde un formulario, debemos recordar siempre poner el enctype en el tag form.

En la View:
```html
<form action='@Url.Action("SubirArchivo","Home")' method="post" enctype="multipart/form-data">
    <input type="file" name="archivo" />
    <br /><br />
    <button type="submit">Subir</button>
</form>
```

Cuando el usuario presione el botón de Submit, se ejecutará la acción "SubirArchivo" que está en el action del formulario. La información del formulario se enviará por POST. El enctype se usará multipart/form-data porque este formato divide el contenido en partes y permite enviar tanto texto como binarios (archivos) en la misma petición sin corromperlos.

## Diapositiva 14: Recepción de Archivos en el Controller

IWebHostEnvironment es un servicio proporcionado por el framework que nos dice: la ruta física del proyecto, la carpeta raíz del contenido estático (wwwroot) y el nombre del entorno (Desarrollo, Producción, etc.).

En el controller Home:
```csharp
private readonly IWebHostEnvironment _env;

public HomeController(IWebHostEnvironment env)
{
    _env = env;
}

public IActionResult SubirArchivo(IFormFile archivo)
{
    if (archivo != null && archivo.Length > 0)
    {
        // Nombre del archivo
        string nombreArchivo = archivo.FileName;

        // Ruta física donde guardar
        string rutaCarpeta = Path.Combine(_env.WebRootPath, "imagenes");

        if (!Directory.Exists(rutaCarpeta))
        {
            Directory.CreateDirectory(rutaCarpeta);
        }

        string rutaCompleta = Path.Combine(rutaCarpeta, nombreArchivo);

        // Guardar el archivo en wwwroot/imagenes
        using (var stream = new FileStream(rutaCompleta, FileMode.Create))
        {
            archivo.CopyTo(stream);
        }
        return View("Resultado");
    }

    // Si no se seleccionó ningún archivo no ejecuta el return view de Resultado
    ViewBag.Mensaje = "No se seleccionó ningún archivo.";
    return View("Index");
}
```

## Diapositiva 15: Paso de Parámetros Avanzados

Se usa con RedirectToAction para pasar datos simples (int, string, etc.) entre acciones (ActionResult). Los datos se agregan a la URL como parte de la ruta o query string.
```csharp
[HttpPost]
public IActionResult RegistrarAlumno(int id, string nombre)
{
    // Redirige pasando los datos por la URL
    return RedirectToAction("MostrarAlumno", new { id = id, nombre = nombre });
}

public IActionResult MostrarAlumno(int id, string nombre)
{
    // Pasa los datos a la vista usando ViewBag
    ViewBag.Id = id;
    ViewBag.Nombre = nombre;

    return View();
}
```

# Getters, setters y propiedades automáticas

## Diapositiva 1: Recordando lo aprendido el año pasado

En 3ero creabamos métodos para manejar los atributos de las clases, por ejemplo:
```csharp
class Persona
{
    private string nombre; // Atributo privado

    // Método para devolver el nombre
    public string DevolverNombre()
    {
        return nombre;
    }

    // Método para cambiar el nombre
    public void CambiarNombre(string nuevoNombre)
    {
        nombre = nuevoNombre;
    }
}
```

## Diapositiva 2: Controlando el acceso a atributos

Este año, algunos atributos vamos a necesitar accederlos desde otras clases de forma pública. ¿Pero cómo controlar lo que se puede hacer con esos atributos públicos? ¿Deberían otras clases solo leer el atributo? ¿Deberían poder modificarlo también? ¿Cómo podemos manejarlo?

La solución está en getters y setters, que nos permiten definir si queremos que un atributo público sea solo lectura, solo escritura, o lectura y escritura.

## Diapositiva 3: ¿Qué son Getters y Setters?

Los getters y setters son métodos especiales que nos permiten leer y modificar los valores de los atributos de una clase, pero de una forma controlada.
- **Getter:** Nos permite leer el valor de un atributo.
- **Setter:** Nos permite modificar el valor de un atributo.

En C#, podemos usar propiedades automáticas para crear getters y setters sin necesidad de escribir métodos adicionales como veníamos haciendo. Esto simplifica el código.

## Diapositiva 4: Sintaxis de Propiedades Automáticas

Podemos usar propiedades automáticas que hacen todo lo mencionado previamente de forma más sencilla:
```csharp
class Persona
{
    public string Nombre { get; set; } // Propiedad automática de lectura y escritura
}
```

Uso:
```csharp
class Program
{
    static void Main()
    {
        Persona p = new Persona();
        p.Nombre = "Carlos"; // Modificamos el valor
        Console.WriteLine(p.Nombre); // Leemos el valor
    }
}
```

Este es el caso más sencillo donde cualquiera puede leer y modificar el valor.

## Diapositiva 5: Controlando la accesibilidad

A veces no queremos que todo sea completamente accesible para ser modificado desde fuera de la clase. Podemos querer que la lectura sea pública, pero restringir la escritura, o al revés.
```csharp
class Persona
{
    public string Nombre { get; private set; } // Solo lectura pública, escritura privada
}
```

Uso:
```csharp
class Program
{
    static void Main()
    {
        Persona p = new Persona();
        p.Nombre = "Carlos"; // ERROR: No se puede modificar desde afuera, porque el set es privado
        Console.WriteLine(p.Nombre); // Se puede leer el valor
    }
}
```

En este caso el get sigue siendo público por lo que cualquier clase puede leer el valor. Con el set privado solo la clase Persona puede modificar el valor de Nombre.

## Diapositiva 6: Enviando Parámetros del Form a un Objeto

Si tenemos muchos parámetros que enviar a un controller y encima después ponerlos en un objeto ahora podemos directamente llenarlo en la recepción de parámetros de la acción del controller.

```html
<div class="text-center">
    <h1 class="display-4">Bienvenido @ViewBag.Nombre @ViewBag.Contraseña</h1>
    <form action='@Url.Action("Login","Home")' method="POST" >
        <input type="text" name="NombreUsuario" placeholder="Usuario">
        <input type="password" name="Contraseña" placeholder="Password">
        <input type="submit" value="Ingresar">
    </form>
</div>
```

Uso:
```csharp
[HttpPost]
// 0 references
public IActionResult Login(Usuario user)
{
    ViewBag.Nombre = user.NombreUsuario;
    ViewBag.Contraseña = user.Contraseña;
    return View("Index");
}
```

¡Recordar que los NAME de cada input deben llamarse IGUAL a los atributos de la clase! En este caso además, todos los atributos deben tener disponible la propiedad set.

## Diapositiva 7: Conclusión

Usando getters y setters automáticos podemos hacer que nuestros atributos sean accesibles de manera sencilla, sin escribir métodos adicionales. A veces necesitamos controlar el acceso a los atributos, y para eso podemos usar private para evitar modificaciones o lecturas no deseadas desde otra clase.

# JavaScript: introducción, variables, estructuras y DOM

## Diapositiva 1: Índice de Contenidos

01 Introducción; 02 Sintaxis; 03 Estructuras; 04 Arrays; 05 DOM; 06 Enlaces complementarios. Detalle visual: la portada muestra un hexágono azul con el logo amarillo cuadrado de JS en el centro.

## Diapositiva 2: Introducción

“Si el término frontend refiere a todo aquello que sucede del lado del cliente (quien consume los datos y no los genera/procesa) el máximo aliado y un jugador fundamental es JavaScript. Sin JavaScript, nuestros sitios son bastante estáticos, carecen de flujos diferentes dependiendo de nuestras acciones en la interfaz, entre otras cosas. Hemos llegado a una instancia en que es tan importante el negocio como las buenas prácticas de UX/UI. Pero... ¿qué es JavaScript?”

## Diapositiva 3: ¿Qué es JavaScript? - Definición

“Es un lenguaje de programación que implementa funciones complejas en páginas web. Interviene cada vez que una página web hace algo más que sentarse allí y mostrar información estática para que la veas; muestra oportunas actualizaciones de contenido, mapas interactivos, animación de Gráficos 2D/3D, desplazamiento de máquinas reproductoras de vídeo, etc. Técnicamente, JavaScript es un lenguaje de programación interpretado, por lo que no es necesario compilar los programas para ejecutarlos. En otras palabras, los programas escritos con JavaScript se pueden probar directamente en cualquier navegador sin necesidad de procesos intermedios.”

## Diapositiva 4: ¿Qué es JavaScript? - Las Capas Web

“Es la tercera capa del pastel de las tecnologías web estándar, dos de las cuales (HTML y CSS) hemos cubierto en la materia de Arte.” Detalle visual: se muestra la ilustración de un pastel de tres capas con una vela: la capa superior es HTML [Estructura], la del medio es CSS [Estilo], y la inferior es JavaScript [Comportamiento].

## Diapositiva 5: ¿Qué es JavaScript? - Motores y Relación con Java

“Cada navegador (Google Chrome, Safari, Edge...) tiene su propio motor de Javascript, cuyo objetivo es interpretar el lenguaje y mostrar en el explorador el resultado del mismo. A pesar de su nombre, JavaScript no guarda ninguna relación directa con el lenguaje de programación Java.”

## Diapositiva 6: Incluyendo JavaScript en mi HTML

Muestra cómo enlazar un archivo externo de JavaScript al final del `<body>` de un documento HTML para optimizar la carga del sitio web:
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>JavaScript</title>
</head>
<body>

    <script src="js/script.js"></script>
</body>
</html>
```

## Diapositiva 7: Variables - Let y Const

JavaScript es un lenguaje de tipado dinámico. Es decir, las variables toman su tipo de dato en tiempo de ejecución. Vamos a utilizar `let` y `const` para declarar variables. La diferencia entre ambas instrucciones radica en que `const` no permite modificar el valor de la variable, y debe ser inicializada inmediatamente en la misma línea en la cual es declarada. `let` permite la modificación futura del valor de la variable. Ambas formas de declarar una variable tienen scope de bloque, es decir, viven entre las llaves `{ }` donde fueron declaradas.

## Diapositiva 8: Variables - Ejemplo de Ámbito y Mutabilidad

```javascript
function saludar(){
    const nombre = "Ezequiel";
    console.log("Hola " + nombre + "!");

    nombre = "Juan"; // -> da error porque no se puede modificar una const
    console.log("Hola " + nombre + "!");

    let apellido = "Perez";
    console.log("Hola " + apellido + "!");

    apellido = "Gonzalez"; // -> ok porque se puede modificar un let
    console.log("Hola " + apellido + "!");
}
```

## Diapositiva 9: Variables - Tipos de Datos y Dinamismo

JavaScript es débilmente tipado y dinámico. Las variables no están asociadas directamente con ningún tipo de valor en particular, y a cualquier variable se le puede asignar (y reasignar) valores de todos los tipos.
```javascript
let foo = 42; // foo ahora es un número
foo = "bar";  // foo ahora es un string
foo = true;   // foo ahora es un booleano
```

## Diapositiva 10: Variables - Tipos de Datos Primitivos

Los tipos de datos primitivos en JavaScript son:
- `undefined`
- `null`
- `boolean`
- `number`
- `string`
- `symbol`

Existen también otros 3 tipos de datos especiales: Null, Object y Function.

## Diapositiva 11: Asignación de variables

La asignación de variables permite otorgarle valores a variables ya declaradas, para luego utilizarlas donde corresponda.
```javascript
// let permite no ser asignado al ser declarado
let nombre;

nombre = "Lionel";

// const debe ser asignado al momento de su declaración
const apellido = "Scaloni";
```

## Diapositiva 12: Condicionales

Los condicionales en JavaScript funcionan y se definen exactamente igual que en otros lenguajes de programación, como por ejemplo C#.

**Uso de if / else if / else:**
```javascript
if (num1 > num2) {
    console.log('Num 1 es mayor a num 2');
} else if (num1 === num2) {
    console.log('Son iguales');
} else {
    console.log('Num 2 es mayor a num 1');
}
```

**Uso de switch:**
```javascript
switch (categoria) {
    case 'A':
        console.log(precio * TIPO_A);
        break;
    case 'B':
        console.log(precio * TIPO_B);
        break;
    default:
        console.log('categoría inexistente');
        break;
}
```

## Diapositiva 13: Ciclos - Introducción

La definición de los ciclos es muy similar a otros lenguajes de programación de alto nivel. Un cambio que vemos es el uso de `let` en el `for`.
```javascript
// Ciclo for
for(let i = 0; i < 100; i++) {
    console.log(i);
}

// Ciclo while
while(!encontrado) {
    //código
}

// Ciclo do while
do {
    //código
} while(!encontrado);
```

“A medida que vayamos viendo objetos, funciones y arrays, exploraremos otras formas de uso del ciclo for y foreach.”

## Diapositiva 14: Funciones - Definición

Las funciones son bloques de código que pueden recibir o no parámetros, y retornan o no un resultado concreto. Los parámetros en JavaScript no son tipados.
```javascript
function sumar(num1, num2) {
    return num1 + num2;
}
```

## Diapositiva 15: ¿Qué es el DOM?

“El modelo de objeto de documento (DOM) es una interfaz de programación para los documentos HTML. El DOM da una representación del documento como un grupo de nodos y objetos estructurados que tienen propiedades y métodos. Esencialmente, conecta las páginas web a scripts o lenguajes de programación.”

## Diapositiva 16: Interfaz Document

La interfaz Document describe las propiedades y métodos comunes para cualquier tipo de documento. Document define algunos métodos interesantes que sirven para acceder desde JavaScript a los elementos dispuestos en HTML.

Métodos listados en la diapositiva:
```javascript
document.getElementById(id)
document.getElementsByTagName(name)
document.getElementsByClassName(name)
document.createElement(element)
document.removeChild(element)
document.appendChild(element)
```

## Diapositiva 17: document.getElementById

Devuelve una referencia al elemento por su ID.

Estructura HTML:
```html
<p id="parrafo"></p>
```

Código JavaScript:
```javascript
const parrafo = document.getElementById('parrafo')
```

## Diapositiva 18: element.innerHTML

Cualquier elemento que forme parte del document de HTML tiene accesible la propiedad `innerHTML`. Esta representa el contenido que tiene ese elemento dentro de la página web.
```javascript
const parrafo = document.getElementById('parrafo')

parrafo.innerHTML = "Taller de Programación"

// Luego de esta línea, el párrafo tendrá "Taller de Programación" como contenido
```

## Diapositiva 19: Propiedades de elementos (input Text)

Cuando obtenemos un elemento mediante JS con el DOM, podemos acceder a todas sus propiedades. Nombraremos algunas, pero investigar en la web cuando se necesiten acceder a otras.

HTML:
```html
<input type="text" name="NombreEmpleado" value="Juan" id="Nombre">
```

JavaScript:
```javascript
let nombre = document.getElementById("Nombre");
console.log(nombre.value); // Muestra "Juan" en consola
nombre.value = "Leo";      // Cambia el nombre en el value a "Leo"
nombre.style.color = "red"; // Cambia el color del texto del input a rojo
```

## Diapositiva 20: Propiedades en elementos (button)

Un ejemplo sobre cómo podemos aprovechar el `onclick` de un botón y además crearle una propiedad para utilizarla en JS.

HTML:
```html
<button id="Actualizar" onclick="cambiarColor()" estado="Azul">Cambiar Color</button>
<h1 id="MostrarTexto">Leo</h1>
```

JavaScript:
```javascript
function cambiarColor()
{
    let Boton = document.getElementById("Actualizar");
    const objH1 = document.getElementById("MostrarTexto");

    objH1.style.color = (Boton.estado == "Azul" ? 'green' : 'blue');
    Boton.estado = (Boton.estado == "Azul" ? 'Verde' : 'Azul');
}
```

# Dapper y SQL Server

## Diapositiva 1: CONTENIDO

Título principal: CONTENIDO. Estructura del índice: 01 Introducción, 02 Instalación de Librerías, 03 Acerca de Dapper ORM, 04 Dapper - Query, 05 Dapper - Execute, 06 Enlaces complementarios. Ilustración visual: gráfico conceptual con servidores de bases de datos grises y azules conectados mediante líneas punteadas con una notebook que visualiza gráficos de barras y de tarta junto a carpetas de archivos, acompañado de un engranaje en la parte inferior.

## Diapositiva 2: Introducción

Hasta ahora, los programas o sistemas estaban basados 100% dentro de la memoria principal. La memoria principal de un dispositivo informático es volátil: una vez que termina el programa o finaliza la ejecución del servidor, los datos se pierden.

En esta unidad se verá cómo persistir y recuperar la información que circula en los proyectos, desde una base de datos creada en el motor Microsoft SQL Server.

## Diapositiva 3: Instalación de librerías

En la terminal de VSCode se corren secuencialmente los siguientes comandos:
```bash
dotnet add package Microsoft.Data.SqlClient
dotnet add package Dapper
```
Estas sentencias instalan los paquetes SqlClient y Dapper, necesarios para conectarse a una base de datos SQL Server.

## Diapositiva 4: Comprobamos ambas instalaciones

Si ingresamos al archivo `.csproj` del proyecto, ubicado en la carpeta raíz, deberíamos ver las dos librerías instaladas correctamente.

Código del archivo `.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net7.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>TP_Prog</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Dapper" Version="2.1.66" />
    <PackageReference Include="Microsoft.Data.SqlClient" Version="6.0.1" />
  </ItemGroup>
</Project>
```

## Diapositiva 5: La librería SqlClient

En C# sobre .NET Core todo está orientado a objetos. Se pueden incorporar librerías de clases ya desarrolladas que permiten solucionar diversos requerimientos. En este caso, el objetivo es conectarse a un servidor SQL Server donde está alojada la base de datos.

Se agrega una nueva directiva `using`:
```csharp
using Microsoft.Data.SqlClient;
```

La clase `SqlConnection` permite crear objetos que contienen conexiones a los servidores que alojan las bases.

## Diapositiva 6: Incorporando SqlClient

En el sistema habrá un único acceso a la base de datos, por eso se utilizará una clase con métodos estáticos dentro de model para manejar toda la lógica de persistencia. Se crea la clase `BD.cs` e incorpora la directiva para importar SQLClient.

Esquema del explorador de archivos en VS Code: `TRY-CATCH-POC`, `bin`, `Controllers`, `HomeController.cs`, `Models`, `BD.cs`, `Cliente.cs`.

Contenido inicial del archivo `BD.cs`:
```csharp
using Microsoft.Data.SqlClient;
using Dapper;

namespace try_catch_poc.Models;

public class BD
{
```

## Diapositiva 7: ORM

ORM (Mapeo Objeto-Relacional en español) es una técnica de programación que permite mapear las estructuras de una base de datos relacional al modelo orientado a objetos de una aplicación. De esta forma se ahorra tiempo programando la asignación de valores a los atributos de los objetos.

Diagrama visual explicativo: equivalencia entre un modelo orientado a objetos en código y su contraparte en tablas de bases de datos relacionales, unidos por flechas bidireccionales cruzadas por una barrera de división amarilla.

Objeto Producto (A Product Object):
- id: 12
- name: Bike
- price: $800.00
- description: fixed gear, blue, fast

Tabla de Productos (Table: product): columnas `id`, `name`, `price`, `description`; fila 12: `12 | Bike | $800.00 | fixed gear, blue, fast`; fila 13: `13 | Helmet | $20.99 | black, fits most`; fila 14: `14 | Jersey | $35.00 | women's small, green and white`.

## Diapositiva 8: La librería Dapper

Dapper es un MicroORM para .NET. Similar a Entity Framework (otro ORM disponible en .NET), es útil para consultas a bases de datos. Para invocar sus métodos se agrega la directiva `using` al comienzo de la clase que lo utiliza:
```csharp
using Dapper;
```

Los métodos mencionados en la presentación son `Query`, `QueryFirstOrDefault` y `Execute`.

## Diapositiva 9: ConnectionString

La conexión con la base es costosa para un sistema o sitio web. Hay que tener en cuenta que solo debe haber 1 conexión activa a la base de datos a la vez. Para conectarse a la base de datos es necesario definir un ConnectionString. Para SQL Server se constituye así:
```csharp
private string _connectionString = @"Server=localhost;DataBase=NombreBase;Integrated Security=True;TrustServerCertificate=True;";
```

`Server` es el nombre de la terminal donde nos encontramos, `DataBase` es el nombre de la base de datos y los últimos dos parámetros son necesarios para conectarnos mediante Windows Authentication.

## Diapositiva 10: Dapper - Método Query

Se crea un objeto de la clase Conexion (`db`) y luego se hace una consulta (`db.Query`) cuyo resultado se convierte a una lista de tipo Patente invocando a `.ToList()`. Al encerrar la operación select en un bloque `using`, se cierra la conexión a la base. Es muy importante que las propiedades de la clase se llamen igual que los campos que vengan de la query y que tengan permisos de escritura (`set`).

Código C#:
```csharp
public List<Patente> LevantarPatentes()
{
    List<Patente> patentes = new List<Patente>();
    using(SqlConnection connection = new SqlConnection(_connectionString))
    {
        string query = "SELECT * FROM Patentes";
        patentes = connection.Query<Patente>(query).ToList();
    }
    return patentes;
}

public class Patente
{
    public string Matricula { get; set; }
    public DateTime FechaCompra { get; set; }
}
```

## Diapositiva 11: Dapper - Método QueryFirstOrDefault

A veces, se realizan consultas que se sabe que devolverán solo un registro. Se crea un objeto de la clase Conexion (`db`) y se hace una consulta (`db.QueryFirstOrDefault`), cuyo resultado puede ser un objeto Patente si encuentra la coincidencia definida en la cláusula `WHERE`, o `null` si no hay coincidencia.

Código C#:
```csharp
public Patente LevantarPatente(string patente)
{
    Patente miPatente = null;
    using(SqlConnection connection = new SqlConnection(_connectionString))
    {
        string query = "SELECT Matricula FROM Patentes WHERE Matricula = @pPatente";
        miPatente = connection.QueryFirstOrDefault<Patente>(query, new { pPatente = patente });
    }
    return miPatente;
}
```

## Diapositiva 12: Dapper - Método Execute (INSERT)

El método `Execute` de Dapper realiza la acción determinada y retorna la cantidad de registros afectados en la tabla (un entero). Ejemplo para agregar un jugador, recibiendo por parámetro el objeto completo y agregando parámetros individualmente.
```csharp
public void AgregarJugador(Jugador jug)
{
    string query = "INSERT INTO Jugadores (IdEquipo, Nombre, FechaNacimiento) VALUES (@pIdEquipo, @pNombre, @pFechaNacimiento)";
    using(SqlConnection connection = new SqlConnection(_connectionString))
    {
        connection.Execute(query, new { pIdEquipo = jug.IdEquipo, pNombre = jug.Nombre, pFechaNacimiento = jug.FechaNacimiento });
    }
}
```

## Diapositiva 13: Dapper - Método Execute (DELETE)

`Execute` realiza la acción determinada y retorna la cantidad de registros afectados. Ejemplo para eliminar una patente según su código:
```csharp
public int EliminarPatente(string patenteAEliminar)
{
    string query = "DELETE FROM Patentes WHERE Matricula = @patenteAEliminar";
    int registrosAfectados = 0;
    using (SqlConnection connection = new SqlConnection(_connectionString))
    {
        registrosAfectados = connection.Execute(query, new { patenteAEliminar });
    }
    return registrosAfectados;
}
```

## Diapositiva 14: Ejecutando Stored Procedures

Muchas veces las consultas se crean en la base de datos y se almacenan en stored procedures. El tercer parámetro del método Query indica que en vez de recibir un SQL se enviará un stored procedure.
```csharp
public List<Patente> LevantarPatentes(string letraInicial)
{
    List<Patente> listaPatentes = null;
    using(SqlConnection connection = new SqlConnection(_connectionString))
    {
        string storedProcedure = "TraerPatentesxLetra";
        listaPatentes =
            connection.Query<Patente>(
                storedProcedure,
                new { Letra = letraInicial },
                commandType: CommandType.StoredProcedure
            ).ToList();
    }
    return listaPatentes;
}
```

Recordá incluir el `using` de la librería `System.Data` para que el código reconozca `CommandType`.

## Diapositiva 15: Enlaces complementarios

Enlaces listados:
- How Dapper Works?
- How to use the Dapper ORM in C#
- Introducción a Dapper | Conéctate a CUALQUIER Base de Datos en .NET | 4 Ejemplos

La presentación indica que podemos conectarnos a la base de datos utilizando el archivo de configuración del proyecto de Net Core de esta manera. Enlace final: Store and read connection string in appsettings.json – ConnectionStrings.com.

# Sessions

## Diapositiva 1: Fundamentos de Sessions

**Definición:** Las Sessions proporcionan un mecanismo para almacenar información específica del usuario durante una sesión de navegación web. Se basan en cookies de sesión que identifican al usuario en el servidor, permitiendo el acceso a datos almacenados.

## Diapositiva 2: Fundamentos de Sessions — Características

- **Persistencia temporal:** Los datos de la sesión se mantienen mientras el usuario esté activo en la aplicación o hasta que la sesión expire.
- **Almacenamiento en el servidor:** Los datos de sesión se almacenan en el servidor y se identifican mediante una cookie de sesión en el cliente.
- **Acceso por clave-valor:** Se accede a los datos de sesión a través de claves únicas, similares a un diccionario.
- **Configuración de tiempo de vida:** Se puede definir un tiempo de expiración automático o finalizar la sesión manualmente.

## Diapositiva 3: Implementación de Sessions

Descargar los paquetes de NuGet:
```bash
dotnet add package Microsoft.AspNetCore.Session
```
Contiene clases para manejar sesiones.

En `Program.cs` debemos agregar líneas para indicarle al proyecto que utilizará Sessions.

Después de la línea `var builder = WebApplication.CreateBuilder(args);`:
```csharp
builder.Services.AddSession();
```

Y antes de `app.MapControllerRoute(...)` agregar:
```csharp
app.UseSession();
```

## Diapositiva 4: Implementación de Sessions

Para guardar una variable en session y después obtenerla en otra página:
```csharp
HttpContext.Session.SetString(Clave, valor);
HttpContext.Session.GetString(Clave);
```

Los valores en sesión se guardan en formato string, por lo que debemos enviar cada valor convirtiéndolo previamente a string.
```csharp
HttpContext.Session.SetString("Nombre", nombreUsuario);
HttpContext.Session.SetString("DNI", dni.ToString());
string Nombre = HttpContext.Session.GetString("Nombre");
int dni = int.Parse(HttpContext.Session.GetString("DNI"));
```

## Diapositiva 5: Implementación de Sessions — Métodos para borrar datos

```csharp
// Eliminar una variable específica de sesión
Session.Remove("Usuario");

// Eliminar todas las variables de sesión
Session.Clear();
```

El método `Remove(key)` borra un dato específico de la sesión, mientras que `Clear()` borra todas las variables que haya en la sesión.

La presentación agrega: “Si te interesa practicar y consolidar estos conceptos, podemos diseñar un cuestionario interactivo para poner a prueba tus conocimientos sobre el manejo de sesiones en .NET.”

# Material adicional incorporado: Fetch, peticiones asíncronas y respuestas JSON

**Fuente indicada por el usuario:** documento "resumen TI & PROG.pdf" adjuntado por el usuario. Este material se suma al resto de las notas y queda sujeto a la misma regla de lista cerrada de código permitido.

## Diapositiva 1: Introducción a Fetch

En una aplicación web creada con ASP.NET Core MVC, normalmente cada acción del usuario —por ejemplo, enviar un formulario— provoca que el navegador cargue una página completa. Sin embargo, muchas veces necesitamos comunicarnos con el servidor sin recargar toda la página.

Algunos ejemplos de uso son:
- Consultar información y mostrarla automáticamente.
- Actualizar solamente una parte de la pantalla.
- Realizar una acción sobre un botón que guarde en la base de datos y muestre el resultado.
- Completar un desplegable según una opción seleccionada.

Fetch es una función de JavaScript que permite realizar solicitudes HTTPS desde el navegador hacia un servidor. En una aplicación MVC, Fetch permite que el código JavaScript de una vista se comunique con una acción de un controlador sin tener que recargar toda la página.

Ejemplo de contexto de la presentación: hay una página con series y cada serie tiene botones para consultar sus temporadas, sus actores y más información. Al presionar un botón, se quiere mostrar la información solicitada de esa serie sin recargar la página.

## Diapositiva 2: Qué se hace en cada capa de MVC

En la vista se muestra la lista de series y se crean botones que llaman a funciones JavaScript. En el ejemplo, los botones abren el modal `#ModalSerie` y llaman a `GetTemporadas`, `GetActores` o `GetInfo` pasando el identificador de la serie y, cuando se muestra en el ejemplo, su nombre.

Código de la vista mostrado en la presentación:
```html
<div class="container">
    <div class="row">
        @foreach (Serie item in ViewBag.Series)
        {
            <div class="col-12 col-md-6 pb-4">
                <div class="card w-100">
                    <img src="@item.ImagenSerie" class="card-img-top" alt="@item.Nombre">
                    <div class="card-body">
                        <h5 class="card-title">@item.Nombre</h5>
                        <button onclick="GetTemporadas(@item.IdSerie, '@item.Nombre')"
                            class="btn btn-danger" data-toggle="modal" data-target="#ModalSerie">Temporadas</button>
                        <button onclick="GetActores(@item.IdSerie, '@item.Nombre')"
                            class="btn btn-success" data-toggle="modal" data-target="#ModalSerie">Actores</button>
                        <button onclick="GetInfo(@item.IdSerie)"
                            class="btn btn-warning" data-toggle="modal" data-target="#ModalSerie">Más info</button>
                    </div>
                </div>
            </div>
        }
    </div>
</div>
```

La presentación muestra las llamadas a `GetTemporadas`, `GetActores` y `GetInfo` desde la vista, pero en este material solo incluye el código de las funciones `GetActores` y `GetInfo`. Por lo tanto, la llamada a `GetTemporadas` está incluida porque aparece en la vista, pero no inventes su implementación si no se aporta en la consigna o en otro material permitido.

## Diapositiva 3: Fetch para obtener información de una serie

La función recibe el `IdSerie` y realiza una petición Fetch. La URL señala el controlador y la acción que se quieren llamar y envía el identificador de la serie. El ejemplo utiliza el método `GET` y el encabezado `Content-Type` con valor `application/json`.

Código JavaScript de la presentación:
```javascript
function GetInfo(idserie)
{
    fetch( '/Home/VerInfo?IdSerie=' + idserie, {method: 'GET',
        headers: { 'Content-Type': 'application/json' },
    })
    .then(response => {console.log(response); return response.json();})
    .then(data => {
        document.getElementById("ModalTitle").innerHTML = "Serie " + data.nombre;
        const body = data.añoInicio + "<br>" + data.sinopsis;
        document.getElementById("ModalBody").innerHTML = body;
    })
    .catch((error) => {
        console.error('Error:', error);
    });
}
```

Explicación proporcionada en la presentación:
1. La función recibe el identificador de la serie y llama al controlador y a la acción indicados en la URL, enviando ese parámetro. Se señala que, al igual que un formulario, Fetch también utiliza un método de envío y un formato de contenido.
2. Fetch trabaja de manera asíncrona: la respuesta no llega inmediatamente. El primer `.then()` se ejecuta cuando responde el servidor.
3. La variable `response` representa la respuesta HTTP completa e incluye información como el código de estado, si la petición fue exitosa y el contenido enviado por el servidor.
4. El segundo `.then()` se ejecuta cuando el JSON ya fue convertido en un objeto de JavaScript. La variable `data` contiene la serie recibida en forma de objeto.
5. `.catch()` se ejecuta cuando la petición al controlador vuelve con algún error.

En este ejemplo, `document.getElementById(...)` permite acceder a los elementos `ModalTitle` y `ModalBody`; la propiedad `innerHTML` cambia el contenido que se muestra dentro de ellos.

## Diapositiva 4: Devolver información desde el controlador

En el controlador, el ejemplo se ocupa de obtener la información del modelo y devolvérsela a JavaScript. El método no devuelve una vista, sino un objeto. Por ese motivo el tipo de retorno que se muestra es `Serie` y no `IActionResult`.

Código del controlador mostrado:
```csharp
public Serie VerInfo(int IdSerie)
{
    BD MiBD = new BD();
    return MiBD.GetInfo(IdSerie);
}
```

Esta forma queda autorizada únicamente en el contexto presentado: la acción `VerInfo` devuelve un objeto `Serie` y utiliza `BD.GetInfo(IdSerie)`. No generalices esta forma de retorno a cualquier acción ni inventes otros tipos o métodos.

## Diapositiva 5: Devolver una lista de objetos

Si el resultado de la búsqueda es una lista de objetos, el controlador puede devolver esa lista y JavaScript puede recorrerla para construir el contenido que se muestra en el modal.

Código del controlador mostrado:
```csharp
public List<Actor> VerActores(int IdSerie)
{
    BD MiBD = new BD();
    return MiBD.GetActores(IdSerie);
}
```

Código JavaScript mostrado para recibir y mostrar la lista:
```javascript
function GetActores(idserie,tituloSerie)
{
    fetch( '/Home/VerActores?IdSerie=' + idserie, {method: 'GET',
        headers: { 'Content-Type': 'application/json' },
    })
    .then(response => response.json())
    .then(data => {
        let tituloSerie = document.getElementById("ModalTitle");
        tituloSerie.innerHTML = "Actores de la serie - " + tituloSerie ;
        let body="";
        data.forEach(item => {
            body += item.nombre + "<br>";
        });
        document.getElementById("ModalBody").innerHTML = body;
    })
    .catch((error) => {
        console.error('Error:', error);
    });
}
```

La presentación muestra `response.json()` para convertir la respuesta, luego `data.forEach(item => ...)` para recorrer los actores y construir una cadena en `body`, agregando cada `item.nombre` y un salto HTML `"<br>"`. Finalmente, el contenido de `ModalBody` se actualiza mediante `innerHTML`. El ejemplo también utiliza `.catch()` y `console.error(...)` para manejar errores.

## Importante sobre la transcripción del ejemplo

El código anterior se transcribe respetando la forma visible en el material. En la función `GetActores`, el parámetro `tituloSerie` y la declaración `let tituloSerie` aparecen con el mismo nombre; además, la asignación del título concatena la variable `tituloSerie` tal como se ve arriba. Si una consigna requiere corregir ese ejemplo, señalá la posible inconsistencia antes de modificarlo y explicá qué cambio haría falta. No lo corrijas silenciosamente ni utilices la inconsistencia como permiso para inventar otra sintaxis.

## Reglas cerradas específicas para Fetch

- Solo podés utilizar Fetch con las formas, URL de ejemplo, método `GET`, encabezado, `.then(...)`, `response.json()`, `.catch(...)`, `console.log(response)`, `console.error(...)`, `document.getElementById(...).innerHTML` y `data.forEach(...)` incluidos en este apartado.
- No agregues otros métodos HTTP, parámetros, encabezados, métodos de promesas, métodos del DOM ni técnicas de serialización o deserialización que no aparezcan aquí.
- No inventes la implementación de `GetTemporadas`, porque la presentación solamente muestra su llamada desde un botón.
- Los métodos `VerInfo`, `VerActores`, `GetInfo`, `GetActores`, `BD.GetInfo` y `BD.GetActores` quedan incluidos solo en el contexto exacto de los ejemplos anteriores. No supongas que podés crear métodos semejantes con nombres o comportamientos nuevos sin autorización.
- Si se requiere algo diferente de estas formas, detenete y preguntame si autorizo el recurso específico antes de escribirlo.


## Criterios de HTML y CSS aportados por el usuario

Este apartado se agrega al material técnico anterior y es igualmente obligatorio. Se aplica a las consignas de páginas web y diseño visual. Todo lo siguiente forma parte de la lista cerrada: no amplíes los recursos autorizados por conocimiento externo.

### 1. Estructura general del proyecto
Primero dividimos las páginas en distintos HTMLs y un solo CSS.

**Estructura básica de cada HTML:**
- `head`
- `title`
- `body`

**Elementos semánticos**

- `header`: contiene al encabezado de una sección o documento, donde se colocan habitualmente vínculos de navegación, los formularios de búsqueda, etc. Sus contenidos son consistentes a lo largo de todo el sitio.
- `main`: representa el contenido principal del `body` de un documento o aplicación. El área principal del contenido consiste en el contenido que está directamente relacionado, o se expande sobre el tema central de un documento o la funcionalidad central de una aplicación.
- `section`: es un elemento de seccionamiento genérico, diseñado para contener temáticamente una parte de un documento definida. Los contenidos de un elemento `section` son usualmente precedidos por un encabezado.
- `article`: el elemento `article` en HTML define un contenido autocontenido y reutilizable, como artículos, entradas de blog, comentarios o widgets, permitiendo su distribución independiente.
- `div`: el elemento `div` es un contenedor genérico que agrupa y estructura dentro de una página, sin aportar significado semántico, utilizado principalmente para aplicar estilos o manipulación mediante CSS y JavaScript.
- `footer`: representa al pie de una sección o documento, donde los autores habitualmente colocan firmas, información acerca del autor, etc. Son consistentes a lo largo de todo el sitio.

**Otros elementos HTML incluidos en los apuntes:** `h1`, `p`, `pre`, entre otros nombrados expresamente en el material general. La palabra “etc.” no autoriza por sí sola etiquetas nuevas: si una etiqueta no aparece concretamente en el material autorizado, preguntá antes de usarla.

### 2. Imágenes
Utilizar:
```html
<img src="imagen.jpg" alt="Descripción">
```
Atributos incluidos: `src` y `alt`.

### 3. Enlaces
Etiqueta `a` y atributo `href`:
```html
<a href="pagina.html">Ir a la página</a>
```

### 4. Botones
```html
<button>Enviar</button>
```

### 5. Formularios
Se puede usar `form` con los atributos `method`, los valores `get` y `post`, y `action`.

Ejemplo:
```html
<form method="post" action="gracias.html">

   <input type="email" />
   <input type="tel" />
   <input type="date" />
   <input type="url" />
   <input type="color" />
   <input type="search" />

   <input
   type="number"
   max="999"
   min="0"
   step="1" />

   <input
   type="range"
   max="999"
   min="0"
   step="1" />

</form>
```

También se puede usar: `checkbox`, `radio`, `select`, `reset`, `submit` y `placeholder`.

Este apartado se suma a los ejemplos de formularios MVC incluidos anteriormente en el material. En cada caso, utilizá solo los atributos, etiquetas y tipos de `input` expresamente incluidos en alguno de los dos apartados de clase.

### 6. Listas

**No ordenadas:**
```html
<ul>
   <li>Elemento</li>
</ul>
```

**Ordenadas:**
```html
<ol>
   <li>Elemento</li>
</ol>
```

En CSS:
```css
list-style-type: none;
```

### 7. Navegación
El elemento HTML `nav` representa una sección de una página cuyo propósito es proporcionar enlaces de navegación, ya sea dentro del documento actual o a otros documentos. Ejemplos comunes son menús, tablas de contenido e índices.

Normalmente lo usamos junto con:
```html
<nav>
   <ul>
       <li><a href="">Inicio</a></li>
   </ul>
</nav>
```

### 8. Iframes
Se pueden usar para:
- Google Maps.
- Videos de YouTube.

### 9. Clases e IDs

**Clases**
Se pueden utilizar clases, sin abusar de ellas. Tienen un nivel de detalle mayor. Sirven para aplicar un estilo a un conjunto de componentes de un mismo tipo (o que sirvan para un mismo propósito), pero de forma diferenciada al selector de tipo. Por ejemplo, `.banners` podría hacer referencia a todos los `img` del sitio relacionados con una tarjeta de perfil de una persona.

**IDs**

HTML:
```html
id="xx"
```

CSS:
```css
#xx
```

### 10. CSS: propiedades de texto

**Colores:**
```css
color: red;
color: #ffffff;
color: rgb(...);
color: rgba(...);
```

**Tamaño de fuente:**
```css
font-size: em;
font-size: large;
font-size: larger;
font-size: px;
```

**Peso:**
```css
font-weight: bold;
```

**Decoración:**
```css
text-decoration: underline;
```

**Alineación:**
```css
text-align: center;
```

**Transformación:**
```css
text-transform: uppercase;
```

**Sombra:**
```css
text-shadow: #7f8c8d;
```

**Fuente:**
```css
font-family: ...;
```

Los valores con `...` son representativos del apunte; no autorizan a incorporar propiedades nuevas.

### 11. CSS: espaciado

**Margin:** `margin`, `margin-top`, `margin-bottom`, `margin-left`, `margin-right`.

**Padding:** `padding`, `padding-top`, `padding-bottom`, `padding-left`, `padding-right`.

### 12. CSS: bordes

**Border radius:** `border-radius: ...;`

**Border:**
```css
border: 1px solid black;
```
Donde `1px` es el tamaño, `solid` es el estilo y `black` es el color.

También: `border-top`, `border-right`, `border-bottom` y `border-left`.

### 13. CSS: tamaños

Propiedades: `width` y `height`.

- `width`: se pueden usar `px` y `%`.
- `height`: se pueden usar `px`, `%` y `auto`.

### 14. CSS: fondos

```css
background-color: #2C3E50;
background-image: url("imagen.jpg");
background-repeat: no-repeat;
background-position: center;
background-size: cover;
background-attachment: fixed;
```

### 15. Selectores CSS

**Selector universal:** `*`.

**Selector por etiqueta:** `p`, `div`, `h1`.

**Selector descendente:** `div h3`.

**Selector múltiple:** `h3, h4`.

**Selector por ID:** `#miId`.

### 16. Selectores de atributo

Elementos `a` con atributo `title`:
```css
a[title] {
   color: purple;
}
```

`href` exacto:
```css
a[href="https://example.org"] {
   color: green;
}
```

`href` que contiene texto:
```css
a[href*="example"] {
   font-size: 2em;
}
```

`href` que comienza con:
```css
a[href^="#"] {
   color: #001978;
}
```

`href` que termina con:
```css
a[href$=".org"] {
   font-style: italic;
}
```

`class` que contiene una palabra:
```css
a[class~="logo"] {
   padding: 2px;
}
```

### 17. Pseudoclases
Se pueden usar:
- `:first-child`
- `:last-child`
- `:hover`

### 18. Float

```css
float: left;
float: right;
```

También se puede usar `clear`.

### 19. Display
La propiedad `display` en CSS define cómo se comporta un elemento en la estructura de la página. Determina si se muestra:
- En línea con otros elementos (`inline`).
- Ocupando todo el ancho disponible (`block`).
- Combinando características de ambos (`inline-block`).

Es fundamental para controlar el diseño y la distribución del contenido en la web.

### 20. Flexbox
Podemos usar:
```css
display: flex;
justify-content: ...;
align-items: ...;
```

### 21. Grid
Permite organizar elementos en áreas o regiones dentro de un contenedor. Podemos definir estructuras bidimensionales precisas, controlando tanto la alineación horizontal como vertical de los elementos. Facilita la creación de diseños complejos y adaptables.

Ejemplos:
```css
display: grid;
grid-template-columns: 1fr 1fr 1fr;
gap: ...;
width: ...;
margin: ...;
```

### 22. Position
Podemos usar:
```css
position: relative;
position: absolute;
```

### 23. Jerarquía y especificidad CSS
En CSS, la jerarquía de estilo determina qué reglas se aplican a un elemento cuando existen varias reglas en conflicto. La prioridad se establece de acuerdo con:
- La especificidad del selector.
- El orden en que se declaran las reglas.

Es clave respetar el orden y seguir un patrón en el desarrollo de los proyectos.

**Orden de prioridad indicado en los apuntes:**
1. `!important`
2. ID
3. Atributo
4. Clase
5. Etiqueta
6. `*` (selector universal)

### 24. Estructura de una regla CSS

Ejemplo:
```css
p {
   color: red;
}
```

- `p`: selector.
- `color`: propiedad.
- `red`: valor de la propiedad.
- `color: red;`: declaración.

### 25. Buenas prácticas
- No abusar de `br`.
- Se pueden usar clases, pero sin abusar de ellas.
- Es clave respetar la jerarquía y especificidad de CSS.
- Seguir un patrón ordenado en el desarrollo del proyecto.

===================== FIN DE LOS CRITERIOS DE HTML Y CSS =====================


===================== FIN DEL MATERIAL DE CLASE =====================
