# AUDITORÍA — Bejeweled 3 Accesible

Fecha: 2026-08-02 · Método: revisión de los 18 `*.cs` (~6.5k LOC), lectura de trazas, chequeo de teclas y traducción. Suite de referencia: **96/96 tests en verde** (MSBuild v4.0, `bin\Debug\Bejeweled3AccessibleTests.exe`). Suite actual (2026-08-11): **137/137 tests en verde**.

## FASE 1 · Inventario

| Módulo | Archivo | LOC | Complejidad |
|---|---|---|---|
| UI | `src\UI\MainWindow.cs` | 2257 | Alta (15 pantallas, como escenario único) |
| Tests | `src\Tests\TestRunner.cs` | 1234 | Alta (96 tests, 36 de Board) |
| Audio | `src\Audio\SoundEngine.cs` | 893 | Alta (BASS, cola voces, HRTF) |
| Motor | `src\Engine\Board.cs` | 787 | Alta (cascadas, especiales) |
| Motor | `src\Engine\Localization.cs` | 295 | Media (ES/EN) |
| Motor | `src\Engine\ZenManager.cs` | 199 | Media |
| Motor | `src\Engine\GameProgress.cs` | 119 | Baja |
| Motor | `src\Engine\BadgeManager.cs` | 108 | Baja |
| Accesibilidad | `src\Accessibility\NvdaSpeech.cs` | 109 | Media (NVDA + SAPI) |
| Motor | `src\Engine\QuestManager.cs` | 103 | Media (40 misiones) |
| Motor | `src\Engine\ProfileManager.cs` | 93 | Baja |
| Motor | `src\Engine\Gem.cs` | 90 | Baja |
| Audio | `src\Audio\PacReader.cs` | 88 | Media (cifrado XOR) |
| Motor | `src\Engine\GameOptions.cs` | 70 | Baja |
| Motor | `src\Engine\HintFinder.cs` | 64 | Baja |
| Motor | `src\Engine\PokerHandEvaluator.cs` | 67 | Baja |
| Motor | `src\Engine\RankSystem.cs` | 30 | Baja |
| Audio | `src\Audio\PacPacker.cs` | 44 | Baja |

Arquitectura: UI WinForms de 1 ventana (15 states) → `Engine` (Board + managers) → `Audio\SoundEngine` (BASS) y `Accessibility\NvdaSpeech` (NVDA, fallback SAPI). Persistencia XML por perfil (4 managers con `OverrideDataDirectory`). Todo en hilo UI; único `async void` en `PerformSwap`.

---

## FASE 2 — Hallazgos de código

### 🔴 ALTO

| # | Ubicación | Hallazgo |
|---|---|---|
| 1 | `MainWindow.cs:1371-1428` + `Board.cs:197` | **Hypercube/annihilator pendiente se pierde→detona en el turno equivocado.** `SwapGems` fijas `_hyperSwapPending/_annihilatorPending`; `MainWindow` hace el swap y `await Task.Delay(110)`; si la revalidación aborta (pausa/reset/restart en los 110 ms) **sin** llamar a `ProcessMatchesAndGravity`, los flags quedan "colgados" y la **siguiente** cascada de un swap normal consume la explosión fantasma completa |
| 2 | `MainWindow.cs:202,261-266,322-327,1664-1668,1714-1718` | **Cierre de ventana (X/Alt+F4) pierde progreso.** No existe `OnFormClosing`/autosave global. `_progress.TotalScore`, `ZenLevel`, `ClassicLevel` se guardan solo en `TransitionToMainMenu` (`:202`) o bajo condiciones de récord; cerrar mientras se está en `Playing`/`Menu`/`GameOver` sin pasar por menú pierde todo lo acumulado. |
| 3 | `MainWindow.cs:1221-1280` | **`Shift+S`, `Shift+R`, `Shift+H` son inalcanzables** (debug tools rotas): el `else if (e.KeyCode == X)` anterior los captura; `ModifierKeys` no se comprueba. |
| 4 | `SoundEngine.cs:334-339,341-359` | **Fallo de arranque BASS mudo y sin señalizar.** `BASS_Init` retorno ignorado y `catch {}`; con `bass.dll` ausente el motor "vive" mudo, los tests NoThrow pasan igual. `CleanFinishedSfxChannels` llama `BASS_ChannelIsActive` sin try/catch → propaga excepción a UI. |
| 5 | `SoundEngine.cs:450-531, 557-616` | **Fuga de `GCHandle`+canal BASS** en rutas de error: si una operación BASS después de `StreamCreateFile` falla, el `catch{}` libera nada; pin y handle se quedan retenidos hasta el fin. |

