# AGENTS.md

Instrucciones para asistentes de código que trabajen en este repositorio.

## Reglas de oro

- **No borrar nunca más sonidos, música ni ficheros del proyecto original.** Los assets son imprescindibles; cualquier limpieza debe confirmarse siempre con el usuario.
- El proyecto target es .NET Framework 4.5 (C# 5), mono-proyecto `src/Bejeweled3Accessible.csproj`.
- Dos únicos builds soportados: `Debug` y `Release` (MSBuild del Framework).
- El juego se ejecuta desde `bin\Debug\` o `bin\Release\`; nunca mover el exe fuera de su carpeta.
- `audio.pac` es **el** paquete de audio para distribución: se genera con `Bejeweled3Accessible.exe --pack-audio` (y se puede regenerar cuando cambien los assets).
- Las imágenes del juego (gemas, logo heatwave) **no van en el PAC**: se cargan en caliente desde `sounds\images\` junto al exe. Por eso el zip de release DEBE incluir esa carpeta.
- **Versiones y hotfixes:** si el usuario indica «hotfix», la release se etiqueta con el **mismo día** y se incrementa el último componente (`v2026.08.10.1`, `v2026.08.10.2`, ...). Si no hay hotfix y pasa el día, la release se etiqueta con el **día siguiente** (`v2026.08.11.0`, ...). Nunca inventar una fecha distinta ni saltarse el día.

## Build y tests

- Compilar: `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe src\Bejeweled3Accessible.csproj /t:Build /p:Configuration=Debug` (y `Release`).
- Tests: compilar `src\Bejeweled3AccessibleTests.csproj` y ejecutar `bin\Debug\Bejeweled3AccessibleTests.exe` → deben pasar **145/145** (con `--no-audio`: 130 ejecutados + 15 tests de sonido real omitidos, 0 fallos).
- Los tests tardan ~20 s; no se necesitan para cambios triviales pero sí para los que tocan `Board`, motor de audio o lógica de juego.
- Para no interferir con el usuario: `Bejeweled3AccessibleTests.exe --no-audio` omite las pruebas que reproducen sonido (las de prefijo `Sound:`) y termina en segundos; se usa en comprobaciones intermedias y deja la suite completa (con audio) para la versión que se publica.

## Release

- Publicar: `gh release create v2026.08.10.X --notes-file <notas> --title "Bejeweled 3 Accesible v2026.08.10.X"` y subir el zip con `gh release upload`.
- El zip se construye con: exe y PDB de `bin\Release\`, `bass.dll`, `nvdaControllerClient32.dll` (de `bin\Debug`), `mscorlib.dll` + `norm*.nlp` + `es\` (directo de `bin\Release`), `README.html`, `audio.pac` (el actual de la release anterior o regenerado con `--pack-audio`) y **`sounds\images\` completa** (gemas 600/768/1200 + NonResize).
- **Después de publicar una release, limpiar `C:\Users\artik\AppData\Local\Temp\opencode`** (zips de descarga, staging y capturas intermedias) para no dejar espacio ocupado en el disco.
- La versión se sube en 3 sitios a la vez: `src\Properties\AssemblyInfo.cs`, los textos `LoadingTitle` y `AppTitle` de `src\Engine\Localization.cs`, y la línea de versión + changelog de `README.html`.

## Convenciones de código

- Español en mensajes de commit; scripts PowerShell con salida en español.
- Los archivos fuente están en UTF-8.
