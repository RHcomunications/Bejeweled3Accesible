# 💎 Bejeweled 3 Accesible - Informe Técnico, Arquitectura y Anecdotario

**Proyecto:** Bejeweled 3 Accesible (Clon Fiel y Accesible de Bejeweled 3 para Jugadores Ciegos y con Baja Visión)  
**Repositorio:** `RHcomunications/Bejeweled3Accesible`  
**Versión Actual:** `v2026.08.19.1`  
**Tecnología Base:** C# (.NET Framework 4.5), Windows Forms, BASS Audio Engine (P/Invoke nativo), libopenmpt (decodificador de módulos .mo3), SAPI 5 / NVDA Controller Client.

---

## 📖 1. Visión y Propósito del Proyecto

El objetivo primordial de **Bejeweled 3 Accesible** fue rescatar y recrear con exactitud matemática, sonora y de diseño la experiencia del clásico juego de PopCap Games (**Bejeweled 3**), haciéndolo **100% jugable sin visión** a través de sintetizadores de voz (NVDA y SAPI5) y un revolucionario sistema de **Audio Espacial 3D Binaural (HRTF)**, sin sacrificar una interfaz visual limpia y nítida.

---

## 🏛️ 2. Arquitectura del Sistema

El proyecto se estructura en capas desacopladas y altamente especializadas:

