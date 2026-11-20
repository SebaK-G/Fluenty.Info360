# Fluenty

**Capacitación en IA a la medida de cada puesto de trabajo.**

Proyecto final de Info 360 · 4.º C Informática · Escuela ORT, sede Almagro · 2026

## Integrantes

- Sebastián Ko
- Federico Martín Architector
- Lenny Guido Bercovich
- Matías Tarsi

## Descripción

Muchas empresas quieren que su equipo use la inteligencia artificial, pero los cursos que hay son generales: no enseñan cómo la usa un vendedor, un administrativo o alguien de atención al cliente, y la gente los abandona.

Fluenty es una app web donde el dueño arma el **árbol de rangos** de su empresa (por ejemplo Dueño › Encargado › Vendedor). Cada rango tiene su propio link: cada persona entra por el link de su rango y completa un formulario corto contando qué hace en su trabajo. Con eso, la IA arma **un curso para cada rango**, con ejemplos del trabajo real de cada uno, y **nivela a cada persona**: cada uno arranca en la unidad que le corresponde.

Los cursos se hacen en **lecciones cortas estilo Duolingo**, con tarjetas, preguntas, ejercicios de "Probalo vos" que corrige la IA, XP y racha de días. Quien tiene gente a cargo ve el avance de su equipo y quién no avanza.

## Funcionalidades

- **Landing** con la presentación del producto e **inicio de sesión**.
- **Árbol de rangos** (Equipo): el dueño crea los rangos, cuenta qué hace cada uno y comparte el link de cada rango (también por WhatsApp).
- **Formulario para sumarse**: cada persona se une con el link de su rango y cuenta su trabajo, sus herramientas y su experiencia con la IA. Si deja floja la tarea que más tiempo le lleva, se le hace una pregunta más.
- **Formulario de la empresa**: el dueño cuenta las dificultades, las herramientas y los objetivos de su empresa (una sola vez).
- **Cursos por rango con IA**: se genera un curso de 4 unidades y 20 lecciones (la quinta de cada unidad es un examen). Queda como borrador: se puede rehacer una unidad con un pedido a la IA, descartar o confirmar. Se puede sumar al curso a quien se une después.
- **Cursos públicos**: un curso se puede publicar para todas las empresas. El buscador (Explorar) filtra por texto, categoría y nivel, y cualquiera se puede inscribir.
- **Lecciones**: tarjetas que explican, preguntas que se corrigen en el servidor (si se equivoca, le toca otra pregunta del mismo tema), "Probalo vos" corregido por la IA, "Explicamelo más simple", "Dame otro ejemplo", escuchar la tarjeta y copiar el pedido bueno.
- **Nota, XP y racha**: al terminar cada lección; si la primera lección le sale perfecta puede saltar al examen, y si le cuesta mucho puede bajar una unidad.
- **Home y Empleados**: cada uno ve sus cursos con su avance; quien tiene gente a cargo ve sus números, quién no avanza y las últimas actividades.
- **Asistente de IA**: chats guardados para preguntarle a la IA cómo usarla en su trabajo o cómo se usa Fluenty.
- **Calculadora de eficiencia**: cuántas horas (y cuánta plata) ahorra el equipo usando IA en una tarea, con los cálculos guardados.

## Tecnologías

- ASP.NET Core MVC (.NET 9) con C#
- SQL Server con Stored Procedures, usando Dapper (`Query`, `QueryFirstOrDefault` y `Execute`)
- HTML, CSS y JavaScript (con `fetch` para las acciones que no recargan la página)
- IA: Groq, llamado por solicitudes HTTP desde `Services/GroqService.cs`

## Cómo está organizado

- `Controllers/`: cada controller lee la sesión, llama a la lógica de su pantalla y arma los ViewBag (o devuelve un objeto para `fetch`).
- `Models/`: las clases de cada tabla, `BD.cs` (todas las llamadas a los Stored Procedures) y una clase de lógica por pantalla (`LogicaEquipo`, `LogicaCurso`, `LogicaLeccion`, etc.).
- `Services/`: la conexión con la IA (`GroqService`) y el armado de las lecciones (`GeneradorLecciones`).
- `Views/`: solo muestran lo que llega por ViewBag. El CSS de cada pantalla va en el `<head>` del `_Layout` con `@section Estilos`.
- `wwwroot/js/`: un archivo de JavaScript por pantalla, y `site.js` con lo que usan varias.
- `ScriptFluentySprint2.sql`: la base de datos completa (tablas, datos de prueba y Stored Procedures).

## Cómo ejecutar el proyecto

1. Instalar el **SDK de .NET 9**, **SQL Server** y **SQL Server Management Studio (SSMS)**.
2. Clonar el repositorio:
   ```
   git clone https://github.com/SebaK-G/Fluenty.Info360.git
   ```
3. En SSMS, abrir `ScriptFluentySprint2.sql` y ejecutarlo: crea la base `FluentySprint2` con sus tablas, datos y Stored Procedures.
4. La conexión está en `Models/BD.cs` y usa `Server=localhost` con autenticación de Windows. Si tu SQL Server tiene otro nombre, cambiá `localhost` por el tuyo.
5. Cargar la clave de la IA (Groq) en User Secrets, desde la carpeta del proyecto:
   ```
   dotnet user-secrets set "Groq:ApiKey" "TU_CLAVE_DE_GROQ"
   ```
   Sin la clave, la app funciona igual, pero no se pueden generar cursos ni lecciones nuevas.
6. Ejecutar:
   ```
   dotnet run
   ```
   y abrir en el navegador la dirección que aparece en la consola.
7. Usuario de prueba (dueño de la empresa): `lennybercovich@gmail.com`.

## Enlaces

- Trello: _(pegar el link del tablero)_
- Figma: _(pegar el link del prototipo)_
- Miro: https://miro.com/welcomeonboard/blpkSkZYb2dITXBEbFJFSUIyMkx1OHdpd25CWkFSVjlxUWJSVGo2Vi96aDdwcGJhY09uL09CamMwekpmSUxIK252K2tsUlZBb3VKNzV6T3p3SHRSdmV4aVUwWmhybS95b3lNL1A1TXdjeHNRTFpMc0ZnWlp1cVpxd0M3cEw5a2RnbHpza3F6REdEcmNpNEFOMmJXWXBBPT0hdjE=?share_link_id=867363535594

## Lo que usamos fuera de lo visto en clase (y por qué)

- **El código de la IA** (`Services/GroqService.cs`, `GeneradorLecciones`): `async`/`await`, `HttpClient` y `JsonSerializer`, porque la IA se llama por HTTP y responde en JSON.
- **`encodeURIComponent`** en JavaScript: para mandar con `fetch` textos que escribe la persona (con espacios, tildes o signos) sin que se rompa la URL.
- **"Escuchar" y "Copiar"** en las lecciones: usan `speechSynthesis` (la voz del navegador) y `navigator.clipboard` (el portapapeles). Ayudan a quien le cuesta leer en pantalla y a usar el pedido en su IA.
- **`STRING_SPLIT`** en algunos Stored Procedures: para filtrar en SQL por varios rangos a la vez (`'2,3,5'`), en vez de traer a toda la empresa y filtrar en C#.
- **`Guid.NewGuid()`**: arma el código único del link de cada rango.
- **`WebUtility.HtmlEncode`** en el asistente: así lo que escriben la persona o la IA se muestra como texto y nunca se ejecuta como HTML.