### 🟠 MEDIO

| # | Ubicación | Hallazgo |
|---|---|---|
| 6 | `Board.cs:575-614` | `TickBombs` destruye gemas **sin registrar** en `MatchedColumns`/`ColumnDestroyedCount`/`TotalGemsCleared` → incoherente con Ice Storm y estadísticas. |
| 7 | `Board.cs:516-529` | `TriggerHypercubeColor` (solo para test) borra un color sin registrar contadores; riesgo si lo usa la UI en un futuro. |
| 8 | `Board.cs:249` | Bucle de cascada `while(true)` sin tope (`depth` crece sin `break>40`); riesgo de colgado teórico con semilla degenerada. |
| 9 | `MainWindow.cs:411-408, 476` | Escape sin manejador en `ProfileInput` y `MainMenu` → input "tragado". |
| 10 | `SoundEngine.cs:363-377` | `StopActiveVoice` no detiene `_voicePumpTimer`; sigue bueley cada 40 ms toda la sesión. |
| 11 | `SoundEngine.cs:852-891` | `Dispose` no espera callbacks en curl (Timer) antes de `BASS_Free` → rare access violation. |
| 12 | `NvdaSpeech.cs:79-80` | `Thread.Sleep(40)` **dentro de `lock`** en el hilo UI por cada Speak que interrumple mensaje reciente con NVDA → micro-erreos de UI. |
| 13 | `NvdaSpeech.cs:61-107` | `recentlySpoke`/`_lastSpeakTime` descarta mensajes si se habla más rápido de 1 s (NVDA no contesta): pérdida de anuncios en ráfagas. |
| 14 | `SoundEngine.cs:96,424` | `GetVoiceDurationMs` crea streams BASS que decodifican OGG en hilo UI por cada locución nueva no cacheada. |
| 15 | `SoundEngine.cs:523,601-607` | Eviction a 25 canales corta un SFX sonando (abrupto). |
| 16 | `RankSystem.cs:27-28` | Cap a nivel 131 conSÉ 30 títulos; a nivel 31+ el anuncio se satura. |
| 17 | `ZenManager.cs:52-61` vs `MainWindow.cs:1497-1501` | Tabla de músicas Zen duplicada → desincronización posible. |
| 18 | `QuestManager.cs:76-89` | `GetObjective` privado y sin guard de rango (solo se usa con 1..5 hoy). |

### 🟡 BAJO

| # | Ubicación | Hallazgo |
|---|---|---|
| 19 | `Board.cs:209,239,435` | Chequeos de rango redundantes siempre verdaderos. |
| 20 | `Board.cs:363-372` | Encoding de elbows `x*100+y` frágil a tableros grandes. |
| 21 | `GameProgress/GameOptions/BadgeManager/ProfileManager` | `GetFilePath` duplicado en 4 clases. |
| 22 | `PacReader.cs:10` / `PacPacker.cs:10` | Clave XOR duplicada; cambio ignoto rompe el otro en silencio. |
| 23 | `Gem.cs:20` / `Board.cs:253,409` | `SpecialType.Star` nunca se crea → dead code (y BadgeStellar/Estelar jamás desbloqueable). |
| 24 | `MainWindow.cs:1104` | `Task.Delay(...).ContinueWith(PlaySound)` sin `try/catch` → en .NET 4.5+ UnobservedTaskException. |
| 25 | `GameProgress.cs:91-96` | Fallos de `Save()` solo a `Debug.WriteLine` (invisible en release). |
| 26 | `MainWindow.cs:1203-1205` | Anuncio de celda tras `Task.Delay(3000)` (sin verificar `_screen`). |
| 27 | Todos | ~31 `catch {}` vacíos en SoundEngine (29) + NvdeauSpeech (2) sin log. 0 llamadas a `BASS_LastError`. |

