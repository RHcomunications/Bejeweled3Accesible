# 💎 Bejeweled 3 Accesible - Informe Técnico, Arquitectura y Anecdotario

**Proyecto:** Bejeweled 3 Accesible (Clon Fiel y Accesible de Bejeweled 3 para Jugadores Ciegos y con Baja Visión)  
**Repositorio:** `RHcomunications/Bejeweled3Accesible`  
**Versión Actual:** Windows: `v2026.08.30.2` | Android: `android-v2026.08.27.2`  
**Tecnología Base:** 
- **Windows (`main`):** C# (.NET Framework 4.5), Windows Forms, BASS Audio Engine (P/Invoke nativo), libopenmpt (decodificador de módulos .mo3), SAPI 5 / NVDA Controller Client.
- **Android (`android`):** C# (.NET 9 Android / MAUI), Android Accessibility Framework (`AccessibilityManager`, `AnnounceForAccessibility`), `SoundPool` para efectos de ultra baja latencia y `MediaPlayer` para la banda sonora original completa en MP3.

---

## 📖 1. Visión y Propósito del Proyecto

El objetivo primordial de **Bejeweled 3 Accesible** es recrear con exactitud matemática, sonora y de diseño la experiencia del clásico juego de PopCap Games (**Bejeweled 3**), haciéndolo **100% jugable sin visión** a través de sintetizadores de voz (NVDA y SAPI5 en Windows; TalkBack nativo en Android) y un sistema de **Audio Espacial de Tablero** (paneo estéreo simple por columna, clásico Bejeweled; sin sala/rebotes ni EQ de profundidad), manteniendo simultáneamente una interfaz visual de alta calidad y soporte completo tanto de teclado como de ratón y gestos táctiles.

---

## 🏛️ 2. Arquitectura del Sistema Multiplataforma

```
bejeweled3_accessible/
├── src/
│   ├── Accessibility/
│   │   └── NvdaSpeech.cs          # Interfaz bidireccional NVDA Controller / SAPI 5 fallback (Windows)
│   ├── Android/
│   │   ├── Accessibility/
│   │   │   └── TalkBackBridge.cs  # Puente de accesibilidad nativo con TalkBack y sintetizador del usuario
│   │   ├── Audio/
│   │   │   └── AndroidSoundEngine.cs # Motor dual SoundPool (baja latencia) + MediaPlayer (OST continua)
│   │   ├── UI/
│   │   │   └── GameScreenView.cs  # Vista gráfica interactiva, gestos, modo apaisado, pista y pausa
│   │   ├── MainActivity.cs        # Actividad principal; ConfigurationChanges para no recrear la Activity al rotar a Landscape (bug crítico de jugabilidad corregido en android-v2026.08.25.5)
│   │   └── Bejeweled3.Android.csproj # Proyecto .NET 9 Android
│   ├── Audio/
│   │   ├── SoundEngine.cs         # Motor BASS, colas atómicas, ducking, rutas espaciales de grid
│   │   ├── SpatialAudio.cs        # Modelo grid espacial estático: PanColumn, DepthForRow, Volume/Air/Width
│   │   ├── (GridSpatializer.cs ELIMINADO: el render binaural de 'objeto en sala' se borró; SFX usan paneo BASS directo)
│   │   ├── AudioMap.cs            # Mapa canónico tipado de los 190 efectos de sonido oficiales
│   │   ├── MusicMap.cs            # Mapa canónico de las 29 pistas musicales originales (suite + ambientales)
│   │   ├── PacCipher.cs           # Cifrado XOR / ofuscación del contenedor audio.pac
│   │   ├── PacPacker.cs           # Empaquetado en memoria; excluye los MP3 01-23 (offsets del módulo .mo3)
│   │   └── PacReader.cs           # Lector de archivos PAC en streaming
│   ├── Engine/
│   │   ├── Board.cs               # Cuadrícula 8x8, físicas de gravedad, cascadas y gemas especiales
│   │   ├── Gem.cs                 # Definición de gemas, formas geométricas canónicas y modificadores
│   │   ├── QuestManager.cs        # Estructura de 5 Relicarios y 40 Misiones con 11 variantes
│   │   ├── PokerHandEvaluator.cs  # Evaluador de manos de póker (Flush, Full House, Parejas, etc.)
│   │   ├── RankSystem.cs          # Sistema de 131 rangos y títulos oficiales de PopCap
│   │   ├── BadgeManager.cs        # Insignias comunes (Bronce a Platino) y 5 Insignias Élite
│   │   ├── ZenManager.cs          # Ambientes Zen, respiración guiada, mantras y afirmaciones
│   │   ├── HintFinder.cs          # Algoritmo de detección de movimientos y sugerencias
│   │   ├── Localization.cs        # Diccionario dinámico bilingüe (Español / English)
│   │   ├── GameProgress.cs        # Persistencia de estadísticas, desbloqueos y récords
│   │   ├── ProfileManager.cs      # Gestión de perfiles de usuario independientes
│   │   └── StoragePaths.cs        # Rutas canónicas en AppData
│   ├── UI/
│   │   └── MainWindow.cs          # Bucle de juego Windows, renderizado GDI+, teclado y ratón
│   ├── Update/
│   │   └── AutoUpdater.cs         # Sistema de auto-actualización vía GitHub Releases API
│   └── Tests/
│       └── TestRunner.cs          # Suite de pruebas unitarias integradas
├── music/                         # Banda sonora completa (29 MP3s originales masterizados de estudio)
├── sounds/                        # 190 efectos de sonido oficiales extraídos en .ogg
```

