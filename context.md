# 💎 Bejeweled 3 Accesible - Informe Técnico, Arquitectura y Anecdotario

**Proyecto:** Bejeweled 3 Accesible (Clon Fiel y Accesible de Bejeweled 3 para Jugadores Ciegos y con Baja Visión)  
**Repositorio:** `RHcomunications/Bejeweled3Accesible`  
**Versión Actual:** `v2026.08.23.0`  
**Tecnología Base:** C# (.NET Framework 4.5), Windows Forms, BASS Audio Engine (P/Invoke nativo), libopenmpt (decodificador de módulos .mo3), SAPI 5 / NVDA Controller Client.

---

## 📖 1. Visión y Propósito del Proyecto

El objetivo primordial de **Bejeweled 3 Accesible** fue rescatar y recrear con exactitud matemática, sonora y de diseño la experiencia del clásico juego de PopCap Games (**Bejeweled 3**), haciéndolo **100% jugable sin visión** a través de sintetizadores de voz (NVDA y SAPI5) y un sistema de **Audio Espacial de Tablero** (modelo grid único y siempre activo: paneo por columna + profundidad por fila), sin sacrificar una interfaz visual limpia y nítida. La **música se trata como clima, atmósfera o ambiente de acompañamiento**: suena centrada, seca y envolvente, nunca posicionada en el tablero.

---

## 🏛️ 2. Arquitectura del Sistema

El proyecto se estructura en capas desacopladas y altamente especializadas:

```
bejeweled3_accessible/
├── src/
│   ├── Accessibility/
│   │   └── NvdaSpeech.cs          # Interfaz bidireccional NVDA Controller / SAPI 5 fallback
│   ├── Audio/
│   │   ├── SoundEngine.cs         # Motor BASS, colas de voz atómicas, ducking, rutas espaciales (modelo grid siempre activo, sin perfiles)
│   │   ├── SpatialAudio.cs        # Modelo grid espacial estático: PanColumn, DepthForRow, Volume/Air/Width por profundidad
│   │   ├── GridSpatializer.cs     # Render grid: pan equal-power + aire/profundidad por one-pole LP (sustituye BinauralRenderer)
│   │   ├── AudioMap.cs            # Mapa canónico tipado de efectos de sonido (cero strings crudos)
│   │   ├── MusicMap.cs            # Mapa canónico de las pistas musicales originales (suite .mo3 + ambientales)
│   │   ├── PacCipher.cs           # Cifrado XOR / obfuscación del contenedor audio.pac
│   │   ├── PacPacker.cs           # Herramienta de empaquetado seguro en RAM
│   │   └── PacReader.cs           # Lector directo en memoria de archivos PAC
│   ├── Engine/
│   │   ├── Board.cs               # Cuadrícula 8x8, físicas de gravedad, cascadas y gemas especiales
│   │   ├── Gem.cs                 # Definición de gemas, formas geométricas canónicas y estados
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
│   │   └── MainWindow.cs          # Bucle de juego, renderizado GDI+, navegación y teclado
│   └── Update/
│       └── AutoUpdater.cs         # Sistema de auto-actualización vía GitHub Releases API
    └── tests/
        └── TestRunner.cs              # Suite de 144 pruebas unitarias integradas
```

---

## 🎮 3. Mecánicas y Modos de Juego Replicados

### Modos Principales
1. **Modo Clásico (Classic):** Partida infinita por puntos con barra de progreso de nivel. Transición espacial *"Level Complete"* con túnel de luz (warp sound). El juego solo concluye cuando no existen movimientos válidos (*"No More Moves"*).
2. **Modo Relámpago (Lightning):** Partida contrarreloj de 60 segundos base, donde crear gemas de tiempo (+5s, +10s) y encadenar combos rápidos otorga tiempo extra continuo.
3. **Modo Zen:** Experiencia relajante sin fin ni peligro de Game Over. Cuenta con 6 ambientes naturales (*Coastal, Crickets, Forest, Ocean Surf, Rain Leaves, Waterfall*), modulación de respiración de 4 fases (*Inhalar, Sostener, Exhalar, Sostener*) y banco oficial de mantras/afirmaciones.
4. **Modo Búsqueda (Quest):** Restauración de los 5 Relicarios sagrados a través de 40 desafíos únicos (Alquimia, Bombas, Fiebre de Oro, Mariposas, Tormenta de Hielo, Poker, etc.).

