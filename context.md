# 💎 Bejeweled 3 Accesible - Informe Técnico, Arquitectura y Anecdotario

**Proyecto:** Bejeweled 3 Accesible (Clon Fiel y Accesible de Bejeweled 3 para Jugadores Ciegos y con Baja Visión)  
**Repositorio:** `RHcomunications/Bejeweled3Accesible`  
**Versión Actual:** `v2026.08.16.1`  
**Tecnología Base:** C# (.NET Framework 4.5), Windows Forms, BASS Audio Engine (P/Invoke nativo), SAPI 5 / NVDA Controller Client.

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
│   │   ├── SoundEngine.cs         # Motor BASS, colas de voz atómicas, ducking, reverb DX8
│   │   ├── SpatialAudio.cs        # Curvas matemáticas HRTF (-0.85 a +0.85), profundidad y sweep
│   │   ├── AudioMap.cs            # Mapa canónico tipado de efectos de sonido (cero strings crudos)
│   │   ├── MusicMap.cs            # Mapa canónico de las 29 pistas musicales originales
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
    └── TestRunner.cs              # Suite de 141 pruebas unitarias integradas
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

- **Posicionamiento Horizontal (-0.85 a +0.85):** Cada una de las 8 columnas del tablero (A-H) está mapeada en el espectro estéreo para auriculares. Los intercambios y cascadas barren el espacio sonoro mediante curvas de paneo continuo (`PlaySoundSpatialSweep`).
- **Profundidad Vertical:** Las filas superiores e inferiores modulan sutilmente volumen y filtrado de agudos para dar sensación de altura y lejanía.
- **Reverb Binaural DX8 en Música:** La banda sonora cuenta con un procesador DSP de reverberación envolvente que dota a la música de una atmósfera profunda y relajante.
- **Locuciones Centradas con Ducking:** La voz del locutor (*Good, Excellent, Awesome, Spectacular, Extraordinary, Unbelievable*) y el lector de pantalla se mantienen siempre centrados (mono/centro) mientras la música baja automáticamente de volumen (*ducking*) para garantizar la máxima comprensión auditiva.

---

## 💎 5. Formas Geométricas Auténticas y Verbalización

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

## 📝 6. Anecdotario Técnico: Desafíos y Soluciones

1. **El "Falso Conflicto" de Candy Crush:**
   - *Anécdota:* Al desarrollar casi simultáneamente un proyecto de *Sugar Crush* y este clon de *Bejeweled 3*, existió la duda de si se habían mezclado mecánicas (como gelatinas o peces).
   - *Resolución:* La auditoría demostró que la arquitectura de *Bejeweled 3* siempre se mantuvo 100% aislada, pura y fiel a los estándares de PopCap.
2. **El Problema de "Pesetas" vs "Puntos":**
   - *Anécdota:* Sintetizadores de voz en español como Helena (SAPI5) o ciertas voces de NVDA leían `"pts"` como *"pesetas"*.
   - *Resolución:* Se purgó cualquier abreviatura en los archivos de localización, sustituyendo explícitamente por `"puntos"` y `"points"`.
3. **El Parpadeo de Respiración en Modo Zen:**
   - *Anécdota:* Al desactivar y reactivar la modulación de respiración, el temporizador reiniciaba toda la sesión de audio deteniendo la música.
   - *Resolución:* Se desacopló el ciclo de temporizadores con el nuevo método `UpdateZenSessionState()`, permitiendo encender y apagar la respiración en caliente sin afectar la pista musical activa.
4. **Activación Instantánea del Reverb Binaural:**
   - *Anécdota:* Cambiar la opción de Audio Atmosférico no se apreciaba de inmediato en la pista musical que ya estaba en reproducción.
   - *Resolución:* Se implementó `BASS_ChannelSetFX` y `BASS_ChannelRemoveFX` dinámicos en `SoundEngine.cs`, aplicando el efecto en caliente en el mismo milisegundo en que el usuario cambia la opción.

---

## 🏆 7. Estado Final del Proyecto

- **Compilación:** 0 Errores en configuraciones Debug y Release.
- **Pruebas Automatizadas:** 141 tests de cobertura unitaria con validación en memoria, físicas de tablero, persistencia y accesibilidad.
- **Git & Releases:** Tag canónico `v2026.08.16.1` publicado en GitHub con auto-actualizador integrado.