---

## 🎮 3. Mecánicas y Modos de Juego Replicados

### Modos Principales
1. **Modo Clásico (Classic):** Partida infinita por puntos con barra de progreso de nivel. Transición espacial *"Level Complete"* con túnel de luz (warp sound).
2. **Modo Relámpago (Lightning):** Partida contrarreloj de 60 segundos base, donde crear gemas de tiempo (+5s, +10s) y encadenar combos rápidos otorga tiempo extra continuo.
3. **Modo Zen:** Experiencia relajante sin fin ni peligro de Game Over. Cuenta con 6 ambientes naturales (*Coastal, Crickets, Forest, Ocean Surf, Rain Leaves, Waterfall*), modulación de respiración de 4 fases y banco oficial de mantras/afirmaciones.
4. **Modo Búsqueda (Quest):** Restauración de los 5 Relicarios sagrados a través de 40 desafíos únicos (Alquimia, Bombas, Fiebre de Oro, Mariposas, Tormenta de Hielo, Poker, etc.).

### Modos Secretos Desbloqueables
- **Póker:** Desbloqueado al nivel 5 de Clásico. Cada combinación añade una carta a la mano; calaveras bloquean manos aleatorias.
- **Mariposas:** Desbloqueado al nivel 5 de Zen. Las mariposas suben 1 fila por cada turno jugado hacia la araña superior.
- **Tormenta de Hielo:** Desbloqueado al superar 100,000 puntos en Relámpago. Columnas de hielo que ascienden y deben quebrarse antes de congelar el tablero.
- **Mina de Diamantes:** Desbloqueado al completar 4 misiones del Relicario 1 en Quest. Filas inferiores de tierra blanda y roca dura con excavación física (+10m de profundidad) y rescate de pepitas de oro.

---

## 🎧 4. Banda Sonora y Audio Espacial

1. **Banda Sonora Original Completa (29 Pistas Oficiales)**:
   - Integrados los 29 temas musicales originales masterizados de PopCap Games.
   - **Encadenamiento Dinámico Continuo (*Seamless Chaining*)**: En los modos Clásico y Zen, el motor de sonido encadena de forma fluida las 4 fases evolutivas de cada modo (*Part 1*, *Part 2*, *Part 3*, *Part 4*) replicando con exactitud la experiencia del tracker `.MO3`.
   - Incluye todas las pistas temáticas de pruebas de Quest (*Buried Treasure*, *Take Your Time*, *Turn by Turn*, *Time Bombs*, *Quest Theme*, *Quest Finale*) y Bonus tracks (*Remix Medley*, *Final Turn*, *Gems of Glass*).