### Modos Secretos Desbloqueables
- **Póker:** Desbloqueado al nivel 5 de Clásico. Cada combinación añade una carta a la mano; calaveras bloquean manos aleatorias.
- **Mariposas:** Desbloqueado al nivel 5 de Zen. Las mariposas suben 1 fila por cada turno jugado hacia la araña superior.
- **Tormenta de Hielo:** Desbloqueado al superar 100,000 puntos en Relámpago. Columnas de hielo que ascienden y deben quebrarse antes de congelar el tablero.
- **Mina de Diamantes:** Desbloqueado al completar 4 misiones del Relicario 1 en Quest. Filas inferiores de tierra blanda y roca dura con excavación física (+10m de profundidad) y rescate de pepitas de oro.

---

## 🎧 4. Audio Espacial de Tablero (modelo grid único, siempre activo)

- **Principio rector — la música es clima / atmósfera / ambiente:** la música (módulo real `.mo3` y ambientales de Zen) se trata como **clima, atmósfera o acompañamiento ambiental del juego**, nunca como un objeto posicionado. Suena **centrada, seca y envolvente** (sin paneo de columna ni profundidad de fila). Las locuciones (voz del juego y lector de pantalla) también van **siempre centradas**, con *ducking* automático de la música mientras hablan. El motor espacial `GridSpatializer` solo procesa los efectos de tablero.
- **Por qué se abandonó el HRTF/Atmos (v2026.08.22.5):** el "HRTF" previo era en realidad ITD + ILD + aire; la ITD quedaba en sub-muestra (~1 muestra a 44,1 kHz, inaudible) y la única pista real era ILD (paneo L/R) + distancia/aire. Activar el perfil Atmos "no se notaba" porque todos los perfiles aplicaban el mismo HRTF. Se decidió borrar binaural/perfiles y usar un modelo generado y específico para este juego.
- **Modelo grid (siempre activo, sin perfiles):** cada efecto se coloca con dos parámetros —**Pan por columna** (A-H reparte izquierda↔derecha en paneo equal-power, `SpatialAudio.PanColumn`, `MaxPan=0.85`) y **Profundidad por fila** (frente=fila 7 → 0, fondo=fila 0 → 1; `SpatialAudio.DepthForRow`). El *swipe* anima el pan de una columna a otra (`ScheduleSfxSweep` + `PanSweepTick`, con `SweepPan`/`EaseSweep`). No hay opciones ni menús de perfil.
- **Profundidad = lejanía (volumen + aire + anchura):** las filas traseras suenan más lejanas vía **menor volumen** (`VolumeForDepth` 1,0→0,65), **aire más cerrado** (`AirCutoffForDepth` 20 kHz→~6 kHz, paso-bajo de un polo en C#) y **estéreo algo más amplio** (`WidthForDepth` 1,0→1,3, mid/side). El timbre y el tono de los 189 efectos reales **jamás cambian**.
- **Render `GridSpatializer` (DSP en el stream directo):** la `bass.dll` reducida no decodifica `BASS_STREAM_DECODE` ni resamplea con `BASS_ATTRIB_FREQ`, así que el OGG se reproduce por el camino directo (stream FLOAT a su tasa nativa, 44,1 o 22,05 kHz) y un `BASS_ChannelSetDSP` sustituye el buffer estéreo por la salida del `GridSpatializer` (pan equal-power * volumen/aire/anchura), configurado con la tasa real del fichero (`BASS_ChannelGetInfo`). El downmix a mono aprovecha que los 189 efectos son estéreo *dual-mono* (L==R).
- **Escuela de Audio (corta, 12 pruebas):** desde el menú principal un mini menú reproduce columnas A-H (izquierda↔derecha), frente, fondo, barrido L→R y barrido frente→fondo, usando `SoundEngine.PlaySoundSpatialPan` / `PlaySoundSpatialSweepPan`. Al pulsar Enter se repite la opción para confirmar la llamada. Verifica la configuración de auriculares sobre el propio tablero.
- **Locuciones centradas con Ducking:** la voz del locutor (*Good, Excellent, Awesome, Spectacular, Extraordinary, Unbelievable*) y el lector de pantalla se mantienen siempre centrados mientras la música baja automáticamente de volumen (*ducking*).

---

## 🎼 5. Audio y Música Reales del Juego Original (v2026.08.18.0)

- **189 efectos extraídos de `main.pak`** (vorbis 96-128 kb/s, byte-idénticos, sin remasterizar) sustituyen a los descargados; se reproducen desde el contenedor cifrado `audio.pac` (196 entradas, de 178 MB a 9 MB).
- **Música real:** el módulo `Bejeweled3_suite.mo3` (62 minutos, extraído del propio juego) se decodifica con libopenmpt (BSD-3) y se reproduce en cadena saltando por los offsets del `music.xml` original, exactamente como el juego original (el intro avanza solo al menú a los 24 segundos). Se reproduce **centrada y seca, como clima/atmósfera de acompañamiento** (nunca posicionada en el tablero); véase el principio rector en §4.
- **6 ambientaciones reales** (`ambient\*.ogg` del juego) completan el modo Zen.
- **Infraestructura en `src\Audio`:** `MusicMap.cs` (offsets), `ModuleMusicPlayer.cs` (libopenmpt vía openmpt module API con `--extended` y `StreamCreate` STREAMPROC), `PacCipher/Packer/Reader` (cifrado XOR con clave `"Bejeweled3AccessibleProtectionKey2026"`), `bass.dll` reducida (sin `BASS_StreamCreatePush` ni `BASS_LastError`).
- **Fallbacks:** módulo y ambientales cargan del PAC y, si falta, de `bin\music\`; los `libopenmpt*.dll` (5 ficheros) son obligatorios junto al exe.

---

## 💎 6. Formas Geométricas Auténticas y Verbalización

Las gemas incorporan las formas geométricas originales de PopCap y adaptan dinámicamente sus modificadores:

| Color | Gema Original | Forma Base | Modificador Especial (Ejemplo) |
| :--- | :--- | :--- | :--- |
| **Rojo** | Rubí | *Cuadrado rojo* | *Cuadrado rojo de fuego* / *Flame red square* |
| **Amarillo** | Topacio | *Triángulo amarillo* | *Triángulo amarillo de fuego* |
| **Verde** | Esmeralda | *Hexágono verde* | *Hexágono verde estela* |
| **Azul** | Zafiro | *Diamante azul* | *Diamante azul supernova* |
| **Púrpura** | Amatista | *Octágono púrpura* | *Octágono púrpura mariposa* |
| **Blanco** | Diamante | *Gema circular blanca* | *Gema circular blanca bomba en 10 turnos* |
| **Naranja** | Ámbar | *Rombo naranja* | *Rombo naranja más 5 segundos* |
| **Especial** | Hipercubo | *Hipercubo* | *Hipercubo* |

---

## 📝 7. Anecdotario Técnico: Desafíos y Soluciones

1. **El "Falso Conflicto" de Candy Crush:**
   - *Anécdota:* Al desarrollar casi simultáneamente un proyecto de *Sugar Crush* y este clon de *Bejeweled 3*, existió la duda de si se habían mezclado mecánicas (como gelatinas o peces).
   - *Resolución:* La auditoría demostró que la arquitectura de *Bejeweled 3* siempre se mantuvo 100% aislada, pura y fiel a los estándares de PopCap.
2. **El Problema de "Pesetas" vs "Puntos":**
   - *Anécdota:* Sintetizadores de voz en español como Helena (SAPI5) o ciertas voces de NVDA leían `"pts"` como *"pesetas"*.
   - *Resolución:* Se purgó cualquier abreviatura en los archivos de localización, sustituyendo explícitamente por `"puntos"` y `"points"`.
3. **El Parpadeo de Respiración en Modo Zen:**
   - *Anécdota:* Al desactivar y reactivar la modulación de respiración, el temporizador reiniciaba toda la sesión de audio deteniendo la música.
   - *Resolución:* Se desacopló el ciclo de temporizadores con el nuevo método `UpdateZenSessionState()`, permitiendo encender y apagar la respiración en caliente sin afectar la pista musical activa.
4. **El HRTF que "detonaba" los sonidos reales:**
   - *Anécdota:* El HRTF antiguo bajaba el tono de los efectos según la profundidad (hasta 0,965×), lo que con los sonidos reales de PopCap se percibía como una detonación artificial.
   - *Resolución:* Se eliminó por completo el pitch por profundidad (`DepthPitch`); la profundidad ahora se expresa solo como volumen y absorción de aire, dejando el tono de los sonidos intacto (v2026.08.18.1).
5. **El Descubrimiento del "Dual-Mono":**
   - *Anécdota:* Antes de reescribir el HRTF se auditaron los 189 efectos reales con BASS: todos los estéreo (183) tienen los canales izquierdo y derecho idénticos (spread 0.0) y solo 6 son mono nativo.
   - *Resolución:* Ese hallazgo garantiza que el downmix a mono y el render binaural posterior no pierden absolutamente nada de la mezcla de PopCap: los 189 efectos se espacializan con fidelidad total.
6. **El 404 del Auto-Actualizador:**
   - *Anécdota:* Una jugadora recibía 404 al actualizar; el zip se había subido como `Bejeweled3Accessible-v2026.08.18.0.zip` (con "v", doble "s" en "Accessible" y versión con ceros).
   - *Resolución:* El updater construye la URL como `Bejeweled3Accesible-<versión-sin-ceros>.zip` (`AutoUpdater.BuildZipAssetName`), así que todo zip de release debe llamarse exactamente así (una sola "s" en "Accesible", sin "v", sin ceros de relleno).
7. **La bass.dll que "no decodificaba":**
   - *Anécdota:* Con el HRTF nuevo (v2026.08.18.1), el select y los combos dejaron de oírse con el audio binaural activado. La ruta binaural "decodificaba → renderizaba → empujaba" con streams `BASS_STREAM_DECODE`, y en esa bass.dll reducida `BASS_ChannelGetData` devuelve 0 muestras siempre (sin error): el canal de salida recibía silencio y solo se notaba en los sonidos importantes (select y combos).
   - *Resolución:* Se reescribió la ruta para reproducir el OGG por el camino directo (stream FLOAT a tasa nativa) e instalar un `BASS_ChannelSetDSP` que sustituye el buffer estéreo por la salida del renderer; el renderer usa la tasa real del fichero (44.1 o 22.05 kHz). Verificado con el modo `--decode-probe` de la suite: duraciones reales (select ~23 ms, combo_1 ~836 ms, tick ~43 ms), RMS > 0 y asimetría L/R correcta a azimut 60°. Suite 154/154 en verde.
8. **Los sonidos "opacos":**
   - *Anécdota:* Con el HRTF sonando de nuevo, algunos efectos (sobre todo combos y sonidos de los bordes y del fondo) se percibían apagados y "nada reales". El modelo de v2026.08.18.1 filtraba el timbre: sombra de cabeza como paso-bajo de hasta 2,7 kHz en el oído lejano y absorción de aire como paso-bajo de hasta 3,5 kHz en AMBOS oídos al fondo. La v2026.08.19.0 eliminó los pasos-bajo pero dejó un estante de sombra que aún restaba ~2,5 dB de agudos en el oído lejano: el usuario seguía percibiendo opacidad.
    - *Resolución (v2026.08.19.1):* El renderer se reescribió DESDE CERO con el **principio Dolby de sonido orientado a objetos**: cero procesamiento espectral. Cada efecto es un objeto que viaja con su señal intacta; la posición usa SOLO retardo interaural (ITD) + ganancia (ILD del oído lejano, máx. ~5,3 dB) y la distancia es SOLO volumen. La métrica de brillo de la probe (`brillo`, energía por encima de 3 kHz) confirma 100 % de los agudos originales en ambos oídos (antes 80-100 %). Suite 154/154 en verde.

9. **Audio 3D por objetos (paradigma Dolby Atmos):**
   - *Anécdota:* Tras el audio binaural puro, el usuario pidió un modelo de **objeto espacial 3D** estilo Dolby Atmos: cada sonido como objeto con posición (X,Y,Z), velocidad, elevación y flag volumétrico, con **absorción de aire real** (paso-bajo que baja con la distancia) y tilt de elevación, más una "Escuela de Audio" para calibrar auriculares.
   - *Resolución:* Se añadieron `SpatialAudioObject`/`SpatialAudioListener`/`SpatialAudioEngine` (`src/Audio/SpatialAudioObject.cs`) y helpers 3D en `SpatialAudio.cs` (mapeo de celda→mundo, `AirAbsorptionCutoffHz` 20 kHz@14 m→1,2 kHz@50 m, `DistanceGainFor` con radios volumétricos, `ElevationTiltDb`, `AzimuthFromRelative`). El `BinauralRenderer` ganó `DistanceGain`, `ElevationTiltDb` y `AirCutoffHz` (paso-bajo de un polo en C#, bypass cuando es 0, así los perfiles 2D siguen transparentes). `SoundEngine` registra cada efecto como objeto 3D en el perfil Atmos 3D y un timer de ~60 FPS refresca la pose. La "Escuela de Audio" (`GameScreen.AudioSchool`) reproduce pruebas ancladas al tablero: por columna (azimut), por fila (profundidad), por altura y barrido/aire. Suite 158/158 (se añadieron tests de aire/volumétrico/elevación/tilt), Debug y Release 0 errores.

---

## 🏆 8. Estado Final del Proyecto

- **Compilación:** 0 Errores en configuraciones Debug y Release.
- **Pruebas Automatizadas:** 158 tests de cobertura unitaria con validación en memoria, físicas de tablero, persistencia, accesibilidad y audio (incluidos los binaurales ITD/ILD/aire y el renderer estéreo 3D por objetos).
- **Git & Releases:** Tags `v2026.08.17.0`, `v2026.08.17.1`, `v2026.08.18.0` (audio y música reales), `v2026.08.18.1` (HRTF binaural paramétrico), `v2026.08.18.2` (hotfix: ruta binaural por DSP en el stream directo, select y combos audibles de nuevo), `v2026.08.19.0` (audio espacial de objeto estilo Dolby: sin pasos-bajo) `v2026.08.19.1` (hotfix: objeto puro, cero filtrado espectral), `v2026.08.20.0` (audio espacial 3D por objetos Atmos + Escuela de Audio), `v2026.08.22.0` (versión mayor: corregido Atmos para que se note — flag `SpatialPose`, demo de aire audible) y `v2026.08.22.1` (Escuela de Audio audible: columnas en fila frontal cono ±60°, altura con tilt exagerado, feedback hablado; música atmosférica en perfil Atmos) publicados en GitHub con auto-actualizador integrado. El asset de cada release debe nombrarse `Bejeweled3Accesible-<versión sin ceros>.zip` (ver anécdota 6).
- **Flujo de release:** bump en `AssemblyInfo.cs`, `Localization.cs` (LoadingTitle/AppTitle) y `README.html` (versión + changelog ES/EN); build Debug+Release; suite completa con audio; zip con exe/PDB Release + `bass.dll` + `nvdaControllerClient32.dll` + 5 `libopenmpt*.dll` + `mscorlib.dll` + `norm*.nlp` + `es\` + `README.html` + `audio.pac` (196 entradas) + `sounds\images\` completa; `gh release create` + upload; limpiar `Temp\opencode` (conservando `extracted\`, `qbms\`, `bms\`, `libopenmpt\` como fuentes canónicas).
