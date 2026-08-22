using System;
using System.Collections.Generic;

namespace Bejeweled3Accessible.Engine
{
    public enum Language
    {
        Spanish,
        English
    }

    public static class Localization
    {
        public static Language CurrentLanguage { get; set; }

        static Localization()
        {
            CurrentLanguage = Language.Spanish;
        }

        private static readonly Dictionary<string, Tuple<string, string>> _dict = new Dictionary<string, Tuple<string, string>>
        {
            // Loading Screen
            { "LoadingTitle", new Tuple<string, string>("Cargando Bejeweled 3... Versión 2026.08.22.3.", "Loading Bejeweled 3... Version 2026.08.22.3.") },
            { "LoadingPrompt", new Tuple<string, string>("Presiona cualquier tecla para continuar", "Press any key to continue") },

            // Profile Screen
            { "CreateProfileTitle", new Tuple<string, string>("Crear Nuevo Perfil de Usuario", "Create New User Profile") },
            { "EnterNamePrompt", new Tuple<string, string>("Por favor introduce tu nombre de usuario y presiona Enter:", "Please enter your username and press Enter:") },
            { "ProfileSelectTitle", new Tuple<string, string>("Seleccionar o Cambiar Usuario", "Select or Change User") },
            { "ProfileCreateNew", new Tuple<string, string>("+ Crear un nuevo perfil", "+ Create a new profile") },
            { "ProfileDelete", new Tuple<string, string>("Eliminar perfil actual", "Delete current profile") },

            // Menus
            { "AppTitle", new Tuple<string, string>("Bejeweled 3 Accesible - Versión 2026.08.22.3", "Bejeweled 3 Accessible - Version 2026.08.22.3") },
            { "MenuPlay", new Tuple<string, string>("Jugar Bejeweled 3", "Play Bejeweled 3") },
            { "MenuChangeUser", new Tuple<string, string>("Haz clic aquí para cambiar de usuario. Perfil actual: {0}", "Click here to change user. Current profile: {0}") },
            { "MenuLanguage", new Tuple<string, string>("Idioma: Español. Pulsa Enter para cambiar", "Language: English. Press Enter to switch to Spanish") },
            { "MenuOptions", new Tuple<string, string>("Opciones de Sonido y Voz", "Sound & Voice Options") },
            { "MenuTutorial", new Tuple<string, string>("Tutorial de Accesibilidad", "Accessibility Tutorial") },
            { "MenuAudioSchool", new Tuple<string, string>("Escuela de Audio", "Audio School") },
            { "AudioSchoolTitle", new Tuple<string, string>("Escuela de Audio: pruebas de auriculares (arriba/abajo para elegir, Enter para escuchar, Escape para volver)", "Audio School: headphone tests (up/down to choose, Enter to listen, Escape to go back)") },
            { "TutorialTitle", new Tuple<string, string>("Tutorial de Accesibilidad y Teclas", "Accessibility & Keys Tutorial") },
            { "TutorialStep1", new Tuple<string, string>("1. Movimiento en Tablero: Usa las Flechas para navegar entre casillas A1 y H8. Cada gema suena a la izquierda o a la derecha según su columna (audio binaural, como el juego original), y su color y tipo los anuncia el lector de pantalla.", "1. Board Movement: Use Arrow keys to navigate cells A1 to H8. Each gem sounds to the left or right according to its column (binaural audio, like the original), and its color and type are announced by the screen reader.") },
            { "TutorialStep2", new Tuple<string, string>("2. Intercambio de Gemas: Presiona las teclas W, A, S, D para realizar un movimiento con la gema adyacente en esa dirección.", "2. Swapping Gems: Press W, A, S, D keys to swap with an adjacent gem in that direction.") },
            { "TutorialStep3", new Tuple<string, string>("3. Consultar Estado General (Tecla R): Presiona R para escuchar la puntuación y el estado del modo actual (tiempo, nivel, hielo, mariposas...). Presiona C para repetir la casilla actual.", "3. General Status Check (Key R): Press R to announce Score and the current mode status (time, level, ice, butterflies...). Press C to repeat current cell.") },
            { "TutorialStep4", new Tuple<string, string>("4. Estado Completo del Modo (Tecla Q): Presiona Q en cualquier momento para escuchar el estado detallado del modo actual: en Quest, el progreso exacto de tu misión (mariposas, pepitas, oro, bombas, cascada, manos de póker, hielo o profundidad); en los demás modos, puntuación, tiempo, nivel, barajadas, cartas, calaveras, mariposas, hielo o metros excavados.", "4. Full Mode Status (Key Q): Press Q anytime to hear the detailed state of the current mode: in Quest, your exact mission progress (butterflies, nuggets, gold, bombs, cascade, poker hands, ice or depth); in other modes, score, time, level, shuffles, cards, skulls, butterflies, ice or meters dug.") },
            { "TutorialStep5", new Tuple<string, string>("5. Buscar Pistas y Ayuda (Tecla H): Presiona H para recibir una recomendación hablada del movimiento más oportuno disponible.", "5. Find Hints & Help (Key H): Press H for a spoken recommendation of the best available move.") },
            { "TutorialStep6", new Tuple<string, string>("6. Modos Principales y Secretos: Clásico (sin tiempo), Relámpago (60s contrarreloj), Zen (relajación ambiental), Quest (40 misiones), Póker (manos de 5 cartas), Mariposas (salvar mariposas), Tormenta de Hielo (bajar columnas de hielo) y Mina de Diamantes (excavación).", "6. Main & Secret Modes: Classic (no timer), Lightning (60s time attack), Zen (relaxation), Quest (40 missions), Poker (5 card hands), Butterflies (save butterflies), Ice Storm (lower ice columns) and Diamond Mine (digging).") },
            { "TutorialStep7", new Tuple<string, string>("7. Menú de Pausa: Presiona Escape en cualquier momento para pausar la partida, ajustar volúmenes o salir.", "7. Pause Menu: Press Escape at any time to pause the match, adjust volumes, or quit.") },
            { "TutorialStep8", new Tuple<string, string>("8. Audio Binaural y Voz: Igual que el original, cada sonido se sitúa a la izquierda o a la derecha del tablero. Las locuciones de voz nunca se cortan ni se solapan: se escuchan completas, una detrás de otra, con un pequeño margen de silencio.", "8. Binaural Audio & Voice: Like the original, each sound is placed to the left or right of the board. Voice lines are never cut short or overlapping: they play in full, one after another, with a small silent gap.") },
            { "MenuExit", new Tuple<string, string>("Salir", "Exit") },
            { "MenuUpdateCheck", new Tuple<string, string>("Actualización: comprobar e instalar", "Update: check and install") },
            { "UpdateChecking", new Tuple<string, string>("Comprobando actualizaciones...", "Checking for updates...") },
            { "UpdateNone", new Tuple<string, string>("No hay versiones nuevas. Estás en la versión {0}, la más reciente.", "No new versions available. You are on version {0}, the latest.") },
            { "UpdateAvailable", new Tuple<string, string>("Estás en la versión {0}. Hay una nueva versión {1} disponible. Novedades de esta versión: {2}. Puedes instalarla desde Actualización: comprobar e instalar en el menú principal.", "You are on version {0}. A new version {1} is available. What's new in this version: {2}. You can install it from Update: check and install in the main menu.") },
            { "UpdateAvailableNoNotes", new Tuple<string, string>("Estás en la versión {0}. Hay una nueva versión {1} disponible. Puedes instalarla desde Actualización: comprobar e instalar en el menú principal.", "You are on version {0}. A new version {1} is available. You can install it from Update: check and install in the main menu.") },
            { "UpdateFound", new Tuple<string, string>("Estás en la versión {0}. La nueva versión {1} está disponible. Novedades de esta versión: {2}. Pulsa Enter para descargar e instalar, o Escape para cancelar.", "You are on version {0}. The new version {1} is available. What's new in this version: {2}. Press Enter to download and install, or Escape to cancel.") },
            { "UpdateFoundNoNotes", new Tuple<string, string>("Estás en la versión {0}. La nueva versión {1} está disponible. Pulsa Enter para descargar e instalar, o Escape para cancelar.", "You are on version {0}. The new version {1} is available. Press Enter to download and install, or Escape to cancel.") },
            { "UpdateCancelled", new Tuple<string, string>("Actualización cancelada.", "Update cancelled.") },
            { "UpdateDownloading", new Tuple<string, string>("Descargando la nueva versión, un momento...", "Downloading the new version, one moment...") },
            { "UpdateInstalling", new Tuple<string, string>("Versión descargada. El juego se cerrará, se instalará la actualización y se abrirá solo al terminar.", "Version downloaded. The game will close, install the update and open by itself when done.") },
            { "UpdateError", new Tuple<string, string>("No se pudo completar la actualización: {0}", "Could not complete the update: {0}") },
            { "UpdateProgress", new Tuple<string, string>("Descarga: {0} por ciento completada", "Download: {0} percent complete") },
            { "UpdateSize", new Tuple<string, string>("Tamaño del archivo: {0}", "File size: {0}") },
            { "UpdateDownloaded", new Tuple<string, string>("Descargados {0} de {1}", "Downloaded {0} of {1}") },
            { "UpdateSpeed", new Tuple<string, string>("Velocidad: {0}. Tiempo restante: {1}", "Speed: {0}. Time remaining: {1}") },
            { "SelectMode", new Tuple<string, string>("Seleccionar Modo de Juego: ", "Select Game Mode: ") },
            { "BackToMain", new Tuple<string, string>("Volver al Menú Principal", "Back to Main Menu") },

            // Options Screen
            { "OptionsTitle", new Tuple<string, string>("Opciones de Sonido y Voz", "Sound & Voice Options") },
            { "OptMusicVol", new Tuple<string, string>("Volumen de Música: {0}%", "Music Volume: {0}%") },
            { "OptSoundVol", new Tuple<string, string>("Volumen de Sonido: {0}%", "Sound Volume: {0}%") },
            { "OptVoiceVol", new Tuple<string, string>("Volumen de Voz: {0}%", "Voice Volume: {0}%") },
            { "OptSpatialAudio", new Tuple<string, string>("Audio Binaural: {0}", "Binaural Audio: {0}") },
            { "OptSpatialProfile", new Tuple<string, string>("Perfil Espacial: {0}", "Spatial Profile: {0}") },
            { "SpatialProfileStage2D", new Tuple<string, string>("Escenario 2D", "Stage 2D") },
            { "SpatialProfileClean", new Tuple<string, string>("Clásico Limpio", "Clean Classic") },
            { "SpatialProfileSimple", new Tuple<string, string>("Simple", "Simple") },
            { "SpatialProfileAtmos3D", new Tuple<string, string>("Objeto 3D (Atmos)", "3D Object (Atmos)") },
            { "OptBack", new Tuple<string, string>("Guardar y Volver al Menú", "Save & Return to Menu") },

            // Main Modes
            { "ModeClassic", new Tuple<string, string>("Modo Clásico. El juego Bejeweled tradicional sin límite de tiempo.", "Classic Mode. Traditional Bejeweled game with no time limits.") },
            { "ModeLightning", new Tuple<string, string>("Modo Relámpago. Carrera contrarreloj llena de emoción y multiplicadores de tiempo.", "Lightning Mode. Action packed race against the clock with time multipliers.") },
            { "ModeZen", new Tuple<string, string>("Modo Zen. Experiencia de juego relajante con sonidos ambientales y respiración.", "Zen Mode. Relaxing gameplay experience with ambient sounds and breathing.") },
            { "ModeQuest", new Tuple<string, string>("Modo Búsqueda (Quest). Resuelve misiones y recupera relicarios antiguos.", "Quest Mode. Solve missions and restore ancient relics.") },

            // Secret Modes (Unlockable)
            { "ModePoker", new Tuple<string, string>("Modo Póker. Realiza combinaciones para formar manos de póker y ganar puntos.", "Poker Mode. Make matches to build poker hands and score big points.") },
            { "ModePokerLocked", new Tuple<string, string>("Modo Póker (Bloqueado - Alcanza Nivel 5 en Clásico)", "Poker Mode (Locked - Reach Level 5 in Classic)") },
            { "ModeButterflies", new Tuple<string, string>("Modo Mariposas. Libera las mariposas antes de que lleguen a la araña superior.", "Butterflies Mode. Free butterflies before they reach the top spider.") },
            { "ModeButterfliesLocked", new Tuple<string, string>("Modo Mariposas (Bloqueado - Alcanza Nivel 5 en Zen)", "Butterflies Mode (Locked - Reach Level 5 in Zen)") },
            { "ModeIceStorm", new Tuple<string, string>("Tormenta de Hielo. Evita que las columnas de hielo suban y congelen el tablero.", "Ice Storm Mode. Stop rising columns of ice from freezing the board.") },
            { "ModeIceStormLocked", new Tuple<string, string>("Tormenta de Hielo (Bloqueado - Supera 100,000 en Relámpago)", "Ice Storm Mode (Locked - Score over 100,000 in Lightning)") },
            { "ModeDiamondMine", new Tuple<string, string>("Mina de Diamantes. Excava en la tierra destruyendo gemas inferiores antes del tiempo.", "Diamond Mine Mode. Dig into the ground destroying lower gems before time runs out.") },
            { "ModeDiamondMineLocked", new Tuple<string, string>("Modo Mina de Diamantes (Bloqueado - Completa 4 desafíos en Quest)", "Diamond Mine Mode (Locked - Complete 4 challenges in Quest)") },

            // Colors & Authentic Bejeweled 3 Geometric Shapes
            { "ShapeRuby", new Tuple<string, string>("Cuadrado rojo", "Red square") },
            { "ShapeTopaz", new Tuple<string, string>("Triángulo amarillo", "Yellow triangle") },
            { "ShapeEmerald", new Tuple<string, string>("Hexágono verde", "Green hexagon") },
            { "ShapeSapphire", new Tuple<string, string>("Diamante azul", "Blue diamond") },
            { "ShapeAmethyst", new Tuple<string, string>("Octágono púrpura", "Purple octagon") },
            { "ShapeDiamond", new Tuple<string, string>("Gema circular blanca", "White sphere diamond") },
            { "ShapeAmber", new Tuple<string, string>("Rombo naranja", "Orange rhombus") },

            // Colors
            { "ColorRed", new Tuple<string, string>("rojo", "red") },
            { "ColorYellow", new Tuple<string, string>("amarillo", "yellow") },
            { "ColorGreen", new Tuple<string, string>("verde", "green") },
            { "ColorBlue", new Tuple<string, string>("azul", "blue") },
            { "ColorPurple", new Tuple<string, string>("púrpura", "purple") },
            { "ColorWhite", new Tuple<string, string>("blanco", "white") },
            { "ColorOrange", new Tuple<string, string>("naranja", "orange") },

            // Gems & Specials with Shape Inheritance
            { "Gem", new Tuple<string, string>("Gema ", "Gem ") },
            { "Hypercube", new Tuple<string, string>("Hipercubo", "Hypercube") },
            { "SupernovaShape", new Tuple<string, string>("Supernova {0}", "{0} supernova") },
            { "FlameShape", new Tuple<string, string>("{0} de fuego", "Flame {0}") },
            { "StarShape", new Tuple<string, string>("{0} estela", "Star {0}") },
            { "Time5Shape", new Tuple<string, string>("{0} más 5 segundos", "{0} plus 5 seconds") },
            { "Time10Shape", new Tuple<string, string>("{0} más 10 segundos", "{0} plus 10 seconds") },
            { "ButterflyShape", new Tuple<string, string>("Mariposa {0}", "{0} butterfly") },
            { "BombShape", new Tuple<string, string>("Bomba {0} en {1} turnos", "{0} bomb in {1} turns") },
            { "GoldShape", new Tuple<string, string>("{0} de oro", "Gold {0}") },

            { "Supernova", new Tuple<string, string>("Gema Supernova ", "Supernova Gem ") },
            { "Flame", new Tuple<string, string>("Gema de Fuego ", "Flame Gem ") },
            { "Star", new Tuple<string, string>("Gema Estela ", "Star Gem ") },
            { "Time5", new Tuple<string, string>("Tiempo más 5 ", "Time plus 5 ") },
            { "Time10", new Tuple<string, string>("Tiempo más 10 ", "Time plus 10 ") },
            { "Butterfly", new Tuple<string, string>("Mariposa ", "Butterfly ") },
            { "Bomb", new Tuple<string, string>("Bomba {0} en {1} turnos", "{0} Bomb in {1} turns") },
            { "TileDirt", new Tuple<string, string>("Bloque de Tierra", "Dirt Block") },
            { "TileHardRock", new Tuple<string, string>("Roca Dura ({0} durabilidad)", "Hard Rock ({0} durability)") },
            { "TileGoldNugget", new Tuple<string, string>("Pepita de Oro", "Gold Nugget") },
            { "Gold", new Tuple<string, string>("Oro ", "Gold ") },

            // Pause Menu & Hints
            { "PauseTitle", new Tuple<string, string>("Juego Pausado", "Game Paused") },
            { "PauseResume", new Tuple<string, string>("Reanudar Partida", "Resume Game") },
            { "PauseReset", new Tuple<string, string>("Reiniciar Tablero", "Reset Board") },
            { "PauseOptions", new Tuple<string, string>("Opciones de Sonido y Voz", "Sound & Voice Options") },
            { "PauseQuit", new Tuple<string, string>("Salir al Menú Principal", "Quit to Main Menu") },
            { "HintFound", new Tuple<string, string>("Pista: Mueve {0} de {1} hacia {2}", "Hint: Move {0} from {1} {2}") },
            { "NoHintFound", new Tuple<string, string>("No se encontraron pistas disponibles.", "No hints available.") },
            { "DirUp", new Tuple<string, string>("Arriba", "Up") },
            { "DirDown", new Tuple<string, string>("Abajo", "Down") },
            { "DirLeft", new Tuple<string, string>("Izquierda", "Left") },
            { "DirRight", new Tuple<string, string>("Derecha", "Right") },
            { "MoveHint", new Tuple<string, string>("Mueve {0}", "Move {0}") },
            { "DirOr", new Tuple<string, string>(" o ", " or ") },

            // Announcements
            { "Welcome", new Tuple<string, string>("Bienvenido a Bejeweled 3 Accesible. Presiona flechas para navegar.", "Welcome to Bejeweled 3 Accessible. Press arrow keys to navigate.") },
            { "ClassicStarted", new Tuple<string, string>("Modo Clásico iniciado. Tablero listo.", "Classic Mode started. Board is ready.") },
            { "LightningStarted", new Tuple<string, string>("Modo Relámpago iniciado. ¡Tienes 60 segundos!", "Lightning Mode started. You have 60 seconds!") },
            { "ZenStarted", new Tuple<string, string>("Modo Zen iniciado. Disfruta un juego relajante.", "Zen Mode started. Enjoy a relaxing game.") },
            { "PokerStarted", new Tuple<string, string>("Modo Póker iniciado. Arma tu mano de 5 cartas.", "Poker Mode started. Build your 5 card hand.") },
            { "ButterfliesStarted", new Tuple<string, string>("Modo Mariposas iniciado. Libera las mariposas.", "Butterflies Mode started. Free the butterflies.") },
            { "IceStormStarted", new Tuple<string, string>("Modo Tormenta de Hielo iniciado. ¡Detén el hielo!", "Ice Storm Mode started. Stop the ice!") },
            { "DiamondMineStarted", new Tuple<string, string>("Modo Mina de Diamantes iniciado. ¡Excava profundo!", "Diamond Mine Mode started. Dig deep!") },
            { "TimeExtended", new Tuple<string, string>("¡Tiempo extendido! Multiplicador a {0}x.", "Time Extended! Multiplier now {0}x.") },
            { "GameOver", new Tuple<string, string>("¡Juego terminado! Puntuación final: {0} puntos.", "Game Over! Final score: {0} points.") },
            { "GameOverReplay", new Tuple<string, string>("Jugar de Nuevo", "Play Again") },
            { "GameOverMenu", new Tuple<string, string>("Menú Principal", "Main Menu") },
            { "InvalidMove", new Tuple<string, string>("Movimiento no válido.", "Invalid move.") },
            { "EdgeReached", new Tuple<string, string>("Límite del tablero alcanzado.", "Edge of board reached.") },
            { "ScoreAnnouncement", new Tuple<string, string>("Puntuación: {0} puntos. Nivel: {1}.", "Score: {0} points. Level: {1}.") },
            { "LightningScoreAnnouncement", new Tuple<string, string>("Puntuación: {0}. Tiempo: {1}s. Multiplicador: {2}x.", "Score: {0}. Time: {1}s. Multiplier: {2}x.") },
            { "Empty", new Tuple<string, string>("Vacío", "Empty") },

            // Badges
            { "MenuBadges", new Tuple<string, string>("Insignias y Logros", "Badges & Achievements") },
            { "BadgeUnlockedAnnounce", new Tuple<string, string>("¡Insignia Desbloqueada! {0} nivel {1}.", "Badge Unlocked! {0} tier {1}.") },
            { "BadgeInferno", new Tuple<string, string>("Inferno (Destruir gemas de fuego: Bronce 50, Plata 350, Oro 1000, Platino 2000)", "Inferno (Destroy flame gems: Bronze 50, Silver 350, Gold 1000, Platinum 2000)") },
            { "BadgeStellar", new Tuple<string, string>("Estelar (Destruir gemas estela: Bronce 25, Plata 125, Oro 400, Platino 750)", "Stellar (Destroy star gems: Bronze 25, Silver 125, Gold 400, Platinum 750)") },
            { "BadgeChromatic", new Tuple<string, string>("Cromático (Destruir hipercubos: Bronce 25, Plata 125, Oro 400, Platino 750)", "Chromatic (Destroy hypercubes: Bronze 25, Silver 125, Gold 400, Platinum 750)") },
            { "BadgeBlaster", new Tuple<string, string>("Destructor (Gemas en 1 movimiento: Bronce 30, Plata 40, Oro 50, Platino 60)", "Blaster (Gems in 1 move: Bronze 30, Silver 40, Gold 50, Platinum 60)") },
            { "BadgeBejeweler", new Tuple<string, string>("Bejeweler (Puntos en Clásico: Bronce 50.000, Plata 150.000, Oro 300.000, Platino 500.000)", "Bejeweler (Classic Mode score: Bronze 50k, Silver 150k, Gold 300k, Platinum 500k)") },
            { "BadgeFinalFrenzy", new Tuple<string, string>("Frenesí Final (Puntos durante la última carga en Relámpago: Bronce 20.000, Plata 30.000, Oro 40.000, Platino 60.000)", "Final Frenzy (Score during a Last Hurrah in Lightning: Bronze 20k, Silver 30k, Gold 40k, Platinum 60k)") },
            { "BadgeHighVoltage", new Tuple<string, string>("Alto Voltaje (Puntos en Relámpago: Bronce 100.000, Plata 300.000, Oro 500.000, Platino 750.000)", "High Voltage (Lightning Mode score: Bronze 100k, Silver 300k, Gold 500k, Platinum 750k)") },
            { "BadgeAnteUp", new Tuple<string, string>("Apuesta Doble (Puntos en Póker: Bronce 100.000, Plata 300.000, Oro 500.000, Platino 750.000)", "Ante Up (Poker Mode score: Bronze 100k, Silver 300k, Gold 500k, Platinum 750k)") },
            { "BadgeGambler", new Tuple<string, string>("El Jugador (Escaleras de color en Póker: Bronce 10, Plata 30, Oro 60, Platino 100)", "The Gambler (Flushes in Poker: Bronze 10, Silver 30, Gold 60, Platinum 100)") },
            { "BadgeGlacialExplorer", new Tuple<string, string>("Explorador Glacial (Puntos en Tormenta de Hielo: Bronce 100.000, Plata 300.000, Oro 500.000, Platino 750.000)", "Glacial Explorer (Ice Storm score: Bronze 100k, Silver 300k, Gold 500k, Platinum 750k)") },
            { "BadgeIceBreaker", new Tuple<string, string>("Rompehielos (Columnas derretidas en 1 jugada en Tormenta de Hielo: Bronce 5, Plata 8, Oro 12, Platino 15)", "Ice Breaker (Ice columns melted in one move: Bronze 5, Silver 8, Gold 12, Platinum 15)") },
            { "BadgeDiamondMine", new Tuple<string, string>("Diamante, Mina (Puntos en Mina de Diamantes: Bronce 100.000, Plata 300.000, Oro 500.000, Platino 750.000)", "Diamond, Mine (Diamond Mine score: Bronze 100k, Silver 300k, Gold 500k, Platinum 750k)") },
            { "BadgeRelicHunter", new Tuple<string, string>("Cazador de Reliquias (Artefactos extraídos en Mina de Diamantes: Bronce 5, Plata 8, Oro 12, Platino 15)", "Relic Hunter (Artifacts dug in Diamond Mine: Bronze 5, Silver 8, Gold 12, Platinum 15)") },
            { "BadgeButterflyMonarch", new Tuple<string, string>("Monarca de Mariposas (Puntos en Mariposas: Bronce 100.000, Plata 300.000, Oro 500.000, Platino 750.000)", "Butterfly Monarch (Butterflies score: Bronze 100k, Silver 300k, Gold 500k, Platinum 750k)") },
            { "BadgeButterflyBonanza", new Tuple<string, string>("Bonanza de Mariposas (Mariposas salvadas en 1 jugada: Bronce 4, Plata 6, Oro 8, Platino 10)", "Butterfly Bonanza (Butterflies freed in one move: Bronze 4, Silver 6, Gold 8, Platinum 10)") },
            { "BadgeAnnihilator", new Tuple<string, string>("Aniquilador Élite (Unir Hipercubo con Hipercubo: Platino)", "Annihilator Elite (Match Hypercube with Hypercube: Platinum)") },
            { "BadgeSuperstar", new Tuple<string, string>("Superestrella Élite (Crear una Supernova: Platino)", "Superstar Elite (Create a Supernova gem: Platinum)") },
            { "BadgeLevelord", new Tuple<string, string>("Señor de los Niveles Élite (Alcanzar Nivel 10 en Clásico: Platino)", "Levelord Elite (Reach Level 10 in Classic: Platinum)") },
            { "BadgeTopSecret", new Tuple<string, string>("Top Secret Élite (Récord en los 4 modos secretos: Platino)", "Top Secret Elite (High score in all 4 secret modes: Platinum)") },
            { "BadgeHeroes", new Tuple<string, string>("Héroes Bienvenidos Élite (Completar el 100% de Quest: Platino)", "Heroes Welcome Elite (Achieve 100% completion in Quest: Platinum)") },
            { "BadgeMenuHelp", new Tuple<string, string>("Pulsa Enter para repetir una insignia. Escape para volver al menú.", "Press Enter to repeat a badge. Escape to go back.") },
            { "TierLocked", new Tuple<string, string>("Bloqueado", "Locked") },
            { "TierBronze", new Tuple<string, string>("Bronce", "Bronze") },
            { "TierSilver", new Tuple<string, string>("Plata", "Silver") },
            { "TierGold", new Tuple<string, string>("Oro", "Gold") },
            { "TierPlatinum", new Tuple<string, string>("Platino", "Platinum") },

            // Records & Stats
            { "MenuRecords", new Tuple<string, string>("Récords y Estadísticas", "Records & Stats") },
            { "StatPlayerRank", new Tuple<string, string>("Rango de Jugador: {0}", "Player Rank: {0}") },
            { "RankTitleFormat", new Tuple<string, string>("Nivel {0}: {1}", "Rank {0}: {1}") },
            { "RankUpAnnouncement", new Tuple<string, string>("¡Nuevo rango alcanzado! {0}.", "New rank reached! {0}.") },
            { "StatTotalScore", new Tuple<string, string>("Puntuación Total Acumulada: {0} puntos", "Total Accumulated Score: {0} points") },
            { "StatTotalGems", new Tuple<string, string>("Total de Gemas Eliminadas: {0}", "Total Gems Cleared: {0}") },
            { "StatClassicLevel", new Tuple<string, string>("Máximo Nivel Clásico: {0}", "Max Classic Level: {0}") },
            { "StatZenLevel", new Tuple<string, string>("Máximo Nivel Zen: {0}", "Max Zen Level: {0}") },
            { "StatLightningRecord", new Tuple<string, string>("Récord Modo Relámpago: {0} puntos", "Lightning Mode High Score: {0} points") },
            { "StatPokerRecord", new Tuple<string, string>("Récord Modo Póker: {0} puntos", "Poker Mode High Score: {0} points") },
            { "StatButterfliesRecord", new Tuple<string, string>("Récord Modo Mariposas: {0} puntos", "Butterflies Mode High Score: {0} points") },
            { "StatIceStormRecord", new Tuple<string, string>("Récord Modo Tormenta de Hielo: {0} puntos", "Ice Storm Mode High Score: {0} points") },
            { "StatDiamondMineRecord", new Tuple<string, string>("Récord Modo Mina de Diamantes: {0} puntos", "Diamond Mine Mode High Score: {0} points") },
            { "StatFlamesDestroyed", new Tuple<string, string>("Gemas de fuego destruidas: {0}", "Flame gems destroyed: {0}") },
            { "StatStarsDestroyed", new Tuple<string, string>("Gemas estela destruidas: {0}", "Star gems destroyed: {0}") },
            { "StatHypercubesDestroyed", new Tuple<string, string>("Hipercubos destruidos: {0}", "Hypercubes destroyed: {0}") },

            // Zen Options Menu
            { "ZenOptionsTitle", new Tuple<string, string>("Opciones del Modo Zen", "Zen Mode Options") },
            { "ZenOptAmbient", new Tuple<string, string>("Sonidos Ambientales: {0}", "Ambient Sounds: {0}") },
            { "ZenOptMantras", new Tuple<string, string>("Mantras de Meditación: {0}", "Meditation Mantras: {0}") },
            { "ZenOptBreath", new Tuple<string, string>("Modulación de Respiración: {0}", "Breath Modulation: {0}") },
            { "StateEnabled", new Tuple<string, string>("Activado", "Enabled") },
            { "StateDisabled", new Tuple<string, string>("Desactivado", "Disabled") },

            // Zen Ambient Names
            { "AmbientNone", new Tuple<string, string>("Ninguno", "None") },
            { "AmbientCoastal", new Tuple<string, string>("Costa", "Coastal") },
            { "AmbientCrickets", new Tuple<string, string>("Grillos", "Crickets") },
            { "AmbientForest", new Tuple<string, string>("Bosque", "Forest") },
            { "AmbientOceanSurf", new Tuple<string, string>("Olas del Mar", "Ocean Surf") },
            { "AmbientRainLeaves", new Tuple<string, string>("Lluvia y Hojas", "Rain Leaves") },
            { "AmbientWaterfall", new Tuple<string, string>("Cascada", "Waterfall") },

            // Zen Mantras & Breath
            { "ZenMantra1", new Tuple<string, string>("La paz comienza con una respiración profunda.", "Peace begins with a deep breath.") },
            { "ZenMantra2", new Tuple<string, string>("Tus pensamientos son claros, serenos y tranquilos.", "Your thoughts are clear, serene, and calm.") },
            { "ZenMantra3", new Tuple<string, string>("Visualiza la abundancia y la armonía en tu vida.", "Visualize abundance and harmony in your life.") },
            { "ZenMantra4", new Tuple<string, string>("Libera el estrés y abraza el momento presente.", "Release stress and embrace the present moment.") },
            { "ZenMantra5", new Tuple<string, string>("Cada gema alineada aporta enfoque y tranquilidad.", "Every matched gem brings focus and tranquility.") },
            { "ZenThemeGeneral", new Tuple<string, string>("General", "General") },
            { "ZenThemePositiveThinking", new Tuple<string, string>("Pensamiento Positivo", "Positive Thinking") },
            { "ZenThemeProsperity", new Tuple<string, string>("Prosperidad", "Prosperity") },
            { "ZenThemeQuitBadHabits", new Tuple<string, string>("Dejar Malos Hábitos", "Quit Bad Habits") },
            { "ZenThemeSelfConfidence", new Tuple<string, string>("Confianza en Uno Mismo", "Self Confidence") },
            { "ZenThemeWeightLoss", new Tuple<string, string>("Pérdida de Peso", "Weight Loss") },
            { "ZenBreathInhale", new Tuple<string, string>("Inhala durante 5 segundos...", "Inhale for 5 seconds...") },
            { "ZenBreathHoldIn", new Tuple<string, string>("Mantén el aire...", "Hold your breath...") },
            { "ZenBreathExhale", new Tuple<string, string>("Exhala durante 5 segundos...", "Exhale for 5 seconds...") },
            { "ZenBreathHoldOut", new Tuple<string, string>("Mantén los pulmones vacíos...", "Hold empty lungs...") },

            // Quest Relics & Challenges
            { "QuestSelectTitle", new Tuple<string, string>("Seleccionar Relicario de Búsqueda", "Select Quest Relic") },
            { "Relic1", new Tuple<string, string>("Relicario 1: El Templo de Rubí", "Relic 1: The Ruby Temple") },
            { "Relic2", new Tuple<string, string>("Relicario 2: La Ciudad Esmeralda", "Relic 2: The Emerald City") },
            { "Relic3", new Tuple<string, string>("Relicario 3: El Altar de Zafiro", "Relic 3: The Sapphire Altar") },
            { "Relic4", new Tuple<string, string>("Relicario 4: La Caverna de Topacio", "Relic 4: The Topaz Cavern") },
            { "Relic5", new Tuple<string, string>("Relicario 5: La Corona de Diamantes", "Relic 5: The Diamond Crown") },
            { "Relic6", new Tuple<string, string>("Relicario 6: La Fuente de Cristal", "Relic 6: The Crystal Fountain") },
            { "Relic7", new Tuple<string, string>("Relicario 7: El Espejo de Ónice", "Relic 7: The Onyx Mirror") },
            { "Relic8", new Tuple<string, string>("Relicario 8: La Púa de Plata", "Relic 8: The Silver Spur") },
            { "Relic9", new Tuple<string, string>("Relicario 9: El Martillo de Bronce", "Relic 9: The Bronze Hammer") },
            { "Relic10", new Tuple<string, string>("Relicario 10: La Esfera Lunar", "Relic 10: The Lunar Sphere") },
            { "QuestMissionButterflies", new Tuple<string, string>("Mariposas {0}: Libera {1} mariposas", "Butterflies {0}: Free {1} butterflies") },
            { "QuestMissionGoldRush", new Tuple<string, string>("Fiebre del Oro {0}: Desentierra {1} pepitas de oro", "Gold Rush {0}: Mine {1} gold nuggets") },
            { "QuestMissionAlchemy", new Tuple<string, string>("Alquimia {0}: Convierte {1} casillas en oro", "Alchemy {0}: Turn {1} tiles into gold") },
            { "QuestMissionTimeBomb", new Tuple<string, string>("Bombas de Tiempo {0}: Destruye {1} bombas", "Time Bombs {0}: Destroy {1} bombs") },
            { "QuestMissionAvalanche", new Tuple<string, string>("Avalancha {0}: Alcanza una cascada de {1}", "Avalanche {0}: Reach a cascade of {1}") },
            { "QuestMissionPoker", new Tuple<string, string>("Póker {0}: Consigue {1} manos sin calaveras", "Poker {0}: Score {1} hands without skulls") },
            { "QuestMissionIceStorm", new Tuple<string, string>("Tormenta de Hielo {0}: Rompe {1} columnas de hielo", "Ice Storm {0}: Break {1} ice columns") },
            { "QuestMissionDiamondMine", new Tuple<string, string>("Mina de Diamantes {0}: Excava {1} metros", "Diamond Mine {0}: Dig {1} meters") },
            { "QuestCompletedMark", new Tuple<string, string>(" (Completada)", " (Completed)") },
            { "QuestProgressButterflies", new Tuple<string, string>("Mariposas liberadas: {0} de {1}. ", "Butterflies freed: {0} of {1}. ") },
            { "QuestProgressNuggets", new Tuple<string, string>("Pepitas de oro: {0} de {1}. ", "Gold nuggets: {0} of {1}. ") },
            { "QuestProgressGold", new Tuple<string, string>("Casillas convertidas en oro: {0} de {1}. ", "Tiles turned to gold: {0} of {1}. ") },
            { "QuestProgressBombs", new Tuple<string, string>("Bombas destruidas: {0} de {1}. ", "Bombs destroyed: {0} of {1}. ") },
            { "QuestProgressAvalanche", new Tuple<string, string>("Cascada máxima: {0} de {1}. ", "Max cascade: {0} of {1}. ") },
            { "QuestProgressPoker", new Tuple<string, string>("Manos de póker: {0} de {1}. Calaveras: {2} de 5. ", "Poker hands: {0} of {1}. Skulls: {2} of 5. ") },
            { "QuestProgressIce", new Tuple<string, string>("Columnas de hielo rotas: {0} de {1}. ", "Ice columns broken: {0} of {1}. ") },
            { "QuestProgressDepth", new Tuple<string, string>("Profundidad: {0} de {1} metros. ", "Depth: {0} of {1} meters. ") },
            { "QuestMissionIntro", new Tuple<string, string>("Misión Iniciada: {0}. ", "Mission Started: {0}. ") },
            { "PokerSkullAnnounce", new Tuple<string, string>("¡Calavera! {0} de 5.", "Skull! {0} of 5.") },
            { "PokerSkullEliminated", new Tuple<string, string>("¡Calavera eliminada! Quedan {0} sobre la mesa.", "Skull eliminated! {0} left on the table.") },
            { "PokerSkullGameOver", new Tuple<string, string>("¡Cinco calaveras! La mesa de póker se derrumbó.", "Five skulls! The poker table collapsed.") },
            { "ShuffleAnnounce", new Tuple<string, string>("Sin movimientos. Barajando. Barajados restantes: {0} de 3.", "No moves. Scrambling. Shuffles left: {0} of 3.") },
            { "NoShufflesLeft", new Tuple<string, string>("Sin movimientos y sin barajados. Fin de la partida.", "No moves and no shuffles left. Game over!") },
            { "BombExploded", new Tuple<string, string>("¡Una bomba explotó!", "A bomb exploded!") },
            { "NuggetFound", new Tuple<string, string>("¡Pepita de oro encontrada!", "Gold nugget found!") },
            { "IceDangerColumns", new Tuple<string, string>("¡Peligro! Hielo cerca del tope en columnas: {0}", "Warning! Ice near the top in columns: {0}") },
            { "IceSkullColumns", new Tuple<string, string>("¡Cráneo! El hielo crestó el tablero en columnas: {0}. ¡Derrita la columna o el tablero se congelará!", "Skull! Ice crested the board in columns: {0}. Melt the column or the board will freeze!") },
            { "IceSkullResolved", new Tuple<string, string>("Columna de hielo {0} empujada hacia abajo. Cráneo desarmado.", "Ice column {0} pushed back down. Skull disarmed.") },
            { "GoldConvertedAnnounce", new Tuple<string, string>("¡Casillas convertidas en oro: {0}!", "{0} tiles turned to gold!") },
            { "QuestChallengeStratamax", new Tuple<string, string>("Estratamax: Destruye 120 gemas en menos de 20 movimientos", "Stratamax: Clear 120 gems in 20 moves or less") },
            { "QuestChallengeGoldRush", new Tuple<string, string>("Fiebre del Oro: Desentierra 5 pepitas de oro de las rocas inferiores", "Gold Rush: Mine 5 gold nuggets from bottom rocks") },
            { "QuestChallengeAlchemy", new Tuple<string, string>("Alquimia: Convierte el 100% de las casillas del tablero en oro", "Alchemy: Turn 100% of board tiles into gold") },
            { "QuestChallengeTimeBomb", new Tuple<string, string>("Bombas de Tiempo: Destruye 10 bombas antes de que su contador llegue a cero", "Time Bombs: Destroy 10 bombs before countdown hits zero") },
            { "QuestChallengeButterflies", new Tuple<string, string>("Rescate de Mariposas: Libera 15 mariposas de la araña", "Butterfly Rescue: Free 15 butterflies from the spider") },
            { "QuestChallengePoker", new Tuple<string, string>("Desafío Póker: Consigue 5 manos de póker sin caer en la calavera", "Poker Challenge: Score 5 poker hands without getting skulls") },
            { "QuestChallengeIceStorm", new Tuple<string, string>("Tormenta de Hielo: Derrite 8 columnas heladas antes de congelarte", "Ice Storm: Melt 8 ice columns before freezing") },
            { "QuestChallengeDiamondMine", new Tuple<string, string>("Mina de Diamantes: Excava 30 metros de profundidad bajo tierra", "Diamond Mine: Dig 30 meters deep underground") },
            { "QuestChallengeAvalanche", new Tuple<string, string>("Avalancha de Gemas: Forma combos continuos en una lluvia masiva", "Gem Avalanche: Form continuous combos in a massive rain") },
            { "QuestChallengeSandstorm", new Tuple<string, string>("Tormenta de Arena: Desentierra 3 reliquias ocultas bajo la arena", "Sandstorm: Unearth 3 hidden relics beneath the sand") },
            { "QuestChallengeBalance", new Tuple<string, string>("Balanza de Gemas: Mantén en equilibrio el peso entre gemas rojas y azules", "Gem Balance: Keep red and blue gem weight balanced") },
            { "QuestChallengeComplete", new Tuple<string, string>("¡Desafío de Búsqueda Completado!", "Quest Challenge Completed!") },
            { "QuestStatusTitle", new Tuple<string, string>("Estado de Quest: Misión \"{0}\". ", "Quest Status: Mission \"{0}\". ") },
            { "QuestStatusScore", new Tuple<string, string>("Puntuación actual: {0} puntos. ", "Current Score: {0} points. ") },
            { "QuestStatusBombs", new Tuple<string, string>("Bombas de tiempo en tablero: {0} activas. Tiempo de bomba menor: {1} segundos. ", "Time bombs on board: {0} active. Lowest bomb timer: {1} seconds. ") },
            { "QuestStatusNuggets", new Tuple<string, string>("Pepitas de oro obtenidas: {0} de {1}. ", "Gold nuggets mined: {0} of {1}. ") },
            { "QuestStatusButterflies", new Tuple<string, string>("Mariposas liberadas: {0} de {1}. ", "Butterflies freed: {0} of {1}. ") },
            { "QuestStatusPokerHands", new Tuple<string, string>("Manos de póker conseguidas: {0} de {1}. ", "Poker hands scored: {0} of {1}. ") },
            { "QuestStatusSkulls", new Tuple<string, string>("Calaveras en la mesa: {0}. ", "Skulls on the table: {0}. ") },
            { "QuestStatusIceColumns", new Tuple<string, string>("Columnas heladas derretidas: {0} de {1}. ", "Ice columns melted: {0} of {1}. ") },
            { "QuestStatusDepth", new Tuple<string, string>("Profundidad excavada: {0} de {1} metros. ", "Mined depth: {0} of {1} meters. ") },
            { "QuestStatusGoldTiles", new Tuple<string, string>("Casillas convertidas en oro: {0} de {1}. ", "Tiles converted to gold: {0} of {1}. ") },
            { "QuestStatusBombsDestroyed", new Tuple<string, string>("Bombas de tiempo destruidas: {0} de {1}. ", "Time bombs destroyed: {0} of {1}. ") },
            { "QuestStatusCascade", new Tuple<string, string>("Cascada máxima conseguida: {0} de {1}. ", "Max cascade reached: {0} of {1}. ") },
            { "QuestStatusInactive", new Tuple<string, string>("No estás en Modo Búsqueda (Quest). Misión inactiva.", "Not in Quest Mode. Mission inactive.") },

            // Per-mode playing status (press Q)
            { "ClassicStatus", new Tuple<string, string>("Modo Clásico. Puntuación: {0}. Nivel: {1}. Progreso de nivel: {2} de {3} puntos. Barajadas restantes: {4}.", "Classic Mode. Score: {0}. Level: {1}. Level progress: {2} of {3} points. Shuffles left: {4}.") },
            { "ZenStatus", new Tuple<string, string>("Modo Zen. Puntuación: {0}. Nivel: {1}. Progreso de nivel: {2} de {3} puntos.", "Zen Mode. Score: {0}. Level: {1}. Level progress: {2} of {3} points.") },
            { "PokerStatus", new Tuple<string, string>("Modo Póker. Puntuación: {0}. Cartas en la mano: {1} de 5. Calaveras en la mesa: {2}. Barra eliminadora: {3} de 3.", "Poker Mode. Score: {0}. Cards in hand: {1} of 5. Skulls on the table: {2}. Skull eliminator: {3} of 3.") },
            { "ButterfliesModeStatus", new Tuple<string, string>("Modo Mariposas. Puntuación: {0}. Mariposas en el tablero: {1}. Columnas: {2}.", "Butterflies Mode. Score: {0}. Butterflies on the board: {1}. Columns: {2}.") },
            { "IceStormModeStatus", new Tuple<string, string>("Tormenta de Hielo. Puntuación: {0}. Columnas desheladas: {1}. {2}", "Ice Storm. Score: {0}. Columns melted: {1}. {2}") },
            { "IceSkullSuffix", new Tuple<string, string>("Columnas crestadas CON cráneo activo: {0}. ", "Columns crested WITH active skull: {0}. ") },
            { "IceDangerSuffix", new Tuple<string, string>("¡Cuidado! El hielo está a punto de congelarse en las columnas {0}. ", "Careful! Ice is about to freeze in columns {0}. ") },
            { "DiamondMineStatus", new Tuple<string, string>("Mina de Diamantes. Puntuación: {0}. Profundidad: {1} metros. Tiempo restante: {2} segundos.", "Diamond Mine. Score: {0}. Depth: {1} meters. Time left: {2} seconds.") },

            // Gameplay Announcements & Unlocks
            { "UnlockPoker", new Tuple<string, string>("¡Modo Póker Desbloqueado!", "Poker Mode Unlocked!") },
            { "UnlockButterflies", new Tuple<string, string>("¡Modo Mariposas Desbloqueado!", "Butterflies Mode Unlocked!") },
            { "UnlockDiamondMine", new Tuple<string, string>("¡Modo Mina de Diamantes Desbloqueado!", "Diamond Mine Mode Unlocked!") },
            { "QuestCompleteAnnounce", new Tuple<string, string>("¡Desafío de Quest completado! Regresando al menú de misiones. {0}", "Quest Challenge Completed! Returning to mission menu. {0}") },
            { "IceWarning", new Tuple<string, string>("¡Peligro! Columna de hielo alta en la columna {0}", "Warning! High ice column on column {0}") },
            { "ButterflyCaught", new Tuple<string, string>("¡Una mariposa fue atrapada por la araña! ", "A butterfly was caught by the spider! ") },
            { "ButterflyFreed", new Tuple<string, string>("¡Mariposas liberadas: {0}!", "{0} Butterfly freed!") },
            { "ButterflyStart", new Tuple<string, string>("Hay {0} mariposas en el tablero. Cada movimiento las hace subir: libera las que puedas.", "There are {0} butterflies on the board. Every move raises them: free as many as you can.") },
            { "ButterflyDanger", new Tuple<string, string>("¡Mariposa en peligro en columna {0}! Un movimiento más y la araña la atrapa.", "Butterfly in danger in column {0}! One more move and the spider catches it.") },
            { "ButterflyStatus", new Tuple<string, string>("Mariposas en el tablero: {0}. Columnas: {1}.", "Butterflies on the board: {0}. Columns: {1}.") },
            { "PokerHandScored", new Tuple<string, string>("¡Mano de Póker! {0}. ¡+{1} Puntos!", "Poker Hand! {0}. +{1} Points!") },
            { "HandHighCard", new Tuple<string, string>("Carta Alta", "High Card") },
            { "HandPair", new Tuple<string, string>("Pareja", "Pair") },
            { "HandSpectrum", new Tuple<string, string>("Espectro", "Spectrum") },
            { "HandTwoPair", new Tuple<string, string>("Doble Pareja", "Two Pair") },
            { "HandThreeOfAKind", new Tuple<string, string>("Trío", "Three of a Kind") },
            { "HandFullHouse", new Tuple<string, string>("Full House", "Full House") },
            { "HandFourOfAKind", new Tuple<string, string>("Póker", "Four of a Kind") },
            { "HandFlush", new Tuple<string, string>("Color", "Flush") },
            { "ArtifactFound", new Tuple<string, string>("¡Artefacto y Tesoro encontrados a {0} metros!", "Artifact and Treasure found at {0} meters!") },
            { "CascadeAnnounce", new Tuple<string, string>("Cascada nivel {0}. {1} gemas destruidas. {2} Puntuación.", "Cascade level {0}. {1} gems destroyed. {2} Score.") },
            { "MatchAnnounce", new Tuple<string, string>("Combinación de {0} gemas. {1} puntos.", "Match {0}. {1} points.") },
            { "MultipleMatchAnnounce", new Tuple<string, string>("¡{0} combinaciones simultáneas! {1} puntos de premio.", "Multi-match: {0} simultaneous matches! {1} bonus points.") },
            { "SpeedBonusAnnounce", new Tuple<string, string>("Bono de velocidad: {0} puntos.", "Speed bonus: {0} points.") },
            { "NoMoreMovesScramble", new Tuple<string, string>("¡Sin más movimientos! Barajando tablero...", "No more moves! Scrambling board...") },
            { "HypercubeCreatedCell", new Tuple<string, string>("Hipercubo creado en casilla actual.", "Hypercube created at current cell.") },
            { "FlameCreatedCell", new Tuple<string, string>("Gema de Fuego creada en casilla actual.", "Flame Gem created at current cell.") },
            { "StarCreatedCell", new Tuple<string, string>("Estrella creada en casilla actual.", "Star created at current cell.") },
            { "IceColumnStatus", new Tuple<string, string>("Puntuación: {0}. Hielo en columna actual: {1} de 8.", "Score: {0}. Ice in current column: {1} of 8.") },
            { "IceColumnCrestedStatus", new Tuple<string, string>("Puntuación: {0}. ¡La columna actual crestó el tablero! Cráneo activo: {1} segundos para congelarse.", "Score: {0}. Current column crested the board! Skull active: {1} seconds until freeze.") },
            { "QuestActiveStatus", new Tuple<string, string>("Misión activa: {0}. Puntuación: {1}.", "Active Mission: {0}. Score: {1}.") }
        };

        public static string Get(string key, params object[] args)
        {
            if (!_dict.ContainsKey(key)) return key;
            string raw = (CurrentLanguage == Language.Spanish) ? _dict[key].Item1 : _dict[key].Item2;
            if (args != null && args.Length > 0) return string.Format(raw, args);
            return raw;
        }

        public static string GetPokerHandName(PokerHandType hand)
        {
            switch (hand)
            {
                case PokerHandType.Pair: return Get("HandPair");
                case PokerHandType.Spectrum: return Get("HandSpectrum");
                case PokerHandType.TwoPair: return Get("HandTwoPair");
                case PokerHandType.ThreeOfAKind: return Get("HandThreeOfAKind");
                case PokerHandType.FullHouse: return Get("HandFullHouse");
                case PokerHandType.FourOfAKind: return Get("HandFourOfAKind");
                case PokerHandType.Flush: return Get("HandFlush");
                default: return Get("HandHighCard");
            }
        }

        public static void ToggleLanguage()
        {
            CurrentLanguage = (CurrentLanguage == Language.Spanish) ? Language.English : Language.Spanish;
        }
    }
}
