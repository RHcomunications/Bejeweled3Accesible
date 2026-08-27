# 💎 Bejeweled 3 Accesible - Informe Técnico, Arquitectura y Anecdotario

**Proyecto:** Bejeweled 3 Accesible (Clon Fiel y Accesible de Bejeweled 3 para Jugadores Ciegos y con Baja Visión)  
**Repositorio:** `RHcomunications/Bejeweled3Accesible`  
**Versión Actual:** Windows: `v2026.08.25.1` | Android: `android-v2026.08.26.8`  
**Tecnología Base:** 
- **Windows (`main`):** C# (.NET Framework 4.5), Windows Forms, BASS Audio Engine (P/Invoke nativo), libopenmpt (decodificador de módulos .mo3), SAPI 5 / NVDA Controller Client.
   - **Android (`android`):** C# (.NET 9 Android / MAUI), Android Accessibility Framework (`AccessibilityManager`, `AnnounceForAccessibility`), `SoundPool` para efectos de ultra baja latencia y un reproductor de módulo `libopenmpt` → `AudioTrack` (igual que Windows) con `MediaPlayer` (MP3) como fallback para la banda sonora.

---

## 📖 1. Visión y Propósito del Proyecto

El objetivo primordial de **Bejeweled 3 Accesible** es recrear con exactitud matemática, sonora y de diseño la experiencia del clásico juego de PopCap Games (**Bejeweled 3**), haciéndolo **100% jugable sin visión** a través de sintetizadores de voz (NVDA y SAPI5 en Windows; TalkBack nativo en Android) y un sistema de **Audio Espacial de Tablero** (modelo grid con paneo por columna y profundidad por fila), manteniendo simultáneamente una interfaz visual de alta calidad y soporte completo tanto de teclado como de ratón y gestos táctiles.

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
│   │   │   ├── AndroidSoundEngine.cs # Motor dual SoundPool (baja latencia) + módulo libopenmpt/AudioTrack (fallback MediaPlayer)
│   │   │   └── AndroidModulePlayer.cs # Decodificador MO3 vía libopenmpt (P/Invoke) → AudioTrack PCM16
│   │   ├── UI/
│   │   │   └── GameScreenView.cs  # Vista gráfica interactiva, gestos, modo apaisado, pista y pausa
│   │   ├── MainActivity.cs        # Actividad principal; ConfigurationChanges para no recrear la Activity al rotar a Landscape (bug crítico de jugabilidad corregido en android-v2026.08.25.5)
│   │   └── Bejeweled3.Android.csproj # Proyecto .NET 9 Android
│   ├── Audio/
│   │   ├── SoundEngine.cs         # Motor BASS, colas atómicas, ducking, rutas espaciales de grid
│   │   ├── SpatialAudio.cs        # Modelo grid espacial estático: PanColumn, DepthForRow, Volume/Air/Width
│   │   ├── GridSpatializer.cs     # Render grid: pan equal-power + aire/profundidad por one-pole LP
│   │   ├── AudioMap.cs            # Mapa canónico tipado de los 189 efectos de sonido oficiales
│   │   ├── MusicMap.cs            # Mapa canónico de las 29 pistas musicales originales (suite + ambientales)
│   │   ├── PacCipher.cs           # Cifrado XOR / ofuscación del contenedor audio.pac
│   │   ├── PacPacker.cs           # Herramienta de empaquetado en memoria
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
├── sounds/                        # 189 efectos de sonido oficiales extraídos en .ogg
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
   - Paneo equal-power por columna (A-H) y profundidad/aire por fila (1-8).
   - Locuciones del narrador y síntesis de voz centradas con *ducking* automático de la música.
4. **Motor de Audio Binaural (BASS_FX) y Ducking**:
    - Paneo logarítmico por columna (extremos A/H abiertos al máximo) y profundidad frente→fondo por volumen + aire (one-pole LP).
    - Interruptor **Binaural** en Opciones (por defecto activo): al desactivarlo todo suena centrado y seco (passthrough).
    - BASS_FX: EQ por fila (agudos al frente, corte progresivo al fondo), barrido 360° del hipercubo (pan −1→+1 en bucle con ducking de la música) y **láser del star gem** (sweep L→R con barrido de EQ agudo→grave), cada uno con reverb y compresor por stream.
    - **Ducking (sidechain)** musical en hipercubo y Supernova (la música baja al 30 % y vuelve).

---

## 📱 5. Port Oficial para Android