2. **Voces Auténticas del Locutor de PopCap Games**:
   - Reintegradas todas las locuciones oficiales (*Awesome*, *Excellent*, *Extraordinary*, *Unbelievable*, *Spectacular*, *Welcome to Bejeweled*, *Welcome back*, etc.) para los combos, jugadas especiales y bienvenida.
3. **Audio Espacial Grid**:
    - Paneo estéreo simple por columna (A-H); la profundidad por fila (`DepthForRow`) se modela en `SpatialAudio` pero no se aplica (sin sala/rebotes ni EQ de profundidad). Música y locuciones centradas.
   - Locuciones del narrador y síntesis de voz centradas con *ducking* automático de la música.
 4. **Paneo estéreo simple por columna y Ducking**:
     - El render binaural de "objeto en sala" (`GridSpatializer`) se **eliminó** (era demasiado agresivo). Los SFX se panearan en estéreo por columna (A-H) vía `BASS_ATTRIB_PAN`, sin sala/rebotes ni EQ de profundidad. El flag `SoundEngine.BinauralEnabled` (toggle en Opciones) controla únicamente si el paneo por columna está activo; al desactivarlo todo suena centrado.
     - Paneo logarítmico (`Math.Pow(|t|, 1.4)`) que aplana el centro y abre los extremos A/H al máximo (`±MaxPan`). Con 8 columnas no hay columna central única: las columnas 3 y 4 quedan cerca de 0 en lados opuestos.
     - `bass_fx.dll` (x64) + `bass_fx32.dll` (x86) se mantienen junto al exe por si se reactiva el pitch en el futuro, pero el pitch de cascada/Relámpago NO los usa (variantes pre-renderizadas con rubberband `gem_hit_p0..p12.ogg` +1 semitono por nivel).
     - **Ducking (sidechain)** musical en hipercubo y Supernova (la música baja al 30 % y vuelve).

---

## 📱 5. Port Oficial para Android

1. **TalkBack 100% Nativo con Árbol de Accesibilidad Virtual (`AccessibilityNodeProvider`)**:
   - Cada menú, botón (Pista 💡, Pausa ⏸️) y las 64 celdas del tablero (A1 a H8) son nodos accesibles individuales.
   - Navegación estándar mediante deslizamiento de 1 dedo a la derecha/izquierda y activación por doble toque.
   - Utiliza de forma transparente la voz, velocidad y sintetizador que el usuario tenga configurado en su dispositivo (Vocalizer, Eloquence, RHVoice, etc.).
2. **Paridad Total de Pantallas y Funcionalidades con Windows**:
   - **Escuela de Audio (`AudioSchool`)**: Pruebas de paneo de 8 columnas L/R y profundidad frente/fondo.
    - **Opciones Zen (`ZenOptionsScreen`)**: Configuración completa de pistas ambientales, mantras y respiración guiada.
   - **Creación Accesible de Perfiles (`AlertDialog`)**: Cuadro de diálogo nativo con campo de texto accesible para escribir el nombre real del jugador con el teclado del teléfono.
   - **Pantalla de Fin de Juego (`GameOver`)**: Opciones claras de reintento o regreso al menú principal.
   - **Gestión Dinámica de Volúmenes**: Modificación en vivo de música, efectos y voces con persistencia en `options.xml`.
3. **Modo Apaisado (*Landscape*) con Tablero Amplio**:
   - Orientación horizontal obligatoria para visualización y accesibilidad óptima del tablero 8x8.
   - Panel lateral derecho con botones dedicados y accesibles de **Pista (💡 HINT)** y **Pausa (⏸️ PAUSA)**.
4. **Control Asistido por Gestos y Táctil**:
   - Deslizamientos verticales (arriba/abajo) para explorar opciones de menú.
   - Deslizamiento hacia la izquierda para retroceder menús.
   - Exploración táctil de tablero: al tocar cualquier gema, TalkBack anuncia sus coordenadas, color y direcciones posibles de movimiento (ej. *"C4: Gema Roja. Puedes mover hacia la derecha o abajo"*), y al deslizar se ejecuta el intercambio inmediato.
