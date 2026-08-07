# Bejeweled 3 Accesible

Versión del juego Bejeweled 3 adaptada para jugadores con discapacidad visual.
Toda la información del juego se transmite por voz (NVDA / subtítulos de sonido) y
el tablero se gobierna por teclado sin depender del ratón.

Este documento es técnico: describe la estructura, el build y la ejecución de tests.

---

## Requisitos

- Windows (x64)
- .NET Framework 4.5 (solo para **compilar**; el binario funciona sobre 4.x runtime)
- MSBuild del .NET Framework 4.0 en adelante (`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe`)
- `bass.dll` junto al ejecutable (carpeta `bin\Debug\`), con `BASS` sin restricción de importa de namespace `Audio`
- NVDA u otro lector de pantalla **opcional** (el juego con voces internas OGG funciona sin él)

La detección de idioma es ES por defecto; las claves `Localization.cs` contienen traducción EN
(y tablas `Ambient*` para el modo Zen).

---

## Estructura de la solución

```
bejeweled3_accessible/
├─ src/
│  ├─ Bejeweled3Accessible.csproj        # Proyecto principal (WinExe, .NET 4.5)
│  ├─ Bejeweled3AccessibleTests.csproj   # Proyecto de tests (exe de consola)
│  ├─ Program.cs                         # Punto de entrada
│  ├─ UI/
│  │  └─ MainWindow.cs                   # Ventana, menús, teclado, bucle de juego (~2300 L)
│  ├─ Accessibility/
│  │  └─ NvdaSpeech.cs                   # Salida NVDA sobre WAMP / interop
│  ├─ Audio/
│  │  ├─ SoundEngine.cs                  # BASS audio: música, SFX y voces (un solo motor)
│  │  ├─ PacReader.cs                    # Lector de audio.pac (OGG cifrado XOR)
│  │  ├─ PacPacker.cs                    # Generador de audio.pac
│  │  └─ PacCipher.cs                    # Clave XOR única compartida R/P
│  ├─ Engine/
│  │  ├─ Board.cs                         # Tablero, matches, cascadas, especiales, gravedad
│  │  ├─ Gem.cs                           # GemColor, SpecialType, Gem
│  │  ├─ QuestManager.cs                 # Modos Quest (objetivos, dificultad 1-5)
│  │  ├─ ZenManager.cs                   # Modo Zen (ambient música tabla, mantras, respiración)
│  │  ├─ HintFinder.cs                   # Pista de movimiento disponible
│  │  ├─ RankSystem.cs                    # Niveles y títulos de puntuación
│  │  ├─ BadgeManager.cs                 # Insignias persistidas en XML
│  │  ├─ PokerHandEvaluator.cs           # Evaluación de manos (modo Poker)
│  │  ├─ GameProgress.cs / GameOptions.cs / ProfileManager.cs  # Persistencia XML
│  │  └─ Localization.cs + StoragePaths.cs  # Tablas ES/EN y rutas de datos
│  └─ Tests/
│     └─ TestRunner.cs                    # 102 tests unitarios sin framework externo
├─ music/          # Pistas MP3 de música y ambientes
├─ sounds/         # OGG e imágenes extraídas del juego original
├─ AUDIT.md        # Auditoría completa + estado de remediación
└─ README.html     # Manual del jugador (copiado a bin al compilar)
```

## Compilación

Desde `src\`, con MSBuild del Framework:

```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe Bejeweled3Accessible.csproj  /p:Configuration=Debug
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe Bejeweled3Accessible.csproj  /p:Configuration=Release
```

- El juego genera `..\bin\Debug\Bejeweled3Accessible.exe` (o `bin\Release\`).
- El proyecto funciones como "juego" y los tests; **todos los `.cs` se declaran explícitamente** en el `.csproj` (`<Compile Include>`). Al añadir un archivo nuevo hay que registrarlo en **ambos** csproj.
- Los assets `music\`, `sounds\` y `audio.pac` se leen relativos a la carpeta del ejecutable (carpeta raíz).
- No hay NuGet: depende solo de las referencias del GAC (System.* con System.Speech) y de `bass.dll`.

## Ejecución de tests

```
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe Bejeweled3AccessibleTests.csproj /p:Configuration=Debug
bin\Debug\Bejeweled3AccessibleTests.exe
```

> Los tests son autónomos (no usan framework de terceros): un exe de consola que
> ejecuta la lista `TestRunner` y escribe el resumen al stdout. La suite de
> referencia es **102/102 en verde** tras la remediación de la auditoría.

## Arquitectura del audio

- **BASS** unifica música (loop + fade + reverb binaural), efectos (lista `_activeSfxList`, cap de 25 que nunca corta un canal en reproducción) y voces (cola con pump en `System.Threading.Timer`, no overlap).
- **Voces**: `voice_*.ogg` de `sounds\sounds\` o `audio.pac`; duraciones cacheadas para no decodificar OGG en el hilo UI (`PreloadVoiceDurations` se lanza en un `ThreadPool` al arrancar).
- **Persistencia**: XML en la carpeta de datos (perfil): `profiles.xml`, `progress.xml`, `options.xml`, `badges_<perfil>.xml`. Errores de IO/serialización → `persistence_errors.log`; errores de BASS → `audio_errors.log`.

## Accesibilidad

- Tablero 8x8 por celdas anunciadas `columna/fila` (verbal), pan espacial HRTF (pan -0.85…+0.85) a la medida del tablero para ubicar gemas; los swaps y cascadas se oyen deslizarse de una columna a otra (swipe llamativo) y las voces del locutor siempre al centro.
- Combinaciones de teclas principales en `MainWindow.HandlePlayingKeys` (ver AUDIT.md; las `Shift+X` se evalúan antes que las simples).
- El modo Zen reduce sonido de interacción (mantras, respiración, ambientes) — útil para relajación.

## Estado

La auditoría (`AUDIT.md`) está **cerrada**: todos los hallazgos fueron corregidos,
aceptados y documentados, y la suite de tests está en verde (102/102).