1. **TalkBack 100% Nativo con Árbol de Accesibilidad Virtual (`AccessibilityNodeProvider`)**:
   - Cada menú, botón (Pista 💡, Pausa ⏸️) y las 64 celdas del tablero (A1 a H8) son nodos accesibles individuales.
   - Navegación estándar mediante deslizamiento de 1 dedo a la derecha/izquierda y activación por doble toque.
   - Utiliza de forma transparente la voz, velocidad y sintetizador que el usuario tenga configurado en su dispositivo (Vocalizer, Eloquence, RHVoice, etc.).
2. **Paridad Total de Pantallas y Funcionalidades con Windows**:
   - **Escuela de Audio (`AudioSchool`)**: Pruebas de paneo de 8 columnas L/R y profundidad frente/fondo.
   - **Opciones Zen (`ZenOptionsScreen`)**: Configuración completa de pistas ambientales binaurales, mantras y respiración guiada.
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

1. **Reproductor de módulo `.MO3` en Android (paridad con Windows)**:
   - *Desafío:* Android carece de soporte nativo para trackers `.MO3`; sin `libopenmpt` no hay decodificación del módulo.
   - *Solución:* Se implementó `AndroidModulePlayer` (P/Invoke `libopenmpt` + `AudioTrack` PCM16 @44100, *seek* por offsets de `MusicMap` y continuidad de la suite de 62 min), espejando `ModuleMusicPlayer` de Windows. Si la librería nativa (`libopenmpt.so`) no está empaquetada en el APK, `IsAvailable` lo detecta y el motor cae con elegancia a los MP3 por separado (misma música, sin regresión). El `Bejeweled3_suite.mo3` se incluye como `AndroidAsset`.
2. **El Conflicto de TTS Interno vs TalkBack en Android**:
   - *Desafío:* La implementación inicial usaba `TextToSpeech` directo, lo que forzaba las voces de Google o Samsung e ignoraba las preferencias del usuario.
   - *Solución:* Se migró a la API nativa de eventos de accesibilidad (`AccessibilityManager` y `AnnounceForAccessibility`), permitiendo que TalkBack hable con el sintetizador configurado por el usuario (Eloquence, Vocalizer, etc.).
3. **Árbol Virtual vs. Activación Accidental al Levantar el Dedo**:
   - *Desafío:* El canvas monolítico provocaba que TalkBack viera un botón sin etiqueta y la activación se disparaba automáticamente al levantar el dedo en modo exploración.
   - *Solución:* Se implementó `GameAccessibilityNodeProvider` exponiendo cada celda y botón de forma individual, separando la exploración táctil de la activación por doble toque.
4. **Organización del Repositorio y Ramas**:
    - *Windows (`main`)*: Código fuente C# .NET 4.5, releases en `.zip` con auto-actualizador y `README.md` de escritorio.
    - *Android (`android`)*: Código fuente C# .NET 9 Android, releases en `.apk` firmados vía GitHub Actions y `README.md` adaptado a la experiencia móvil.
    - *Unificación (2026-08-26)*: `main` se fusionó con `android`, así que **ambas ramas contienen hoy todo el código** (motor binaural, TalkBack, updaters). La distinción de plataforma ya no está en la rama sino en el **prefijo del tag de release**: `v…` = Windows (`.zip`), `android-v…` = Android (`.apk`).
    - *Actualizadores aislados por plataforma*: `AutoUpdater` (Windows) ignora tags `android-*` y ofrece siempre la última `v…`; `AndroidAutoUpdater` enumera y elige la `android-v…` mayor. Cada uno busca en su "rama" de tags, independiente del marcador Latest del repo.
5. **Soporte Completo de Ratón y Flechas en Windows (v2026.08.24.3)**:
   - *Desafío:* Usuarios con baja visión o educadores querían interactuar con ratón sin perder la verbalización de casillas y opciones de movimiento.
   - *Solución:* Se implementó eco de ratón hablado, cálculo de direcciones válidas en tiempo real y soporte completo de flechas direccionales en el tablero.

6. **Guarda de instancia única en Windows (hotfix v2026.08.25.1)**:
    - *Desafío:* Lanzar el .exe dos veces abría dos ventanas que duplicaban audio e interfaz.
    - *Solución:* `Program.cs` adquiere un `Mutex` global `Global\Bejeweled3Accessible-SingleInstance`; si ya existe, trae la ventana existente al frente y sale. El empaquetado `--pack-audio` también está protegido. Si el sistema niega el mutex (permisos), arranca igualmente.