5. **Gestión Completa de Perfiles y Persistencia en `FilesDir`**:
   - Almacenamiento seguro en el directorio privado de la aplicación para perfiles, configuraciones y medallas.

---

## 📝 6. Anecdotario Técnico y Soluciones Clave

1. **Separación de Pistas vs. Archivo `.MO3` en Android**:
   - *Desafío:* Android carece de soporte nativo para reproducir trackers `.MO3` dinámicos con `libopenmpt` sin incurrir en fallos de compatibilidad NDK multiplataforma.
   - *Solución:* Se extrajeron las pistas oficiales masterizadas de estudio y se programó un sistema de encadenamiento dinámico (*Seamless Playlist Listener*) que enlaza las 4 partes de Clásico y Zen de forma continua.
2. **El Conflicto de TTS Interno vs TalkBack en Android**:
   - *Desafío:* La implementación inicial usaba `TextToSpeech` directo, lo que forzaba las voces de Google o Samsung e ignoraba las preferencias del usuario.
   - *Solución:* Se migró a la API nativa de eventos de accesibilidad (`AccessibilityManager` y `AnnounceForAccessibility`), permitiendo que TalkBack hable con el sintetizador configurado por el usuario (Eloquence, Vocalizer, etc.).
3. **Árbol Virtual vs. Activación Accidental al Levantar el Dedo**:
   - *Desafío:* El canvas monolítico provocaba que TalkBack viera un botón sin etiqueta y la activación se disparaba automáticamente al levantar el dedo en modo exploración.
   - *Solución:* Se implementó `GameAccessibilityNodeProvider` exponiendo cada celda y botón de forma individual, separando la exploración táctil de la activación por doble toque.
4. **Organización del Repositorio y Ramas**:
    - *Windows (`main`)*: Código fuente C# .NET 4.5, releases en `.zip` con auto-actualizador y `README.md` de escritorio.
    - *Android (`android`)*: Código fuente C# .NET 9 Android, releases en `.apk` firmados vía GitHub Actions y `README.md` adaptado a la experiencia móvil.
    - *Unificación (2026-08-26)*: `main` se fusionó con `android`, así que **ambas ramas contienen hoy todo el código** (motor de audio, TalkBack, updaters). La distinción de plataforma ya no está en la rama sino en el **prefijo del tag de release**: `v…` = Windows (`.zip`), `android-v…` = Android (`.apk`).
    - *Actualizadores aislados por plataforma*: `AutoUpdater` (Windows) ignora tags `android-*` y ofrece siempre la última `v…`; `AndroidAutoUpdater` enumera y elige la `android-v…` mayor. Cada uno busca en su "rama" de tags, independiente del marcador Latest del repo.
5. **Soporte Completo de Ratón y Flechas en Windows (v2026.08.24.3)**:
   - *Desafío:* Usuarios con baja visión o educadores querían interactuar con ratón sin perder la verbalización de casillas y opciones de movimiento.
   - *Solución:* Se implementó eco de ratón hablado, cálculo de direcciones válidas en tiempo real y soporte completo de flechas direccionales en el tablero.

6. **Guarda de instancia única en Windows (hotfix v2026.08.25.1)**:
    - *Desafío:* Lanzar el .exe dos veces abría dos ventanas que duplicaban audio e interfaz.
    - *Solución:* `Program.cs` adquiere un `Mutex` global `Global\Bejeweled3Accessible-SingleInstance`; si ya existe, trae la ventana existente al frente y sale. El empaquetado `--pack-audio` también está protegido. Si el sistema niega el mutex (permisos), arranca igualmente.

7. **Compilación y publicación de releases vía GitHub Actions**:
    - *Android*: el workflow `.github/workflows/android.yml` se dispara al pushear a `android`, compila el APK en el runner (ubuntu + .NET 9 workload Android), lo firma y sube como artifact. Se descarga con `gh run download` y se publica con `gh release create` + `gh release upload`.
    - *Windows*: se compila localmente con MSBuild (`Debug`+`Release`), se ejecuta la suite de tests (144/144) y se empaqueta el `.zip` manualmente; luego `gh release create`/`upload`. El agente no puede compilar Android localmente (falta el workload Xamarin/.NET-Android).