```
bejeweled3_accessible/
├── src/
│   ├── Accessibility/
│   │   └── NvdaSpeech.cs          # Interfaz bidireccional NVDA Controller / SAPI 5 fallback
│   ├── Audio/
│   │   ├── SoundEngine.cs         # Motor BASS, colas de voz atómicas, ducking, rutas espaciales (binaural y pan)
│   │   ├── SpatialAudio.cs        # HRTF binaural paramétrico: azimut ±75°, ITD, ILD, sombra de cabeza, absorción de aire
│   │   ├── BinauralRenderer.cs    # Render binaural por oído: delay fraccional (ITD) + one-pole LP por oído
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
    └── TestRunner.cs              # Suite de 154 pruebas unitarias integradas
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

## 🎧 4. Innovación: Audio Espacial 3D (HRTF Binaural)

- **Azimut Real por Columna (±75°):** Cada columna del tablero (A-H) se oye como un azimut en el plano horizontal, calculado con el **principio Dolby de sonido orientado a objetos** (v2026.08.19.1): el objeto viaja con su señal 100 % intacta — el renderer NUNCA procesa el espectro (ni pasos-bajo ni estantes) — y la posición usa solo retardo interaural de Woodworth `(a/c)·(sinθ+θ)` (hasta ~0,58 ms a ±75°) y ganancia ILD del oído lejano (hasta ~5,3 dB). El oído cercano suena idéntico a la muestra original.
- **Profundidad por Fila como Distancia:** Las filas del fondo suenan más lejanas (**solo volumen**: 0,80 lejos .. 1,00 cerca, como la distancia real); las del frente plenas y cercanas. **El timbre y el tono de los sonidos jamás cambian** por profundidad ni lateralidad: se eliminó cualquier filtrado (la sombra de cabeza de la 19.0 aún restaba ~2,5 dB de agudos y los sonidos seguían "opacos"; en la 19.1 es cero filtrado).
- **Fidelidad Total a la Mezcla de PopCap:** Los 189 efectos reales son estéreo *dual-mono* (L==R), así que el render binaural los espacializa sin perder nada. La música del módulo real y las ambientales suenan **centradas, secas y sin procesar**; la reverberación DX8 que se añadía a la música fue retirada.
- **Deslizamientos y Cascadas:** Los intercambios animan el azimut de la gema de una columna a otra (con realce de presencia solo en el perfil Escenario 2D, sin doppler de tono).
- **Ruta de reproducción (DSP en el stream directo):** la `bass.dll` reducida no decodifica streams `BASS_STREAM_DECODE` (devuelve 0 muestras) ni resamplea vía `BASS_ATTRIB_FREQ`, así que la ruta binaural no puede usar "decodificar → renderizar → push". En su lugar, el OGG se reproduce por el camino directo que sí funciona (stream FLOAT a su tasa nativa) y un `BASS_ChannelSetDSP` sustituye el buffer estéreo por la salida del renderer; el renderer se configura con la tasa real del fichero (`BASS_ChannelGetInfo`), correcta tanto a 44.1 kHz como en los 6 OGG reales a 22.05 kHz (misma duración y tono originales, ITD exacto).
- **Perfiles Espaciales:** *Escenario 2D* (binaural completo con profundidad y realce), *Clásico Limpio* (por defecto: binaural lateral puro y seco), *Simple* (pan clásico izquierda/derecha sin HRTF) y **Objeto 3D (Atmos)** (paradigma Dolby Atmos completo, ver abajo). Se recuerdan entre sesiones.
- **Audio Espacial 3D por Objetos (perfil Atmos 3D):** Cada efecto posicionado es un `SpatialAudioObject` (`src/Audio/SpatialAudioObject.cs`) con posición `Vector3` (X lateral, Y altura, Z profundidad, en metros), `Velocity`, `AngleSpreadDeg`, `IsVolumetric` y radios `MinDistance`/`MaxDistance`. El `SpatialAudioEngine` (singleton, timer de ~60 FPS en `SoundEngine`) recalcula cada frame la pose relativa al `SpatialAudioListener` y escribe en el `BinauralRenderer`: azimut (`AzimuthFromRelative`), ganancia por distancia (`DistanceGainFor`, lineal con radios mayores para fuentes volumétricas), **absorción de aire real** (`AirAbsorptionCutoffHz`: paso-bajo de un polo en C# cuyo corte es 20 kHz por debajo de 14 m y baja exponencialmente a ~1,2 kHz a 50 m; el renderer lo aplica bilateralmente solo cuando `AirCutoffHz>0`, así los perfiles 2D quedan transparentes) y **tilt de elevación** (`ElevationTiltDb`: atenuación sutil ±4 dB por diferencia de altura). El tablero se mapea a un mundo donde las distancias de juego quedan < 14 m (nítido); la absorción solo se oye en fuentes lejanas o en la calibración.
- **Escuela de Audio (calibración):** Desde el menú principal (`GameScreen.AudioSchool`) un mini menú reproduce pruebas por carril (A-H al frente), por altura (suelo/gema/aérea en el centro) y por dirección (frente/detrás/izquierda/derecha/diagonal y lejos con aire) usando `SoundEngine.PlaySoundSpatialElevated` / `PlaySoundAtWorld`, que fuerzan el camino de objeto 3D. Verifica la configuración de auriculares.
- **Locuciones Centradas con Ducking:** La voz del locutor (*Good, Excellent, Awesome, Spectacular, Extraordinary, Unbelievable*) y el lector de pantalla se mantienen siempre centrados mientras la música baja automáticamente de volumen (*ducking*).

---

## 🎼 5. Audio y Música Reales del Juego Original (v2026.08.18.0)

- **189 efectos extraídos de `main.pak`** (vorbis 96-128 kb/s, byte-idénticos, sin remasterizar) sustituyen a los descargados; se reproducen desde el contenedor cifrado `audio.pac` (196 entradas, de 178 MB a 9 MB).
- **Música real:** el módulo `Bejeweled3_suite.mo3` (62 minutos, extraído del propio juego) se decodifica con libopenmpt (BSD-3) y se reproduce en cadena saltando por los offsets del `music.xml` original, exactamente como el juego original (el intro avanza solo al menú a los 24 segundos).
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
   - *Resolución:* Se añadieron `SpatialAudioObject`/`SpatialAudioListener`/`SpatialAudioEngine` (`src/Audio/SpatialAudioObject.cs`) y helpers 3D en `SpatialAudio.cs` (mapeo de celda→mundo, `AirAbsorptionCutoffHz` 20 kHz@14 m→1,2 kHz@50 m, `DistanceGainFor` con radios volumétricos, `ElevationTiltDb`, `AzimuthFromRelative`). El `BinauralRenderer` ganó `DistanceGain`, `ElevationTiltDb` y `AirCutoffHz` (paso-bajo de un polo en C#, bypass cuando es 0, así los perfiles 2D siguen transparentes). `SoundEngine` registra cada efecto como objeto 3D en el perfil Atmos 3D y un timer de ~60 FPS refresca la pose. La "Escuela de Audio" (`GameScreen.AudioSchool`) reproduce pruebas por carril/altura/dirección. Suite 158/158 (se añadieron tests de aire/volumétrico/elevación/tilt), Debug y Release 0 errores.

---

## 🏆 8. Estado Final del Proyecto

- **Compilación:** 0 Errores en configuraciones Debug y Release.
- **Pruebas Automatizadas:** 158 tests de cobertura unitaria con validación en memoria, físicas de tablero, persistencia, accesibilidad y audio (incluidos los binaurales ITD/ILD/aire y el renderer estéreo 3D por objetos).
- **Git & Releases:** Tags `v2026.08.17.0`, `v2026.08.17.1`, `v2026.08.18.0` (audio y música reales), `v2026.08.18.1` (HRTF binaural paramétrico), `v2026.08.18.2` (hotfix: ruta binaural por DSP en el stream directo, select y combos audibles de nuevo), `v2026.08.19.0` (audio espacial de objeto estilo Dolby: sin pasos-bajo) y `v2026.08.19.1` (hotfix: objeto puro, cero filtrado espectral) publicados en GitHub con auto-actualizador integrado. El asset de cada release debe nombrarse `Bejeweled3Accesible-<versión sin ceros>.zip` (ver anécdota 6).
- **Flujo de release:** bump en `AssemblyInfo.cs`, `Localization.cs` (LoadingTitle/AppTitle) y `README.html` (versión + changelog ES/EN); build Debug+Release; suite completa con audio; zip con exe/PDB Release + `bass.dll` + `nvdaControllerClient32.dll` + 5 `libopenmpt*.dll` + `mscorlib.dll` + `norm*.nlp` + `es\` + `README.html` + `audio.pac` (196 entradas) + `sounds\images\` completa; `gh release create` + upload; limpiar `Temp\opencode` (conservando `extracted\`, `qbms\`, `bms\`, `libopenmpt\` como fuentes canónicas).