7. **Compilación y publicación de releases vía GitHub Actions**:
    - *Android*: el workflow `.github/workflows/android.yml` se dispara al pushear a `android`, compila el APK en el runner (ubuntu + .NET 9 workload Android), lo firma y sube como artifact. Se descarga con `gh run download` y se publica con `gh release create` + `gh release upload`.
    - *Windows*: se compila localmente con MSBuild (`Debug`+`Release`), se ejecuta la suite de tests (145/145) y se empaqueta el `.zip` manualmente; luego `gh release create`/`upload`. El agente no puede compilar Android localmente (falta el workload Xamarin/.NET-Android).

8. **Firma estable de Android (keystore commiteado)**:
    - *Desafío:* El workflow regeneraba el keystore en cada run, así que cada APK tenía una firma distinta y no se podía actualizar en sitio sobre una instalación previa.
    - *Solución:* Se generó y commiteó `.github/release.keystore` (alias `bejeweled3key`, storepass `bejeweled3secret`). El workflow ahora lo reutiliza, de modo que todas las releases Android futuras comparten la misma firma y permiten actualización en sitio. El APK de `android-v2026.08.25.5` ya se re-publicó firmado con esta clave estable.

9. **Pantalla congelada con TalkBack (hotfix android-v2026.08.26.1)**:
   - *Causa:* En `android-v2026.08.26.0` se añadió a `GameScreenView.OnTouchEvent` un `return true` cuando había exploración táctil, para delegar al `AccessibilityNodeProvider`. Pero este binding de .NET para Android no expone `View.getVirtualViewAt` (ni en `View` ni en el provider), así que el framework no podía mapear el dedo a ningún nodo virtual y el doble toque no tenía destino → la pantalla parecía congelada (nada respondía al toque).
    - *Solución:* El tacto ya no se bloquea (el juego es jugable por toque también con TalkBack: tocar dos celdas contiguas intercambia; el menú se navega por tap). Durante la exploración táctil se desactivan los gestos de deslizamiento (swap/navegación) para que arrastrar para "leer" no provoque intercambios o saltos de menú no deseados. El `AccessibilityNodeProvider` se mantiene para describir nodos a TalkBack.

10. **Módulo MO3 real en el APK / APK mucho más ligero (hotfix android-v2026.08.26.2)**:
    - *Objetivo:* reproducir el `.mo3` de verdad en el dispositivo (sin depender del fallback de MP3) y reducir el tamaño del APK, que con 29 MP3 pesaba ~184 MB.
    - *Solución:* No hay `libopenmpt.so` precompilado para Android, así que se compila en CI con el NDK: el workflow descarga el tarball `libopenmpt-0.8.9+release.makefile.tar.gz`, copia `build/android_ndk/Android.mk` a `jni/Android.mk` y corre `ndk-build` con `MPT_WITH_MINIMP3=1` y `MPT_WITH_STBVORBIS=1` (decodifican MO3 sin librerías externas) y `APP_STL=c++_static` (para no arrastrar `libc++_shared.so`). El `.so` resultante se copia a `src/Android/libs/<abi>/libopenmpt.so` y se declara en el csproj como `AndroidNativeLibrary` (abi `arm64-v8a` y `x86_64`). Los 29 MP3 se eliminaron del asset (la música completa vive en `Bejeweled3_suite.mo3`, 62 min). El APK baja de ~184 MB a unos pocos MB y suena 1:1 con Windows. `AndroidModulePlayer.IsAvailable` sigue detectando si el `.so` falta en algún ABI y cae a los MP3 (ya no presentes), pero en la práctica el módulo suena en todos los dispositivos.