8. **Firma estable de Android (keystore commiteado)**:
    - *Desafío:* El workflow regeneraba el keystore en cada run, así que cada APK tenía una firma distinta y no se podía actualizar en sitio sobre una instalación previa.
    - *Solución:* Se generó y commiteó `.github/release.keystore` (alias `bejeweled3key`, storepass `bejeweled3secret`). El workflow ahora lo reutiliza, de modo que todas las releases Android futuras comparten la misma firma y permiten actualización en sitio. El APK de `android-v2026.08.25.5` ya se re-publicó firmado con esta clave estable.

 9. **Modelo de canales dev/stable y bug 404 del auto-updater (2026-08-28)**:
    - *Canales (decisión del usuario)*: `Debug` = canal **dev** → corre en la máquina del usuario y se mantiene al día **reconstruyendo `bin\Debug` en el workspace**; **jamás se publica un zip de Debug en el repo**. `Release` = canal **stable** → se publica en GitHub (`v…`) y es lo que recibe el público. El auto-updater elige canal en tiempo de compilación con `#if DEBUG` (`AutoUpdater.IsDevBuild`): en `Debug` no consulta GitHub (se actualiza localmente al reconstruir); en `Release` consulta la última release estable. Por tanto, tras cada cambio de código hay que reconstruir también `Debug`.
    - *Bug 404*: el actualizador descargaba `…/releases/download/vX/Bejeweled3Accesible-<ver>.zip` (una 'c', según `AutoUpdater.ZipAssetPrefix`), pero los zips se habían subido como `Bejeweled3Accessible-…zip` (doble 'c') → **404**. Se renombraron los assets de `v2026.08.27.0`, `v2026.08.28.0` y `v2026.08.28.1` a la forma de una 'c'. Regla estricta: el nombre del zip SIEMPRE debe ser `Bejeweled3Accesible-<version>.zip` (una 'c'), idéntico a `AutoUpdater.BuildZipAssetName(tag)`; si no, el updater falla con 404.
     - *Causa raíz real del 404 (2026-08-28, release v2026.08.28.2):* además de la 'c', el otro factor eran los **ceros a la izquierda**. `Version.ToString()` NO rellena con ceros, así que `AutoUpdater.BuildZipAssetName("v2026.08.28.2")` produce `Bejeweled3Accesible-2026.8.28.2.zip` (sin ceros), pero el paquete se había subido como `Bejeweled3Accesible-2026.08.28.2.zip` (con ceros) → **404**. Regla definitiva: el zip se nombra **sin ceros** (`2026.8.28.2`), coincidiendo exactamente con `BuildZipAssetName`; de lo contrario el updater arma otra URL y da 404.

 10. **Empaquetado compacto de `audio.pac` (162 MB → 14 MB) y orden de explosión en cascadas (v2026.08.28.2)**:
     - *Contexto:* `audio.pac` pesaba ~162 MB porque empaquetaba también los 29 MP3 de música, pero las pistas 01-23 no son archivos sueltos: son **offsets** dentro del módulo `Bejeweled3_suite.mo3` (0.74 MB) que el motor reproduce con `libopenmpt` (saltando al orden/pista). Los 23 MP3 eran ~148 MB muertos.
     - *Solución (`PacPacker.cs`)*: se añadió `IsRedundantModuleMp3(file, baseDir)` que omite los `.mp3` de `music\` cuyo nombre corresponde a un offset del módulo (`MusicMap.OrderForTrack(name) >= 0`). El PAC queda en **~14 MB** (SFX + módulo `.mo3` + ambientales 24-29). Los 23 MP3 originales siguen en `music/` como respaldo dev, pero ya no se empaquetan ni se usan en runtime.
     - *Cascadas (corrección de cierre/crash + solapamiento):* en `MainWindow.cs` la explosión de impacto suena **junto a su combo en cada nivel** de la cadena (mismo evento) y la creación de gema especial (supernova/hipercubo/estrella/flama) se dispara en el **último nivel** junto al combo final, vía `BeginInvoke` al hilo de UI (BASS se inicializa en el hilo de UI); cada lambda va en `try/catch`. Así combos y explosiones no se solapan. El crash nativo de combos grandes se corrigió con un lock en `ModuleMusicPlayer` (dispose del módulo libopenmpt no destruye mientras BASS decodifica). Combos empiezan en nivel 2. `gem_fall.ogg` (caída por gema) y `gem_hit_p0..p12.ogg` (pitch rubberband) para cascadas/Relámpago sin `BASS_FX_TempoCreate`. El audio binaural (`GridSpatializer`) se eliminó: SFX = paneo estéreo por columna, música centrada.
     - *Restauración de assets:* un apagado precipitado del equipo borró los 23 MP3 originales de `music-ost-original/`; se recuperaron con `git checkout -- music-ost-original/`. El working tree quedó limpio tras esto.
     - *Release (re-publicado tras corregir 404):* el auto-updater arma la URL del zip **sin ceros** (`2026.8.28.2`) porque `Version.ToString()` no rellena, pero el asset se había subido con ceros (`2026.08.28.2`) → **404**. Se renombró el paquete a `Bejeweled3Accesible-2026.8.28.2.zip` (sin ceros) y se re-publicó el release `v2026.08.28.2` (mismo tag, commit de fix `ffde7df` + rebuild). El zip (19.95 MB) incluye `libopenmpt.dll` + 4 `openmpt-*.dll`, `bass.dll`, `bass_fx.dll` (x64) + `bass_fx32.dll` (x86), `nvdaControllerClient32.dll`, `mscorlib.dll`, `norm*.nlp`, `es\`, `README.html`, `audio.pac` (~14 MB) y `sounds\images\` completa; sin `Tests.*`, sin `music/`, sin `sounds/*.ogg`.
     - *Cascadas más ágiles (mismo release):* la reacción en cadena ahora avanza con una **cadencia fija de ~200 ms por nivel** (`chainStepMs`) en lugar de esperar a que terminara cada combo (`comboMs` de hasta ~1,6 s), eliminando la lentitud de varios segundos por nivel. Los combos se solapan ligeramente, como en el original.

  11. **Bug de build que bucleaba el auto-updater (v2026.08.28.3, 2026-08-29)**:
      - *Síntoma:* los usuarios en `2026.08.28.2` veían ofrecida la actualización a `v2026.08.28.3` una y otra vez sin nunca actualizar.
      - *Causa raíz:* `SpatialSfxSource` es una **clase de nivel superior** (no anidada en `SoundEngine`) y no podía ver el miembro **privado** `BASS_ATTRIB_PAN` de `SoundEngine` → error `CS0103`. El build incremental (`/t:Build`) no recompiló `SoundEngine.cs`, así que en `bin\Release` quedó el exe viejo `2026.08.28.2`; el zip de release se armó con ese exe. El updater detectaba `v2026.08.28.3` como más nuevo y, como el cambio real nunca se aplicaba, volvía a ofrecerlo en bucle.
      - *Solución:* `BASS_ATTRIB_PAN` se declaró como `const` **local** dentro de `SpatialSfxSource` (junto a sus otras constantes BASS). Se reconstruyó con `/t:Build` (no `/t:Rebuild`) y se re-subió el asset al mismo tag `v2026.08.28.3` con `gh release upload --clobber`. Tests en **144/144** (se relajó el assert de paneo: 8 columnas no tienen columna central única, así que las columnas 3 y 4 quedan cerca de 0 en lados opuestos).
      - *Lección de build (CRÍTICA):* **NUNCA usar `/t:Rebuild`** para este proyecto. El `.csproj` copia los assets (`audio.pac`, `bass*.dll`, `libopenmpt.dll`, `openmpt-*.dll`, `nvdaControllerClient32.dll`, `README.html`) desde `..\bin\Debug\...` vía `<Content Include="..\bin\Debug\...">`; `Rebuild` ejecuta `Clean` y borra esos assets de `bin\Debug` → `MSB3030: No se pudo copiar el archivo "bin\Debug\audio.pac" porque no se encontró.` Para forzar recompilación completa se hace `touch` de los `.cs` (o se borra solo el exe/pdb), **nunca** se limpian los assets.

  12. **Puntuación y progresión de cascadas en tiempo real (v2026.08.30.1, 2026-08-30)**:
      - *Mecánica PopCap:* En Bejeweled 3 los puntos y el bonus acumulativo por nivel de cascada (+50 x nivel) se acumulan y reflejan en el marcador y la barra de nivel con cada explosión y combo en sucesión directa, no en bloque al final del turno.
      - *Implementación:* `CascadeResult` calcula `StepPoints` y `StepHypercubeCreationPoints` por cada paso/iteración. En `MainWindow.cs` cada ciclo del temporizador de combos invoca la suma incremental de puntos al marcador, barra de progreso y rango en el hilo de UI sincronizado con el sonido del combo y la explosión correspondiente.

  13. **Inmunidad a movimientos involuntarios del ratón y opción configurable (v2026.08.30.2, 2026-08-30)**:
      - *Problema:* Notificaciones del sistema operativo, ventanas emergentes o toques accidentales en touchpads hacían que el puntero del ratón pasara sobre la ventana del juego, desplazando el cursor del tablero y verbalizando casillas inesperadas mediante el evento de sobrevuelo pasivo (`MouseMove`).
      - *Solución:* (a) Se eliminó el movimiento pasivo por sobrevuelo (hover) en el tablero: el cursor solo cambia con clics deliberados o teclas de flechas/WASD. (b) Se integró la opción "Control con Ratón: Activado/Desactivado" (`MouseEnabled`) en el menú de Opciones (`options.xml`), permitiendo anular totalmente la interacción con ratón para quienes prefieren juego exclusivo por teclado.

---

## 🏆 7. Estado del Proyecto y Releases

- **Windows (`main`)**: Release `v2026.08.30.2` (Hotfix: protección contra movimientos involuntarios de ratón en el tablero por notificaciones del sistema; opción configurable de control con ratón en Opciones; puntuación en tiempo real en cascadas y menú GameOver interactivo). Suite de 144 tests en verde.
- **Android (`android`)**: Release `android-v2026.08.27.2` (recorrido vertical por columnas, cabecera dinámica y selección secuencial de 2 pasos) con TalkBack 100% nativo, árbol de accesibilidad virtual de 64 nodos, auto-actualizador de APK que busca su propio tag `android-v…`, y APK firmado con keystore estable (actualización en sitio). **Rama congelada (sin releases nuevas por ahora).**
- **Cómo distinguir al distribuir**: tag `v…` + asset `.zip` = Windows; tag `android-v…` + asset `.apk` = Android. El auto-actualizador de cada plataforma entrega el correcto sin que el usuario elija.
- **Flujo de release:** bump en `AssemblyInfo.cs`, `Localization.cs` (LoadingTitle/AppTitle) y `README.html` (versión + changelog ES/EN); en Windows build Debug+Release + suite completa (144/144) y zip con exe/PDB Release + `bass.dll` + `bass_fx.dll` (x64) + `bass_fx32.dll` (x86) + `nvdaControllerClient32.dll` + `libopenmpt.dll` + 4 `openmpt-*.dll` + `mscorlib.dll` + `norm*.nlp` + `es\` + `README.html` + `audio.pac` (generado por `--pack-audio`, ~14 MB) + `sounds\images\` completa (sin `sounds/*.ogg` ni `music/`). **Regla crítica del nombre del zip:** `Bejeweled3Accesible-<version>.zip` SIN ceros a la izquierda (p.ej. `2026.8.30.2`), porque `Version.ToString()` no rellena; si lleva ceros (`2026.08.30.2`) el updater arma otra URL y da 404. **NUNCA usar `/t:Rebuild`** (ver anecdotario 11). En Android el APK se compila en GitHub Actions (ver anecdotario 7); `gh release create` + `gh release upload`; limpiar `Temp\opencode`.