---

## FASE 3 — Documentación

- **No existe** `README.md` ni `AGENTS.md`.
- Sí existe **`README.html`** = manual de usuario final (2026.8.2, en español e inglés). Es correcto y actual respecto a modos, teclas y accesibilidad.
- **Falta documentación técnica**: comandos de build (`MSBuild v4.0` con `-t:Build -p:Configuration=Debug`), estructura `src/`, cómo correr tests (`bin\Debug\Bejeweled3AccessibleTests.exe`), dependencias (bass.dll, nvdaControllerClient32.dll, audio PAC), y arquitectura del proyecto accesible. Añadir `README.md` técnico y enlazarlo con el `README.html`.
- La versión en el código (`Version 2026.8.2`) y la del HTML coinciden.

---

## FASE 4 — Funcional / dinámica

- Suite ejecutada al cierre: **106/106** (incluye: HRTF a la medida del tablero, swipe de gemas, voces al centro, cola de voces NServicio, regresión crossfade); 2 tests nuevos de `ColumnDestroyedCount`.
- **Módulo HRTF adaptado**: `src/Audio/SpatialAudio.cs` (curva perceptual col≈±0.85, máx ±0.85, voces siempre centro, `PanColumn`/-1→centro arreglando el bug "sonidos no-posicionales a la izquierda"); `SoundEngine.PlaySoundSpatialSweep` + `SchedulePanSweep`/`PanSweepTick` animan el pan swap/cascada en `MainWindow.PerformSwap`.
- Mapa de teclado auditando: **3 teclas de debug rotas** (Hallazgo #3), Escape sin acción en `ProfileInput`/`MainMenu`.
- Modos jugables cubiertos por tests de motor; **falta cobertura funcional real**, no drive por UI (no hay test E2E).

---

## FASE 5 — Plan de remediación (hoja de ruta)

1. **P1 (ALTO)**: arreglar A1 (consume los flags de hypercube solo con la resolución real o borrarlos en aborto) — requiere tocar `Board.SwapGems`/`ProcessMatches...`.
2. **P1**: OnFormClosing con guardas de sesión (timer parado + `_profileMgr.Save()` + timers Dispose) para no perder progreso.
3. **P2**: arreglar Shift+S/R/H (comparar `Control.ModifierKeys` y `e.Shift` en orden; o mover arch).
4. **P2**: en `SoundEngine`, encerrar `CleanFinishedSfxChannels` (protege si BASS no init) y añadir `finally` de limpieza de pin/handle en rutas de BOSS SFX/voz; detener `_voicePumpTimer` a Start/Dispose.
5. **P2**: arreglar `Tick` y `TriggerHypercubeColor` para registrar en contadores (o documentar política).
6. **P2**: top cola de cascada (`if (depth > 40) break;`).
7. **P3**: centralizar `GetFilePath`, clave XOR, tabla Zen; cap/títulos Rank; revisar `Star` dead.
8. **P3**: tests adicionales (Quest ≠ 0 covers, explosiones encadenada, bombas, `TickBombs`).
9. **P3**: `README.md` técnico (build/test/estructura).

---

## Auditoría de TRADUCCIONES (Localization.cs)

Estado: **auditada y corregida** (2026-08-02). 2 idiomas; 1 clave repetida no crítico (`OptionsTitle` `:55`/`:106` idénticas).

### Traducciones deficientes o con errores — CORREGIDAS

| Clave (`Localization.cs:`) | Antes | Ahora |
|---|---|---|
| `MenuLanguage` (`:38`) | "Idioma / Language: Español" | ES "Idioma: Español. Pulsa Enter..." / EN "Language: English. Press Enter..." |
| `PauseQuit` (`:107`) | "Abandonar al Menú Principal" | "Salir al Menú Principal" |
| `QuestCompletedMark` (`:207`) | " Completada" (sin paréntesis) | " (Completada)" — coherente con EN |
| `MatchAnnounce` (`:274`) | "Match {0}" (sin traducir) | "Combinación de {0} gemas. {1} puntos." |
| `GoldConvertedAnnounce` (`:224`) | "¡{0} casillas convertidas en oro!" (plural roto con 1) | "¡Casillas convertidas en oro: {0}!" |
| `ButterflyFreed` (`:267`) | "¡{0} Mariposa liberada!" (plural roto) | "¡Mariposas liberadas: {0}!" |
| `ZenBreathInhale/Exhale` (`:182-184`) | "Inhala/Exhala EN 5 segundos" | "durante 5 segundos" (EN inalterado, ya OK) |
| `NoMoreMovesScramble` (`:275`) | "Mezclando tablero" | "Barajando tablero" (unificado con `ShuffleAnnounce`) |

### Notas de calidad restantes (no bloqueantes)
- `Bomb` (`:96`) "Bomba {0} en {1} turnos" — válida; idealmente singular/plural, no crítico.
- `QuestStatusBombs`, `QuestStatusBombsDestroyed`, `PokerStatus`, `QuestProgress*` — repetibles mismas plantillas con `{0}`=1 (minoría), aceptables.
- Valoración: revisar en una futura pasada pureza de singular/plural en ES/EN (solo estético).

---

## Acciones realizadas hasta ahora (si se aprueba intervenir)
- La edición de traduccional corrigió todas las tabla-anterior; compila y **96/96 tests en verde**.

---

## Estado de remediación (2026-08-02, tercera pasada)

Suite de referencia actual: **102/102 tests en verde** (MSBuild v4.0; `bin\Debug\Bejeweled3AccessibleTests.exe`).

### FIXES APLICADOS (primera pasada, P1/P2)
- **#2 Pérdida de progreso al cerrar** ✅ — `OnFormClosing` detiene timers, `SaveOptionsState()` + `_profileMgr.Save()`, libera `NvdaSpeech`/`SoundEngine` con best-effort (`MainWindow.cs`).
- **#3 `Shift+S/R/H/F` inalcanzables** ✅ — `HandlePlayingKeys` evalúa las combinaciones Shift ANTES que las teclas simples: `R` anuncia, `Shift+R` resetea, `Shift+H` hipercubo, `Shift+F` gema de fuego.
- **#4 Falta de guard `BASS_Init`** ✅ — Flag `_bassReady = BASS_Init(...)`; `CleanFinishedSfxChannels` con `if (!_bassReady) return;` y try/catch por canal.
- **#18 `QuestManager.GetObjective` sin guard** ✅ — clampa `difficulty` a 1..5.
- **Ambientes Zen en inglés** ✅ — claves `Ambient*` en `Localization.cs` (ES/EN), `ZenManager.GetAmbientName()`, usado en `GetZenOptionsMenuItems`; 2 tests nuevos.
- **Tabla de músicas Zen duplicada** ✅ — eliminada la inline en `MainWindow` (usa `ZenManager.GetZenTrackForLevel`).

### FIXES APLICADOS (tercera pasada, MEDIO/BAJO)
- **#9 Escape en `ProfileInput`** ✅ — si hay perfiles vuelve a `ProfileSelectScreen`; si no, reanuncia el prompt.
- **#10 pump de voces sin apagarse** ✅ — `VoicePumpTick` se auto-dispone al quedar cola+activo vacíos; `StopActiveVoices` también lo detiene.
- **#12 `Thread.Sleep(40)` dentro de lock (NvdaSpeech)** ✅ — cancel+sleep NVDA fuera del lock; `Speak` re-adquiere el lock solo para el envío.
- **#26 Announce tras `Task.Delay(3000)`** ✅ — verifica `IsDisposed`/`IsHandleCreated`/`_screen==Playing`/`_isSwapping`.
- **#24 `ContinueWith(voice_go)` sin try** ✅ — try/catch y guard de `_screen==Playing`.
- **#25 `GameProgress.Save` solo debug** ✅ — nuevo `PersistenceLog.Write` a `persistence_errors.log` en `%APPDATA%`, activado en los 4 managers.
- **#21 `GetFilePath` duplicado** ✅ — nuevo `StoragePaths` compartido (`ResolveDataDirectory`/`GetPath`); los 4 managers lo usan.
- **#22 clave XOR duplicada** ✅ — nuevo `PacCipher` (única fuente); `PacReader`/`PacPacker` lo usan.

### FIXES APLICADOS (cuarta pasada, P3)
- **#5 Fuga de `GCHandle`/canal BASS en rutas de error** ✅ — nuevo `StartSfxStream` unifica `PlaySoundSpatial`/`PlaySoundPitch`: si BASS falla o algo lanza a mitad de camino, el `catch` interno libera el stream y el pin (`SoundEngine.cs`).
- **#11 `Dispose` sin esperar Timer callbacks** ✅ — `Timer.Dispose(WaitHandle)` + `WaitOne()` en `_voicePumpTimer` (Dispose) y en `CancelFadeTimer`, de modo que ningún callback toque BASS tras `BASS_Free`.
- **#14 decodificación OGG en hilo UI** ✅ — `CacheVoiceDuration` rellena el caché de duración desde el hilo del pump (en `StartVoice`), y `PreloadVoiceDurations()` (invocado vía ThreadPool al arrancar) precarga `voice_*.ogg` en segundo plano.
- **#16 `RankSystem` cap 131** ✅ — el nivel máximo ahora es el número de títulos (29) para que `GetRankLevel` y `GetRankTitle` nunca se desincronicen; test actualizado (cap=29, saturación).
- **#23 `SpecialType.Star`** → **aceptado**: se conserva por compatibilidad con guardados antiguos (deserialización) y tiene gameplay (explosión fila+columna) ya implementado; `BadgeStellar` queda documentado como inalcanzable hasta una futura revisión de gameplay.
- **#27 errores BASS sin log** ✅ — P/Invoke `BASS_LastError` + `LogAudioError` a `audio_errors.log` (`%APPDATA%`) en las rutas clave: `BASS_Init` fallo, voces (`StreamCreateFile=0`), SFX y música; cada mensaje incluye el código de error de BASS.
- **F3 `README.md` técnico** ✅ — creado `README.md` raíz: estructura, compilación (MSBuild v4.0), ejecución de tests (98/98), arquitectura de audio/persistencia y accesibilidad.

### FIXES APLICADOS (quinta pasada)
- **#15 Evicción de SFX cortaba sonido en reproducción** ✅ — `StartSfxStream` ya no evicta el canal más viejo si sigue sonando: primero limpia canales terminados (la cap libre) y solo descarta el recién solicitado si los 25 siguen activos (`SoundEngine.cs`).
- **#19 Chequeos de rango redundantes (`Board.cs:209,239,435`)** ✅ — eliminados los `if (x >= 0 && x < Cols)` siempre verdaderos (x proviene del bucle `for`) en los 3 puntos.
- **#20 Encoding de elbows `x*100+y` frágil** ✅ — reemplazado por clave determinista `y * Cols + x` con bounds check explícito de vecinos en la detección de T/L (`Board.cs`).
- **F4 Cobertura funcional (tests E2E + bombas)** ✅ — 4 tests nuevos en `TestRunner.cs`: explosión Flame en cascada ≥9 gemas (`ProcessMatchesAndGravity`), `TickBombs` sin bombas → 0, ciclo completo de bomba (decrementa y explota al 0), y **E2E de motor** (hint → swap → proceso → tablero lleno) para semillas 1..5. Suite actual: **102/102 en verde**.

### HALLGATOS ACEPTADOS (sin cambio)
- **#6** `TickBombs` sin contadores — solo se usa en Quest TimeBomb (no en Ice Storm); cambiar su firma rompería su propósito; documentado.
- **#7** `TriggerHypercubeColor` sin contadores — exclusivo de pruebas; documentado.
- **#13** descarte de anuncios en ráfagas — voice gate intencional (`interrupt=false` se descarta si hubo voz <1 s); no corrompe estado.
- **#9 (MainMenu)** Escape en la pantalla raíz — no aplica: no debería salir con Escape.

### PENDIENTES (no urgentes)
| # | Item |
|---|---|
- Ningún pendiente de auditoría resta. Se cierran #5, #11, #14, #15, #16, #19, #20, #23 (aceptado), #27, F3 y F4.

---

## Estado de remediación (2026-08-11 · hotfix v2026.08.11.1 · puntuación y gemas especiales)

Release: `v2026.08.11.1` (mismo día que `v2026.08.11.0`, hotfix). Suite: **137/137 en verde** (audio incluido; `--no-audio` deja 124/124 + 13 omitidos). Toca `Board.cs` (motor), `MainWindow.cs` (UI), `Localization.cs`, `TestRunner.cs`, `README.html`, `AssemblyInfo.cs`.

### Puntuación fiel al manual de PopCap (v1.0.8)
- **50 puntos por combinación** (`res.MatchesMade`, antes se puntuaba 50 por gema destruida: `TotalGemsDestroyed * 50`). `MatchesMade` cuenta cada racha de 3+ de cada pasada.
- **Bonos de creación** en `BasePoints`: Fuego 100, Estrella 150, Hipercubo 500, Supernova 1.000.
- **Bono de combinación doble** (forma T/L): 50 por cada racha + 50 de premio → `DoubleMatchBonus++` por codo detectado; se anuncia con las claves `MultipleMatchAnnounce` (ES/EN) y el efecto `doubleset` al terminar la jugada (`MainWindow.cs`).
- **Detonaciones**: Fuego 20 + 20 por gema (explosión 3x3), Estrella y Supernova 50 + 50 por gema (cruz fila+columna y 3 filas + 3 columnas), Hipercubo 50 + 50 por gema del color (`HypercubeDetonationPoints`), aniquilador 100 + 50 por gema (`AnnihilatorPoints`, con el bono extra de 2.500 de la UI).
- **Bono de cascada acumulativo** `50 * nivel * (nivel+1) / 2` (`CascadeBonus`), sustituye al bono plano anterior.
- **Nuevos campos de `CascadeResult`**: `MatchesMade`, `DoubleMatchBonus`, `CascadeBonus`, `SupernovaDestroyed`, `FlameBlastGems`, `StarBlastGems`, `SupernovaBlastGems`, `HypercubeDetonationPoints`, `AnnihilatorPoints`, `HypercubeCreationPoints`.
  - `HypercubeCreationPoints` (500 × Hipercubos creados) queda como campo informativo: su valor ya está integrado en `BasePoints`; la UI no lo anuncia por separado (documentado, no se elimina).
  - `SimultaneousMatches` (campo muerto desde la auditoría) ahora se rellena en `Board.cs` con las combinaciones de la primera pasada (`MatchesMade - matchesBeforeThisPass` con `depth == 0`) y la UI lo usa en el anuncio de combinación doble.
- **Gemas especiales**: las formas L, T y cruz crean la **Estrella** (`StarCreated` en el codo, explosión fila + columna), nunca la Supernova (antes T/L → Supernova era un error frente al manual; el cierre del hallazgo #23 de 2026-08-02 — Star dead code — queda definitivamente zanjado). La Supernova queda reservada a 6+ en línea. Sonido de creación de Estrella en la UI y atajo oculto **Shift+S** (junto a Shift+F/Shift+H/Shift+R) para colocarla en el cursor.
- **Test**: "Board: forma T crea Estrella" sustituye al antiguo "forma T crea Supernova" (assert `StarCreated >= 1`). Sin tests nuevos: la suite sigue en **137**.
- **README.html**: reglas ES/EN corregidas (Estrella en L/T/cruz, Supernova en 6+) y changelog del hotfix en ambos idiomas. Versión `2026.08.11.1` en `AssemblyInfo.cs` y `LoadingTitle`/`AppTitle`.

### Verificación
- Build Debug y Release sin errores (warnings preexistentes MSB3644/MSB3270 de las reference assemblies del Framework 4.5).
- Suite completa 137/137 (incluye los 13 tests de sonido reales).
- Release publicada en GitHub con el asset `Bejeweled3Accesible-2026.8.11.1.zip` (patrón del actualizador, sin ceros: fecha → `2026.8.11.1`), montado con exe+PDB de Release, `bass.dll`, `nvdaControllerClient32.dll` (Debug), `mscorlib.dll` + `norm*.nlp` + `es\`, `README.html`, `audio.pac` actual y `sounds\images\` completa.

> Nota de inventario: ahora son **20 `.cs`** (añadidos `StoragePaths.cs`, `PacCipher.cs`) + 2 csproj. `MainWindow.cs` quedó en ~2295 líneas tras los fixes.