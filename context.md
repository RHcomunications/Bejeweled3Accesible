# 💎 Bejeweled 3 Accesible - Informe Técnico, Arquitectura y Anecdotario

**Proyecto:** Bejeweled 3 Accesible (Clon Fiel y Accesible de Bejeweled 3 para Jugadores Ciegos y con Baja Visión)  
**Repositorio:** `RHcomunications/Bejeweled3Accesible`  
**Versión Actual:** Windows: `v2026.08.24.3` | Android: `android-v2026.08.24.8`  
**Tecnología Base:** 
- **Windows (`main`):** C# (.NET Framework 4.5), Windows Forms, BASS Audio Engine (P/Invoke nativo), libopenmpt (decodificador de módulos .mo3), SAPI 5 / NVDA Controller Client.
- **Android (`android`):** C# (.NET 9 Android / MAUI), Android Accessibility Framework (`AccessibilityManager`, `AnnounceForAccessibility`), `SoundPool` para efectos de ultra baja latencia y `MediaPlayer` para la banda sonora original completa en MP3.

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
│   │   │   └── AndroidSoundEngine.cs # Motor dual SoundPool (baja latencia) + MediaPlayer (OST continua)
│   │   ├── UI/
│   │   │   └── GameScreenView.cs  # Vista gráfica interactiva, gestos, modo apaisado, pista y pausa
│   │   ├── MainActivity.cs        # Actividad principal, bloqueo de orientación SensorLandscape
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
5. **Soporte Completo de Ratón y Flechas en Windows (v2026.08.24.3)**:
   - *Desafío:* Usuarios con baja visión o educadores querían interactuar con ratón sin perder la verbalización de casillas y opciones de movimiento.
   - *Solución:* Se implementó eco de ratón hablado, cálculo de direcciones válidas en tiempo real y soporte completo de flechas direccionales en el tablero.

---

## 🏆 7. Estado del Proyecto y Releases

- **Windows (`main`)**: Release `v2026.08.24.3` con soporte completo de teclado, ratón hablado, audio binaural y auto-actualizador.
- **Android (`android`)**: Release `android-v2026.08.24.12` (**Release Completa con Actualizador Integrado y Paridad 1:1**) con TalkBack nativo, árbol de accesibilidad virtual de 64 nodos, actualizador automático de APK desde GitHub Releases, Escuela de Audio, Opciones Zen, Modo Misión con Relicarios y Desafíos, diálogos accesibles nativos de perfil y físicas completas de los 8 modos (Póker, Tormenta de Hielo, Mariposas y Mina de Diamantes).
- **Flujo de release:** bump en `AssemblyInfo.cs`, `Localization.cs` (LoadingTitle/AppTitle) y `README.html` (versión + changelog ES/EN); build Debug+Release; suite completa con audio; zip con exe/PDB Release + `bass.dll` + `nvdaControllerClient32.dll` + 5 `libopenmpt*.dll` + `mscorlib.dll` + `norm*.nlp` + `es\` + `README.html` + `audio.pac` (196 entradas) + `sounds\images\` completa; `gh release create` + upload; limpiar archivos temporales locales.
