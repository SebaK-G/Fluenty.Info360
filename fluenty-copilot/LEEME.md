# Instalar Fluenty en GitHub Copilot (VS Code)

1. Descomprimí la carpeta `.github` en la raíz del repositorio donde trabajás.
2. La estructura final tiene que quedar así:

```text
TU-PROYECTO/
└── .github/
    └── agents/
        ├── fluenty.agent.md
        └── material-clase.md
```

3. Abrí VS Code y seleccioná el agente personalizado **fluenty** en el selector de agentes de Copilot.
4. Mantené `material-clase.md` en la misma carpeta que `fluenty.agent.md`. El agente lo referencia antes de responder.

## Por qué son dos archivos

El archivo `fluenty.agent.md` contiene las reglas de funcionamiento del agente. `material-clase.md` conserva la referencia de clase, incluidos los ejemplos de C#, MVC/Razor, formularios, propiedades, JavaScript/DOM, Dapper, Sessions y Fetch/JSON.

Se separan porque el cuerpo de un agente personalizado tiene un límite de tamaño; dividir instrucciones y material permite que el archivo del agente se mantenga dentro del límite sin eliminar el material de referencia.