11. **Textos de interfaz coherentes con táctil en Android (override bilingüe en `Localization.cs`)**:
    - *Desafío:* `Localization.cs` es un diccionario compartido (ES/EN) entre Windows y Android, con verbos de teclado/ratón ("Presiona Enter", "Haz clic", "Usa las Flechas", "W/A/S/D", "Pulsa Enter") que en móvil son incorrectos y confunden a TalkBack. No existía detección de plataforma.
    - *Solución:* Se añadió la propiedad estática `UseAndroidStrings` (default `false`) y un segundo diccionario `_androidDict` con overrides ES/EN táctiles para las claves de acción (LoadingPrompt, EnterNamePrompt, MenuChangeUser, MenuLanguage, AudioSchoolTitle, TutorialTitle/Step1/2/3/4/5/7, Welcome, BadgeMenuHelp, UpdateFound/UpdateFoundNoNotes). `Get(key,args)` consulta primero el override cuando `UseAndroidStrings` está activo. `MainActivity.OnCreate` fija `Localization.UseAndroidStrings = true`, de modo que Windows queda idéntico y Android habla de doble toque, deslizar, tocar los botones de Pista/Pausa, etc. También se añadieron al diccionario principal las claves faltantes `GameReady` y `EnterNameConfirm` (usadas por el diálogo de perfil de Android y que antes se mostraban como texto crudo "GameReady"/"EnterNameConfirm").

 12. **Release `android-v2026.08.26.8` (consolidado): toda la rama Android en una sola release**:
     - *Cambio:* El usuario pidió borrar todas las releases de Android `.1`–`.11` y publicar una sola `.8` con el build binaural (que ya contenía acumulados todos los fixes). Se cambió la versión en código a `2026.08.26.8` (`ApplicationDisplayVersion`, `AndroidAutoUpdater.CurrentVersion`, `Localization`) y se recompiló en CI. La `.8` agrupa: (a) audio binaural completo idéntico a Windows — `AndroidSoundEngine` decodifica cada OGG de SFX a PCM mono con `MediaExtractor`+`MediaCodec`, aplica el `GridSpatializer` exacto (paneo equal-power + aire/low-pass por profundidad de fila + anchura estéreo por profundidad) y reproduce por `AudioTrack`, con fallback al modelo equal-power+profundidad vía SoundPool si la decodificación PCM falla en el dispositivo; (b) bienvenida solo al iniciar la app; (c) TalkBack: inmersivo sticky, exploración con 1 dedo (`DispatchHoverEvent`), etiqueta concisa (`A1, Roja`), acciones de nodo direccionales y celdas correctas en apaisado; (d) sin doble swap accidental y barridos dinámicos en la Escuela de Audio; (e) módulo MO3 real vía libopenmpt→AudioTrack (APK de pocos MB), textos de interfaz coherentes con táctil, optimización de batería y `README.html` solo táctil. Las voces quedan centradas y secas. Bumps en `csproj`, `AndroidAutoUpdater.CurrentVersion`, `Localization` y `README.html`. La rama Android queda definitivamente cerrada y al día en `android-v2026.08.26.8`.

---

## 🏆 7. Estado del Proyecto y Releases

- **Windows (`main`)**: Release `v2026.08.25.1` (hotfix: guarda de instancia única) con soporte completo de teclado, ratón hablado, audio binaural 3D, suite de 145 tests en verde y auto-actualizador multiplataforma (filtra tags `android-*`). Marcado como **Latest**.
    - **Android (`android`)**: Release `android-v2026.08.26.8` (hotfix consolidado: módulo MO3 real en el dispositivo, APK mucho más ligero, textos de interfaz coherentes con táctil, optimización de batería, TalkBack correcto en apaisado, sin doble swap accidental, movimiento accesible real con TalkBack, lectura limpia de cada gema con 1 dedo, "Welcome to Bejeweled" solo al iniciar, audio espacial tipo Windows y binaural completo vía PCM) con TalkBack 100% nativo (el tacto siempre activo), árbol de accesibilidad virtual de 64 nodos, reproductor de módulo `libopenmpt`→`AudioTrack` con el `.so` compilado en CI vía NDK (minimp3+stb_vorbis internos, sin dependencias externas) y los 29 MP3 eliminados del APK (baja de ~184 MB a pocos MB), auto-actualizador de APK que busca su propio tag `android-v…`, y APK firmado con keystore estable (actualización en sitio). Única release de Android: agrupa todo el trabajo de la rama, incluido el binaural completo de Windows (AndroidSoundEngine decodifica cada OGG de SFX a PCM mono con MediaExtractor+MediaCodec, aplica el GridSpatializer exacto — aire/low-pass por profundidad + anchura estéreo — y reproduce por AudioTrack, con fallback al modelo equal-power+profundidad vía SoundPool si la decodificación PCM falla en el dispositivo). Las voces quedan centradas y secas.
- **Cómo distinguir al distribuir**: tag `v…` + asset `.zip` = Windows; tag `android-v…` + asset `.apk` = Android. El auto-actualizador de cada plataforma entrega el correcto sin que el usuario elija.
- **Flujo de release:** bump en `AssemblyInfo.cs`, `Localization.cs` (LoadingTitle/AppTitle) y `README.html` (versión + changelog ES/EN); en Windows build Debug+Release + suite completa (145/145) y zip con exe/PDB Release + `bass.dll` + `nvdaControllerClient32.dll` + 5 `libopenmpt*.dll` + `mscorlib.dll` + `norm*.nlp` + `es\` + `README.html` + `audio.pac` (generado por `--pack-audio`) + `sounds\images\` completa; en Android el APK se compila en GitHub Actions (ver anecdotario 7); `gh release create` + `gh release upload`; limpiar `Temp\opencode`.
